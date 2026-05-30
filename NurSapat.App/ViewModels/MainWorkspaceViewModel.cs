using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.App.ViewModels;

public sealed partial class MainWorkspaceViewModel : ObservableObject
{
    private readonly Dictionary<string, object> _screens;
    private bool _isNavigating; // Защита от рекурсии

    public IRelayCommand<string> NavigationCommand { get; }

    [ObservableProperty]
    private object? _currentContent;

    [ObservableProperty]
    private string _currentTitle = "Чертеж";

    [ObservableProperty]
    private int _selectedWorkspaceTabIndex;

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
        if (_isNavigating) return;

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
            return;

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