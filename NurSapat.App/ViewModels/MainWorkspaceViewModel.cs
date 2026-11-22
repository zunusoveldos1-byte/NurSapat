using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Help;
using NurSapat.App.Models.Settings;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Documents;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.EditDialogs;
using NurSapat.App.Services.Export;
using NurSapat.App.Services.Help;
using NurSapat.App.Models.Materials;
using NurSapat.App.Services.Materials;
using NurSapat.App.Services.Orders;
using NurSapat.App.Services.Users;
using NurSapat.App.Services.Settings;
using NurSapat.App.ViewModels.Utilities;
using NurSapat.App.Views;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Services.Orders;
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
    private readonly IExportWindowService _exportWindowService;
    private readonly IUserWindowService _userWindowService;
    private readonly IHelpWindowService _helpWindowService;
    private readonly IEnvironmentSettingsService _environmentSettingsService;
    private readonly IDialogService _dialogService;
    private readonly ICustomersStore _customersStore;
    private readonly IOrderErpDialogService _orderErpDialogService;
    private readonly IUserSessionService _userSessionService;
    private readonly IWorkspaceNavigationService _workspaceNavigation;
    private readonly SystemLogsViewModel _systemLogsViewModel;
    private readonly AdditionalDashboardViewModel _additionalDashboardViewModel;
    private bool _isNavigating;

    // ДОБАВЛЕНО: Ссылка на главное окно для диалогов
    private Window? _ownerWindow;
    private MessagesWindow? _messagesWindow;

    public UtilitiesViewModel Utilities { get; }

    public IRelayCommand<string> NavigationCommand { get; private set; } = null!;

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

    public string ActiveProfileName { get; private set; } = DocumentManager.DefaultConfigurationName;

    public Task<bool> ConfirmCloseAsync() => _documentManager.ConfirmCloseAsync();

    public string CompanyDisplayName { get; private set; } = "—";

    public string CurrencyDisplay { get; private set; } = "KGS";

    public string ConfigurationStatusText { get; private set; } = "Admin · — · KGS";

    public bool IsOffline => _userSessionService.IsOffline;

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
    private Task Logout() => _documentManager.ExitToLoginAsync();

    [RelayCommand]
    private async Task OpenOrderSettingsAsync()
    {
        if (!_documentManager.HasOpenDocument)
        {
            return;
        }

        var order = _orderWorkspace.Order;
        if (!OrderBusinessRules.IsCadReadOnly(order.Status))
        {
            _orderWorkspace.SyncCadToOrder();
        }
        var confirmed = await _orderErpDialogService.ShowOrderSettingsAsync(
            order,
            _userSessionService.IsAdministrator);
        if (!confirmed)
        {
            return;
        }

        _orderWorkspace.ApplyCadReadOnlyStatus();
        if (!OrderBusinessRules.IsCadReadOnly(order.Status))
        {
            _orderWorkspace.RecalculateAll();
        }
        _orderWorkspace.NotifyModified();
    }

    [RelayCommand]
    private async Task ReviewProductionAsync()
    {
        if (!_documentManager.HasOpenDocument)
        {
            return;
        }

        var order = _orderWorkspace.Order;
        if (!OrderBusinessRules.IsCadReadOnly(order.Status))
        {
            _orderWorkspace.SyncCadToOrder();
        }
        var issues = OrderProductionValidator.Review(order);
        if (issues.Count == 0)
        {
            if (ResolveOwnerWindow() is { } owner)
            {
                AppNotificationHelper.ShowSuccess(owner, "Проверка заказа завершена. Замечаний нет.");
            }
            return;
        }

        var labels = issues.Select((issue, index) =>
            $"{index + 1}. {(issue.BlocksProduction ? "Ошибка" : "Проверить")} · {issue.Message}").ToArray();
        var selected = await _dialogService.ShowListPickerAsync("Проверка перед производством", labels);
        var selectedIndex = Array.IndexOf(labels, selected);
        if (selectedIndex >= 0 && issues[selectedIndex] is { ConstructionId: Guid constructionId } issue)
        {
            await _orderWorkspace.EditConstructionInDrawingAsync(constructionId);
            if (issue.ElementId is Guid elementId)
            {
                _orderWorkspace.Drawing.CadEditor.SelectProductionIssueElement(elementId);
            }
        }
    }

    [RelayCommand]
    private async Task SelectOrderCustomerAsync()
    {
        var customer = await _dictionaryWindowService.ShowCustomersForSelectionAsync(ResolveOwnerWindow());
        if (customer is null)
        {
            return;
        }

        _orderWorkspace.Sheet.ApplySelectedCustomer(customer);
        _orderWorkspace.NotifyModified();
    }

    [RelayCommand]
    private async Task SelectOrderManagerAsync()
    {
        await _dictionaryWindowService.ShowManagersAsync(ResolveOwnerWindow());
        _orderWorkspace.Sheet.ReloadManagerLookups();
    }

    [RelayCommand]
    private async Task SelectOrderDealerAsync()
    {
        await _dictionaryWindowService.ShowDealersPlaceholderAsync(ResolveOwnerWindow());
    }

    [RelayCommand]
    private async Task Managers() =>
        await _dictionaryWindowService.ShowManagersAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task OpenPriceModeAsync()
    {
        if (!await _orderErpDialogService.ShowPriceModeAsync(_orderWorkspace.Order))
        {
            return;
        }

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
    private async Task ExportDbfJson() =>
        await _exportWindowService.ShowDbfJsonExportAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task ExportXls() =>
        await _exportWindowService.ShowXlsExportAsync(ResolveOwnerWindow());

    [RelayCommand]
    private async Task ChangePassword() =>
        await _userWindowService.ShowChangePasswordAsync(ResolveOwnerWindow());

    [RelayCommand]
    private void OpenArchive() => Navigate("archive");

    [RelayCommand]
    private void OpenDataTransfer() => Navigate("data-transfer");

    [RelayCommand]
    private void OpenProductionOptions() => Navigate("production-options");

    // ==========================================
    // КОМАНДЫ МЕНЮ "СПРАВКА"
    // ==========================================
    [RelayCommand]
    private async Task OpenHelpGuideWindow() =>
        await _helpWindowService.ShowAsync(ResolveOwnerWindow(), HelpTabKeys.Help);

    [RelayCommand]
    private async Task OpenWhatsNewWindow() =>
        await _helpWindowService.ShowAsync(ResolveOwnerWindow(), HelpTabKeys.WhatsNew);

    [RelayCommand]
    private async Task OpenUpdateWindow() =>
        await _helpWindowService.ShowAsync(ResolveOwnerWindow(), HelpTabKeys.Update);

    [RelayCommand]
    private async Task OpenSupportWindow() =>
        await _helpWindowService.ShowAsync(ResolveOwnerWindow(), HelpTabKeys.Consultation);

    [RelayCommand]
    private async Task OpenAboutWindow() =>
        await _helpWindowService.ShowAsync(ResolveOwnerWindow(), HelpTabKeys.About);

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
        IExportWindowService exportWindowService,
        IUserWindowService userWindowService,
        IHelpWindowService helpWindowService,
        UtilitiesViewModel utilitiesViewModel,
        IEnvironmentSettingsService environmentSettingsService,
        IDialogService dialogService,
        ICustomersStore customersStore,
        IOrderErpDialogService orderErpDialogService,
        IUserSessionService userSessionService,
        IWorkspaceNavigationService workspaceNavigation,
        IMaterialsDetailStore materialsDetailStore,
        IProfileSeriesStore profileSeriesStore)
    {
        _orderWorkspace = orderWorkspace;
        _documentManager = documentManager;
        _recentFilesService = recentFilesService;
        _printService = printService;
        _editDialogService = editDialogService;
        _dictionaryWindowService = dictionaryWindowService;
        _exportWindowService = exportWindowService;
        _userWindowService = userWindowService;
        _helpWindowService = helpWindowService;
        Utilities = utilitiesViewModel;
        Utilities.ConfigureOwnerResolver(ResolveOwnerWindow);
        _environmentSettingsService = environmentSettingsService;
        _dialogService = dialogService;
        _customersStore = customersStore;
        _orderErpDialogService = orderErpDialogService;
        _userSessionService = userSessionService;
        _workspaceNavigation = workspaceNavigation;
        _systemLogsViewModel = systemLogsViewModel;
        _additionalDashboardViewModel = additionalDashboardViewModel;
        _workspaceNavigation.Configure(Navigate);
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
            ["customers"] = new ErpReferenceHubViewModel(
                "Клиенты",
                "Справочник заказчиков, дилеров и контактных данных.",
                GetOnlineCatalogCount(() => _customersStore.GetAll().Count),
                () => Customers(),
                () => Customers()),
            ["materials"] = new ErpReferenceHubViewModel(
                "Материалы и цены",
                "Профили, стеклопакеты, фурнитура и прайс-листы.",
                GetOnlineCatalogCount(() => materialsDetailStore.GetAll().Count),
                () => Materials()),
            ["profile-systems"] = new ErpReferenceHubViewModel(
                "Системы профилей",
                "Каталог профильных серий и производственных систем.",
                GetOnlineCatalogCount(() => profileSeriesStore.GetAll().Count),
                () => Series()),
            ["hardware"] = new ErpReferenceHubViewModel(
                "Фурнитура",
                "Справочник фурнитуры, ручек и комплектующих.",
                GetOnlineCatalogCount(() =>
                    materialsDetailStore.GetAll()
                        .Count(item => item.Section == MaterialSection.Hardware)),
                () => Materials()),
            ["additional"] = additionalDashboardViewModel,
            ["help"] = new ErpReferenceHubViewModel(
                "Помощь",
                "Руководство пользователя, обновления и консультации.",
                0,
                () => OpenHelpGuideWindow())
        };

        Navigate("cad");
        RefreshConfigurationDisplay();
    }

    private int GetOnlineCatalogCount(Func<int> getCount)
    {
        if (_userSessionService.IsOffline)
        {
            return 0;
        }

        return getCount();
    }

    private void OnEnvironmentProfileChanged(object? sender, EventArgs e) =>
        RefreshConfigurationDisplay();

    private void RefreshConfigurationDisplay()
    {
        var settings = _environmentSettingsService.Current;
        ActiveProfileName = _environmentSettingsService.ActiveProfile?.ProfileName
            ?? DocumentManager.DefaultConfigurationName;
        CompanyDisplayName = string.IsNullOrWhiteSpace(settings.CompanyName) ? "—" : settings.CompanyName.Trim();
        CurrencyDisplay = string.IsNullOrWhiteSpace(settings.NationalCurrency) ? "KGS" : settings.NationalCurrency.Trim();
        ConfigurationStatusText = $"{ActiveProfileName} · {CompanyDisplayName} · {CurrencyDisplay}";

        OnPropertyChanged(nameof(ActiveProfileName));
        OnPropertyChanged(nameof(CompanyDisplayName));
        OnPropertyChanged(nameof(CurrencyDisplay));
        OnPropertyChanged(nameof(ConfigurationStatusText));
        OnPropertyChanged(nameof(DocumentTitle));
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

        Navigate(value switch
        {
            0 => "cad",
            1 => "orders",
            _ => "additional"
        });
    }

    private string? _activeScreenKey = "cad";

    private void Navigate(string? key)
    {
        if (key == null || !_screens.TryGetValue(key, out var screen))
        {
            return;
        }

        if (_activeScreenKey == "cad" && key != "cad")
        {
            _orderWorkspace.Drawing.CadEditor.OnDrawingTabDeactivated();
        }

        if (key == "orders")
        {
            _orderWorkspace.Sheet.EnterViewMode();
        }
        else if (_activeScreenKey == "orders")
        {
            _orderWorkspace.Sheet.ExitViewMode();
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
            ErpReferenceHubViewModel hub => hub.Title,
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

        if (key == "cad")
        {
            _orderWorkspace.Drawing.CadEditor.OnDrawingTabActivated();
        }

        if (key == "archive")
        {
            _ = _systemLogsViewModel.LoadCommand.ExecuteAsync(null);
        }

        if (key == "additional")
        {
            _ = _additionalDashboardViewModel.LoadRatesAsync();
        }

        _activeScreenKey = key;
    }

    // Section helper removed — replaced by ErpReferenceHubViewModel.
}
