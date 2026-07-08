using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Help;
using NurSapat.App.Services.Help;
using System.Diagnostics;

namespace NurSapat.App.ViewModels.Help;

public sealed partial class HelpAndAboutViewModel : ViewModelBase
{
    private readonly IAppUpdateService _appUpdateService;

    public HelpAndAboutViewModel(IAppUpdateService appUpdateService)
    {
        _appUpdateService = appUpdateService;
        AppVersion = _appUpdateService.CurrentVersion;
        ChangelogEntries = CreateChangelog();
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync, () => !IsCheckingUpdates);
        OpenDownloadPageCommand = new RelayCommand(OpenDownloadPage, () => !string.IsNullOrWhiteSpace(DownloadUrl));
        OpenTelegramCommand = new RelayCommand(() => OpenUrl(SupportTelegramUrl));
        OpenWhatsAppCommand = new RelayCommand(() => OpenUrl(SupportWhatsAppUrl));
        OpenPhoneCommand = new RelayCommand(() => OpenUrl(SupportPhoneTelUri));
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
    }

    public IAsyncRelayCommand CheckForUpdatesCommand { get; }

    public IRelayCommand OpenDownloadPageCommand { get; }

    public IRelayCommand OpenTelegramCommand { get; }

    public IRelayCommand OpenWhatsAppCommand { get; }

    public IRelayCommand OpenPhoneCommand { get; }

    public IRelayCommand CloseCommand { get; }

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public string AppVersion { get; }

    public string SupportTelegramUrl { get; } = "https://t.me/willofeldos";

    public string SupportWhatsAppUrl { get; } = "https://wa.me/996559340032";

    public string SupportPhoneDisplay { get; } = "+996 (559) 34-00-32";

    public string SupportPhoneTelUri { get; } = "tel:+996559340032";

    public IReadOnlyList<ChangelogEntryViewModel> ChangelogEntries { get; }

    public string HelpGuideText { get; } = """
        Добро пожаловать в NurSapat

        NurSapat — единая CAD/ERP-система для проектирования оконных конструкций, расчёта стоимости и ведения производственных заказов.

        Быстрый старт в конструкторе

        1. Создайте заказ через меню «Заказ → Новый заказ».
        2. Перейдите на вкладку «Чертеж» и выберите инструмент рамки или створки на панели CAD.
        3. Укажите габариты конструкции в свойствах или на чертеже.
        4. Добавьте импосты, фурнитуру и стеклопакеты из библиотеки материалов.
        5. Сохраните проект и выполните расчёт стоимости на вкладке «Заказ».

        Полезные разделы

        • Словари — материалы, профильные системы, фурнитура и клиенты.
        • Сервис — экспорт списания и смена пароля пользователя.
        • Утилиты — обслуживание базы и обмен дилерскими пакетами.
        • Настройки среды — валюта, раскрой, печать и параметры организации.

        Горячие клавиши

        • Ctrl+S — сохранить заказ
        • Ctrl+O — открыть заказ
        • Ctrl+P — печать

        При возникновении вопросов откройте вкладку «Консультация» или обратитесь к администратору системы.
        """;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool _isCheckingUpdates;

    [ObservableProperty]
    private bool _isUpdateProgressIndeterminate;

    [ObservableProperty]
    private string _updateStatusText = "Нажмите кнопку, чтобы проверить наличие обновлений на сервере.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDownloadPageCommand))]
    private string? _downloadUrl;

    public void SetInitialTab(string? tabKey)
    {
        SelectedTabIndex = HelpTabKeys.Parse(tabKey) switch
        {
            HelpTab.WhatsNew => 1,
            HelpTab.Update => 2,
            HelpTab.Consultation => 3,
            HelpTab.About => 4,
            _ => 0
        };
    }

    private async Task CheckForUpdatesAsync()
    {
        IsCheckingUpdates = true;
        IsUpdateProgressIndeterminate = true;
        UpdateStatusText = "Проверка обновлений на GitHub Releases...";
        DownloadUrl = null;

        try
        {
            var result = await _appUpdateService.CheckForUpdatesAsync();
            DownloadUrl = result.DownloadUrl;
            UpdateStatusText = result.Message;

            if (result.IsUpdateAvailable && !string.IsNullOrWhiteSpace(result.DownloadUrl))
            {
                UpdateStatusText += " Нажмите «Открыть страницу загрузки».";
            }
        }
        finally
        {
            IsCheckingUpdates = false;
            IsUpdateProgressIndeterminate = false;
        }
    }

    private void OpenDownloadPage()
    {
        if (!string.IsNullOrWhiteSpace(DownloadUrl))
        {
            OpenUrl(DownloadUrl);
        }
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static IReadOnlyList<ChangelogEntryViewModel> CreateChangelog() =>
    [
        new()
        {
            Version = "V9.0",
            Title = "Версия V9.0 — текущий релиз",
            Items =
            [
                "Новый движок оптимизации SQLite и PostgreSQL для обслуживания базы.",
                "Рефакторинг панели CAD-инструментов и улучшенная навигация по заказам.",
                "Бесшовный интерфейс справочников, экспорта и утилит.",
                "Единое окно «Справка и О программе» с проверкой обновлений.",
                "Модуль дилерских и производственных пакетов обмена данными."
            ]
        },
        new()
        {
            Version = "V8.5",
            Title = "Версия V8.5",
            Items =
            [
                "Расширенные настройки среды и смена конфигурации пользователя.",
                "Экспорт списания в DBF/JSON и XLS.",
                "Справочник элементов конструкций и стандартных типовых решений."
            ]
        }
    ];
}
