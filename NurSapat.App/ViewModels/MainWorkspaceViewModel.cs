using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.UI.ViewModels;
using System;
using System.Collections.Generic;

#nullable enable

namespace NurSapat.App.ViewModels;

public sealed partial class MainWorkspaceViewModel : ObservableObject
{
    private readonly Dictionary<string, object> _screens;
    private bool _isNavigating;

    public IRelayCommand<string> NavigationCommand { get; }

    [ObservableProperty]
    private object? _currentContent;

    [ObservableProperty]
    private string _currentTitle = "Чертеж";

    [ObservableProperty]
    private int _selectedWorkspaceTabIndex;

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
    private void NewOrder() => System.Diagnostics.Debug.WriteLine("NewOrder executed");

    [RelayCommand]
    private void OpenOrder() => System.Diagnostics.Debug.WriteLine("OpenOrder executed");

    [RelayCommand]
    private void SaveOrder() => System.Diagnostics.Debug.WriteLine("SaveOrder executed");

    [RelayCommand]
    private void SaveAsCopyOrder() => System.Diagnostics.Debug.WriteLine("SaveAsCopyOrder executed");

    [RelayCommand]
    private void CloseOrder() => System.Diagnostics.Debug.WriteLine("CloseOrder executed");

    [RelayCommand]
    private void OpenFromFile() => System.Diagnostics.Debug.WriteLine("OpenFromFile executed");

    [RelayCommand]
    private void SaveToFile() => System.Diagnostics.Debug.WriteLine("SaveToFile executed");

    [RelayCommand]
    private void PrintOrder() => System.Diagnostics.Debug.WriteLine("PrintOrder executed");

    [RelayCommand]
    private void PrintSettings() => System.Diagnostics.Debug.WriteLine("PrintSettings executed");

    [RelayCommand]
    private void Exit() => System.Diagnostics.Debug.WriteLine("Exit executed");

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

    [RelayCommand]
    private void ToggleLeftPanel()
    {
        if (CurrentContent is CadEditorViewModel cadVm)
        {
            cadVm.ToggleLeftPanelCommand.Execute(null);
            IsLeftPanelVisible = cadVm.IsLeftPanelVisible; // Синхронизация
        }
    }

    [RelayCommand]
    private void ToggleRightPanel()
    {
        if (CurrentContent is CadEditorViewModel cadVm)
        {
            cadVm.ToggleRightPanelCommand.Execute(null);
            IsRightPanelVisible = cadVm.IsRightPanelVisible; // Синхронизация
        }
    }

    [RelayCommand]
    private void ToggleBottomPanel()
    {
        if (CurrentContent is CadEditorViewModel cadVm)
        {
            cadVm.ToggleBottomPanelCommand.Execute(null);
            IsBottomPanelVisible = cadVm.IsBottomPanelVisible; // Синхронизация
        }
    }

    [RelayCommand]
    private void ToggleTopPanel()
    {
        if (CurrentContent is CadEditorViewModel cadVm)
        {
            cadVm.ToggleTopPanelCommand.Execute(null);
            IsTopPanelVisible = cadVm.IsTopPanelVisible; // Синхронизация
        }
    }

    // ==========================================
    // КОМАНДЫ МЕНЮ "СПРАВОЧНИКИ"
    // ==========================================
    [RelayCommand]
    private void Materials() => System.Diagnostics.Debug.WriteLine("Materials executed");

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
        CadEditorViewModel cadEditorViewModel,
        OrderEditViewModel orderEditViewModel)
    {
        NavigationCommand = new RelayCommand<string>(Navigate);

        _screens = new Dictionary<string, object>
        {
            ["cad"] = cadEditorViewModel,
            ["orders"] = orderEditViewModel,
            ["archive"] = systemLogsViewModel,
            ["production-options"] = productionOptionsViewModel,
            ["environment"] = environmentSettingsViewModel,
            ["data-transfer"] = dataTransferViewModel,
            ["customers"] = Section("Клиенты", "CRUD клиентов будет подключен на следующем этапе.", "Создать клиента", "История заказов"),
            ["materials"] = Section("Материалы и цены", "Складской справочник материалов и прайсов.", "Профили", "Стеклопакеты", "Фурнитура"),
            ["profile-systems"] = Section("Системы профилей", "Каталог профильных систем.", "PVC", "Aluminum"),
            ["hardware"] = Section("Фурнитура", "Справочник фурнитуры и модификаторов створок.", "Петли", "Ручки", "Замки"),
            ["additional"] = Section("Дополнительно", "Сервисные операции и утилиты.", "Импорт/Экспорт", "Оптимизация раскроя", "Калькулятор"),
            ["help"] = Section("Помощь", "Руководство пользователя и справка.", "Чертеж", "Лист заказа")
        };

        Navigate("cad");
    }

    partial void OnSelectedWorkspaceTabIndexChanged(int value)
    {
        if (_isNavigating) return; // Защита от рекурсии

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
            CadEditorViewModel => "Чертеж",
            OrderEditViewModel => "Лист заказа",
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
        _isNavigating = false;
    }

    private static WorkspaceSectionViewModel Section(string title, string description, params string[] items) =>
        new(title, description, items);
}