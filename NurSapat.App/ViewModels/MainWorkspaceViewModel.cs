using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Settings;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Documents;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.EditDialogs;
using NurSapat.App.Services.Settings;
using NurSapat.App.Views;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Calculation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#nullable enable

namespace NurSapat.App.ViewModels;

public sealed partial class MainWorkspaceViewModel : ObservableObject
{
    private readonly Dictionary<string, object> _screens;
    private readonly OrderWorkspaceViewModel _orderWorkspace;
    private readonly IDocumentManager _documentManager;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IPrintService _printService;
    private readonly IEditDialogService _editDialogService;
    private readonly IDictionaryWindowService _dictionaryWindowService;
    private readonly IEnvironmentSettingsService _environmentSettingsService;
    private readonly IDialogService _dialogService;
    private readonly ICustomersStore _customersStore;
    private bool _isNavigating;

    // ДОБАВЛЕНО: Ссылка на главное окно для диалогов
    private Window? _ownerWindow;
    private MessagesWindow? _messagesWindow;

    public IRelayCommand<string> NavigationCommand { get; }

    [ObservableProperty]
    private object? _currentContent;

    [ObservableProperty]
    private string _currentTitle = "Чертеж";

    [ObservableProperty]
    private int _selectedWorkspaceTabIndex;

    public bool IsCadSelected => SelectedWorkspaceTabIndex == 0;

    public bool IsOrdersSelected => SelectedWorkspaceTabIndex == 1;

    public bool IsAdditionalSelected => SelectedWorkspaceTabIndex == 2;

    public string DocumentTitle => _documentManager.WindowTitle;

    public string ActiveProfileName { get; private set; } = "Admin";

    public string CompanyDisplayName { get; private set; } = "—";

    public string CurrencyDisplay { get; private set; } = "KGS";

    public string ConfigurationStatusText { get; private set; } = "Admin · — · KGS";

    public ObservableCollection<RecentProjectMenuItem> RecentProjects { get; } = new();

    // ДОБАВЛЕНО: Свойства для галочек в меню
    [ObservableProperty]
    private bool _isLeftPanelVisible = true;

    [ObservableProperty]
    private bool _isRightPanelVisible = true;

    [ObservableProperty]
    private bool _isBottomPanelVisible = true;

    [ObservableProperty]
    private bool _isTopPanelVisible = true;

    // ==========================================
    // КОМАНДЫ МЕНЮ "ЗАКАЗ"
    // ==========================================
    [RelayCommand]
    private Task NewOrder() => _documentManager.NewOrderAsync();

    [RelayCommand]
    private Task OpenOrder() => _documentManager.OpenOrderAsync();

    [RelayCommand]
    private Task SaveOrder() => _documentManager.SaveOrderAsync();

    [RelayCommand]
    private Task SaveAsCopyOrder() => _documentManager.SaveAsCopyAsync();

    [RelayCommand]
    private Task CloseOrder() => _documentManager.CloseOrderAsync();

    [RelayCommand]
    private Task PrintOrder() => _printService.ShowPrintDialogAsync();

    [RelayCommand]
    private Task PrintSettings() => _printService.ShowPrintSettingsDialogAsync();

    [RelayCommand]
    private Task Exit() => _documentManager.ExitToLoginAsync();

    [RelayCommand]
    private async Task SelectOrderCustomerAsync()
    {
        var names = _customersStore.GetAll()
            .Where(c => c.Type is not CustomerType.Dealer)
            .Select(c => c.FullName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        var selected = await _dialogService.ShowListPickerAsync("Выбрать заказчика", names);
        if (selected is null)
        {
            return;
        }

        _orderWorkspace.Sheet.CustomerName = selected;
    }

    [RelayCommand]
    private async Task SelectOrderDealerAsync()
    {
        var names = _customersStore.GetAll()
            .Where(c => c.Type == CustomerType.Dealer)
            .Select(c => c.FullName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        var selected = await _dialogService.ShowListPickerAsync("Выбрать дилера", names);
        if (selected is null)
        {
            return;
        }

        _orderWorkspace.Sheet.Header.DealerName = selected;
        _orderWorkspace.NotifyModified();
    }

    [RelayCommand]
    private void TogglePriceMode()
    {
        var construction = _orderWorkspace.Order.SelectedConstruction
            ?? _orderWorkspace.Order.EnsureDefaultConstruction();
        var next = construction.Cost.ExpensesMode switch
        {
            CostInputMode.Fixed => CostInputMode.PerSquareMeter,
            CostInputMode.PerSquareMeter => CostInputMode.Percent,
            CostInputMode.Percent => CostInputMode.Fixed,
            _ => CostInputMode.Fixed
        };

        construction.Cost.ExpensesMode = next;
        _orderWorkspace.RecalculateAll();
        _orderWorkspace.NotifyModified();
    }

    [RelayCommand]
    private async Task RemoveCurrentConstructionAsync()
    {
        if (ResolveCadEditor() is { } cadEditor)
        {
            await cadEditor.RemoveConstructionCommand.ExecuteAsync(null);
        }
    }

    // ==========================================
    // КОМАНДЫ МЕНЮ "ПРАВКА"
    // ==========================================
    [RelayCommand(CanExecute = nameof(CanUseEditDialogs))]
    public Task OpenCopyDrawingWithoutDimensions() =>
        _editDialogService.ShowCopyDrawingDialogAsync(includeDimensionLines: false);

    [RelayCommand(CanExecute = nameof(CanUseEditDialogs))]
    public Task OpenCopyDrawingWithDimensions() =>
        _editDialogService.ShowCopyDrawingDialogAsync(includeDimensionLines: true);

    [RelayCommand(CanExecute = nameof(CanUseEditDialogs))]
    public Task OpenConstructionColorDialog() =>
        _editDialogService.ShowConstructionColorDialogAsync();

    [RelayCommand(CanExecute = nameof(CanUseEditDialogs))]
    public Task OpenOrderColorDialog() =>
        _editDialogService.ShowOrderColorDialogAsync();

    private bool CanUseEditDialogs() => ResolveCadEditor() is not null;

    // ==========================================
    // КОМАНДЫ МЕНЮ "ВИД"
    // ==========================================
    [RelayCommand]
    private async Task CostTable()
    {
        var provider = App.ServiceProvider;
        if (provider is null || _ownerWindow is null)
        {
            return;
        }

        var viewModel = provider.GetRequiredService<CostTableViewModel>();
        viewModel.CalculateFromCurrentConstruction();

        var window = new CostTableWindow
        {
            DataContext = viewModel
        };

        await window.ShowDialog(_ownerWindow);
    }

    [RelayCommand]
    private void Messages()
    {
        var provider = App.ServiceProvider;
        if (provider is null)
        {
            return;
        }

        if (_messagesWindow is { IsVisible: true })
        {
            _messagesWindow.Activate();
            return;
        }

        var viewModel = provider.GetRequiredService<MessagesViewModel>();
        _messagesWindow = new MessagesWindow
        {
            DataContext = viewModel
        };

        _messagesWindow.Closed += (_, _) => _messagesWindow = null;
        if (_ownerWindow is not null)
        {
            _messagesWindow.Show(_ownerWindow);
        }
        else
        {
            _messagesWindow.Show();
        }
    }

    private CadEditorViewModel? ResolveCadEditor() =>
        CurrentContent switch
        {
            CadEditorViewModel cad => cad,
            OrderDrawingViewModel drawing => drawing.CadEditor,
            _ => null
        };

    [RelayCommand]
    private void ToggleToolPanel()
    {
        if (CurrentContent is OrderDrawingViewModel drawing)
        {
            drawing.CadEditor.ToggleLeftPanelCommand.Execute(null);
            IsLeftPanelVisible = drawing.CadEditor.IsLeftPanelVisible;
            return;
        }

        if (ResolveCadEditor() is { } cadVm)
        {
            cadVm.ToggleLeftPanelCommand.Execute(null);
            IsLeftPanelVisible = cadVm.IsLeftPanelVisible;
        }
    }

    [RelayCommand]
    private void ToggleLeftPanel()
    {
        if (CurrentContent is OrderDrawingViewModel drawing)
        {
            ToggleToolPanel();
            return;
        }

        if (ResolveCadEditor() is { } cadVm)
        {
            cadVm.ToggleLeftPanelCommand.Execute(null);
            IsLeftPanelVisible = cadVm.IsLeftPanelVisible;
        }
    }

    [RelayCommand]
    private void ToggleRightPanel()
    {
        if (ResolveCadEditor() is { } cadVm)
        {
            cadVm.ToggleRightPanelCommand.Execute(null);
            IsRightPanelVisible = cadVm.IsRightPanelVisible;
        }
    }

    [RelayCommand]
    private void ToggleBottomPanel()
    {
        if (ResolveCadEditor() is { } cadVm)
        {
            cadVm.ToggleBottomPanelCommand.Execute(null);
            IsBottomPanelVisible = cadVm.IsBottomPanelVisible;
        }
    }

    [RelayCommand]
    private void ToggleTopPanel()
    {
        if (ResolveCadEditor() is { } cadVm)
        {
            cadVm.ToggleTopPanelCommand.Execute(null);
            IsTopPanelVisible = cadVm.IsTopPanelVisible;
        }
    }

    // ==========================================
    // КОМАНДЫ МЕНЮ "СПРАВОЧНИКИ"
    // ==========================================

    // ДОБАВЛЕНО: Метод для передачи окна-владельца
    public void SetOwnerWindow(Window window)
    {
        _ownerWindow = window;
    }

    private Window? ResolveOwnerWindow()
    {
        if (_ownerWindow is not null)
        {
            return _ownerWindow;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }

    [RelayCommand]
    private async Task Materials() =>
        await _dictionaryWindowService.ShowMaterialsAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task Customers() =>
        await _dictionaryWindowService.ShowCustomersAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task Orders() =>
        await _dictionaryWindowService.ShowOrdersAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task StockMaterials() =>
        await _dictionaryWindowService.ShowStockMaterialsAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task StockProfileCuts() =>
        await _dictionaryWindowService.ShowStockProfileCutsAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task StockGlassCuts() =>
        await _dictionaryWindowService.ShowStockGlassCutsAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task StockFinished() =>
        await _dictionaryWindowService.ShowStockFinishedGoodsAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task StandardStructures() =>
        await _dictionaryWindowService.ShowStandardStructuresAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task GraphicSymbols() =>
        await _dictionaryWindowService.ShowGraphicSymbolsAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task Users() =>
        await _dictionaryWindowService.ShowUsersAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task ExchangeRates() =>
        await _dictionaryWindowService.ShowExchangeRatesAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task Series() =>
        await _dictionaryWindowService.ShowProfileSeriesAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task Manufacturers() =>
        await _dictionaryWindowService.ShowManufacturersAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task Suppliers() =>
        await _dictionaryWindowService.ShowSuppliersAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task OrderTemplates() =>
        await _dictionaryWindowService.ShowOrderTemplatesAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task StructureNames() =>
        await _dictionaryWindowService.ShowStructureNamesAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task Colors() =>
        await _dictionaryWindowService.ShowWindowColorsAsync(ResolveOwnerWindow());

    // ==========================================
    // КОМАНДЫ МЕНЮ "НАСТРОЙКИ"
    // ==========================================
    [RelayCommand]
    private async Task Environment() =>
        await _dictionaryWindowService.ShowEnvironmentSettingsAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task ChangeConfiguration() =>
        await _dictionaryWindowService.ShowChangeConfigurationAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task ConstructionElements() =>
        await _dictionaryWindowService.ShowConstructionElementsAsync(ResolveOwnerWindow());

    // ==========================================
    // КОМАНДЫ МЕНЮ "СЕРВИС"
    // ==========================================
    [RelayCommand]
    private void ExportWriteOffDBF() => System.Diagnostics.Debug.WriteLine("ExportWriteOffDBF executed");

    [RelayCommand]
    private void ExportWriteOffXLS() => System.Diagnostics.Debug.WriteLine("ExportWriteOffXLS executed");

    [RelayCommand]
    private void ImportMosquitoNet() => System.Diagnostics.Debug.WriteLine("ImportMosquitoNet executed");

    [RelayCommand]
    private void ImportAccessories() => System.Diagnostics.Debug.WriteLine("ImportAccessories executed");

    // ==========================================
    // КОМАНДЫ МЕНЮ "УТИЛИТЫ"
    // ==========================================
    [RelayCommand]
    private void CompressDatabase() => System.Diagnostics.Debug.WriteLine("CompressDatabase executed");

    [RelayCommand]
    private void GenerateDealerDatabase() => System.Diagnostics.Debug.WriteLine("GenerateDealerDatabase executed");

    [RelayCommand]
    private void SetDatabasePassword() => System.Diagnostics.Debug.WriteLine("SetDatabasePassword executed");

    [RelayCommand]
    private void CorporateNode() => System.Diagnostics.Debug.WriteLine("CorporateNode executed");

    [RelayCommand]
    private void GenerateProductionDatabase() => System.Diagnostics.Debug.WriteLine("GenerateProductionDatabase executed");

    [RelayCommand]
    private void UpdateProductionDatabase() => System.Diagnostics.Debug.WriteLine("UpdateProductionDatabase executed");

    // ==========================================
    // КОМАНДЫ МЕНЮ "ПОМОЩЬ"
    // ==========================================
    [RelayCommand]
    private void Help() => System.Diagnostics.Debug.WriteLine("Help executed");

    [RelayCommand]
    private void WhatIsNew() => System.Diagnostics.Debug.WriteLine("WhatIsNew executed");

    [RelayCommand]
    private void UpdateProgram() => System.Diagnostics.Debug.WriteLine("UpdateProgram executed");

    [RelayCommand]
    private void Consultation() => System.Diagnostics.Debug.WriteLine("Consultation executed");

    [RelayCommand]
    private void About() => System.Diagnostics.Debug.WriteLine("About executed");

    // ==========================================
    // КОНСТРУКТОР И СУЩЕСТВУЮЩАЯ ЛОГИКА НАВИГАЦИИ
    // ==========================================
    public MainWorkspaceViewModel(
        ProductionOptionsViewModel productionOptionsViewModel,
        EnvironmentPathsViewModel environmentPathsViewModel,
        DataTransferViewModel dataTransferViewModel,
        SystemLogsViewModel systemLogsViewModel,
        AdditionalDashboardViewModel additionalDashboardViewModel,
        OrderWorkspaceViewModel orderWorkspace,
        OrderSheetViewModel orderSheetViewModel,
        IDocumentManager documentManager,
        IRecentFilesService recentFilesService,
        IPrintService printService,
        IEditDialogService editDialogService,
        IDictionaryWindowService dictionaryWindowService,
        IEnvironmentSettingsService environmentSettingsService,
        IDialogService dialogService,
        ICustomersStore customersStore)
    {
        _orderWorkspace = orderWorkspace;
        _documentManager = documentManager;
        _recentFilesService = recentFilesService;
        _printService = printService;
        _editDialogService = editDialogService;
        _dictionaryWindowService = dictionaryWindowService;
        _environmentSettingsService = environmentSettingsService;
        _dialogService = dialogService;
        _customersStore = customersStore;
        _orderWorkspace.NavigateToCadTabHandler = () =>
        {
            Navigate("cad");
            return Task.CompletedTask;
        };
        orderWorkspace.Drawing.CadEditor.OpenStandardStructuresHandler =
            () => _dictionaryWindowService.ShowStandardStructuresAsync(ResolveOwnerWindow());
        _environmentSettingsService.ProfileChanged += OnEnvironmentProfileChanged;
        NavigationCommand = new RelayCommand<string>(Navigate);

        _screens = new Dictionary<string, object>
        {
            ["cad"] = orderWorkspace.Drawing,
            ["orders"] = orderSheetViewModel,
            ["archive"] = systemLogsViewModel,
            ["production-options"] = productionOptionsViewModel,
            ["environment"] = environmentPathsViewModel,
            ["data-transfer"] = dataTransferViewModel,
            ["customers"] = Section("Клиенты", "CRUD клиентов будет подключен на следующем этапе.", "Создать клиента", "История заказов"),
            ["materials"] = Section("Материалы и цены", "Складской справочник материалов и прайсов.", "Профили", "Стеклопакеты", "Фурнитура"),
            ["profile-systems"] = Section("Системы профилей", "Каталог профильных систем.", "PVC", "Aluminum"),
            ["hardware"] = Section("Фурнитура", "Справочник фурнитуры и модификаторов створок.", "Петли", "Ручки", "Замки"),
            ["additional"] = additionalDashboardViewModel,
            ["help"] = Section("Помощь", "Руководство пользователя и справка.", "Чертеж", "Лист заказа")
        };

        Navigate("cad");
        RefreshConfigurationDisplay();
    }

    private void OnEnvironmentProfileChanged(object? sender, EventArgs e) =>
        RefreshConfigurationDisplay();

    private void RefreshConfigurationDisplay()
    {
        var settings = _environmentSettingsService.Current;
        ActiveProfileName = _environmentSettingsService.ActiveProfile?.ProfileName ?? ConfigurationProfile.AdminProfileName;
        CompanyDisplayName = string.IsNullOrWhiteSpace(settings.CompanyName) ? "—" : settings.CompanyName.Trim();
        CurrencyDisplay = string.IsNullOrWhiteSpace(settings.NationalCurrency) ? "KGS" : settings.NationalCurrency.Trim();
        ConfigurationStatusText = $"{ActiveProfileName} · {CompanyDisplayName} · {CurrencyDisplay}";

        OnPropertyChanged(nameof(ActiveProfileName));
        OnPropertyChanged(nameof(CompanyDisplayName));
        OnPropertyChanged(nameof(CurrencyDisplay));
        OnPropertyChanged(nameof(ConfigurationStatusText));
    }

    public void InitializeDocumentLifecycle()
    {
        _orderWorkspace.Sheet.AttachDocumentManager(_documentManager);
        _documentManager.DocumentStateChanged -= OnDocumentStateChanged;
        _documentManager.DocumentStateChanged += OnDocumentStateChanged;
        _documentManager.PrepareWorkspace();
        _recentFilesService.RecentFilesChanged -= OnRecentFilesChanged;
        _recentFilesService.RecentFilesChanged += OnRecentFilesChanged;
        RebuildRecentProjects();
        OnDocumentStateChanged(this, EventArgs.Empty);
    }

    private void OnRecentFilesChanged(object? sender, EventArgs e) => RebuildRecentProjects();

    private void OnDocumentStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(DocumentTitle));
    }

    private void RebuildRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var entry in _recentFilesService.GetRecentFiles())
        {
            RecentProjects.Add(new RecentProjectMenuItem(entry, _documentManager));
        }
    }

    partial void OnSelectedWorkspaceTabIndexChanged(int value)
    {
        if (_isNavigating) return;

        if (value == 1)
        {
            _orderWorkspace.Sheet.EnterViewMode();
        }
        else
        {
            _orderWorkspace.Sheet.ExitViewMode();
        }

        Navigate(value switch
        {
            0 => "cad",
            1 => "orders",
            _ => "additional"
        });
    }

    private void Navigate(string? key)
    {
        if (key == null || !_screens.TryGetValue(key, out var screen))
        {
            return;
        }

        CurrentContent = screen;
        CurrentTitle = screen switch
        {
            OrderDrawingViewModel => "Чертеж",
            OrderSheetViewModel => "Лист заказа",
            SystemLogsViewModel => "Архив / Сводный журнал",
            ProductionOptionsViewModel => "Технологические допуски",
            EnvironmentPathsViewModel => "Настройка окружения",
            DataTransferViewModel => "Коммерческий перенос данных",
            WorkspaceSectionViewModel section => section.Title,
            _ => "NurSapat"
        };

        _isNavigating = true;
        SelectedWorkspaceTabIndex = key switch
        {
            "cad" => 0,
            "orders" => 1,
            "additional" => 2,
            _ => SelectedWorkspaceTabIndex
        };
        OnPropertyChanged(nameof(IsCadSelected));
        OnPropertyChanged(nameof(IsOrdersSelected));
        OnPropertyChanged(nameof(IsAdditionalSelected));
        _isNavigating = false;
    }

    private static WorkspaceSectionViewModel Section(string title, string description, params string[] items) =>
        new(title, description, items);
}