using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services;
using NurSapat.App.Services.Documents;
using NurSapat.Cad.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

#nullable enable

namespace NurSapat.App.ViewModels;

public sealed partial class MainWorkspaceViewModel : ObservableObject
{
    private readonly Dictionary<string, object> _screens;
    private readonly OrderWorkspaceViewModel _orderWorkspace;
    private readonly IDocumentManager _documentManager;
    private readonly IRecentFilesService _recentFilesService;
    private readonly IPrintService _printService;
    private bool _isNavigating;

    // ДОБАВЛЕНО: Ссылка на главное окно для диалогов
    private Window? _ownerWindow;

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

    // ==========================================
    // КОМАНДЫ МЕНЮ "ПРАВКА"
    // ==========================================
    [RelayCommand]
    private void CopyDrawing() => System.Diagnostics.Debug.WriteLine("CopyDrawing executed");

    [RelayCommand]
    private void PasteDrawing() => System.Diagnostics.Debug.WriteLine("PasteDrawing executed");

    [RelayCommand]
    private void ChangeConstructionElements() => System.Diagnostics.Debug.WriteLine("ChangeConstructionElements executed");

    [RelayCommand]
    private void ChangeOrderElements() => System.Diagnostics.Debug.WriteLine("ChangeOrderElements executed");

    // ==========================================
    // КОМАНДЫ МЕНЮ "ВИД" (ИСПРАВЛЕНЫ)
    // ==========================================
    [RelayCommand]
    private void CostTable() => System.Diagnostics.Debug.WriteLine("CostTable executed");

    [RelayCommand]
    private void Messages() => System.Diagnostics.Debug.WriteLine("Messages executed");

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

    [RelayCommand]
    private void Materials()
    {
        var provider = App.ServiceProvider;
        if (provider == null || _ownerWindow == null) return;

        var vm = provider.GetRequiredService<MaterialsViewModel>();

        var window = new Window
        {
            Title = "Материалы",
            Width = 800,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = vm
        };

        window.ShowDialog(_ownerWindow);
    }

    [RelayCommand]
    private void Customers() => System.Diagnostics.Debug.WriteLine("Customers executed");

    [RelayCommand]
    private void Orders() => System.Diagnostics.Debug.WriteLine("Orders executed");

    [RelayCommand]
    private void StockMaterials() => System.Diagnostics.Debug.WriteLine("StockMaterials executed");

    [RelayCommand]
    private void StockProfileCuts() => System.Diagnostics.Debug.WriteLine("StockProfileCuts executed");

    [RelayCommand]
    private void StockGlassCuts() => System.Diagnostics.Debug.WriteLine("StockGlassCuts executed");

    [RelayCommand]
    private void StockFinished() => System.Diagnostics.Debug.WriteLine("StockFinished executed");

    [RelayCommand]
    private void StandardStructures() => System.Diagnostics.Debug.WriteLine("StandardStructures executed");

    [RelayCommand]
    private void Users() => System.Diagnostics.Debug.WriteLine("Users executed");

    [RelayCommand]
    private void ExchangeRates() => System.Diagnostics.Debug.WriteLine("ExchangeRates executed");

    [RelayCommand]
    private void Series() => System.Diagnostics.Debug.WriteLine("Series executed");

    [RelayCommand]
    private void Manufacturers() => System.Diagnostics.Debug.WriteLine("Manufacturers executed");

    [RelayCommand]
    private void Suppliers() => System.Diagnostics.Debug.WriteLine("Suppliers executed");

    [RelayCommand]
    private void OrderTemplates() => System.Diagnostics.Debug.WriteLine("OrderTemplates executed");

    [RelayCommand]
    private void StructureNames() => System.Diagnostics.Debug.WriteLine("StructureNames executed");

    // ==========================================
    // КОМАНДЫ МЕНЮ "НАСТРОЙКИ"
    // ==========================================
    [RelayCommand]
    private void Environment() => System.Diagnostics.Debug.WriteLine("Environment executed");

    [RelayCommand]
    private void ChangeConfiguration() => System.Diagnostics.Debug.WriteLine("ChangeConfiguration executed");

    [RelayCommand]
    private void ConstructionElements() => System.Diagnostics.Debug.WriteLine("ConstructionElements executed");

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
        EnvironmentSettingsViewModel environmentSettingsViewModel,
        DataTransferViewModel dataTransferViewModel,
        SystemLogsViewModel systemLogsViewModel,
        AdditionalDashboardViewModel additionalDashboardViewModel,
        OrderWorkspaceViewModel orderWorkspace,
        OrderSheetViewModel orderSheetViewModel,
        IDocumentManager documentManager,
        IRecentFilesService recentFilesService,
        IPrintService printService)
    {
        _orderWorkspace = orderWorkspace;
        _documentManager = documentManager;
        _recentFilesService = recentFilesService;
        _printService = printService;
        NavigationCommand = new RelayCommand<string>(Navigate);

        _screens = new Dictionary<string, object>
        {
            ["cad"] = orderWorkspace.Drawing,
            ["orders"] = orderSheetViewModel,
            ["archive"] = systemLogsViewModel,
            ["production-options"] = productionOptionsViewModel,
            ["environment"] = environmentSettingsViewModel,
            ["data-transfer"] = dataTransferViewModel,
            ["customers"] = Section("Клиенты", "CRUD клиентов будет подключен на следующем этапе.", "Создать клиента", "История заказов"),
            ["materials"] = Section("Материалы и цены", "Складской справочник материалов и прайсов.", "Профили", "Стеклопакеты", "Фурнитура"),
            ["profile-systems"] = Section("Системы профилей", "Каталог профильных систем.", "PVC", "Aluminum"),
            ["hardware"] = Section("Фурнитура", "Справочник фурнитуры и модификаторов створок.", "Петли", "Ручки", "Замки"),
            ["additional"] = additionalDashboardViewModel,
            ["help"] = Section("Помощь", "Руководство пользователя и справка.", "Чертеж", "Лист заказа")
        };

        Navigate("cad");
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
            EnvironmentSettingsViewModel => "Настройка окружения",
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