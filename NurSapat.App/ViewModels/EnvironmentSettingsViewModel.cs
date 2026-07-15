using Avalonia.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Settings;
using NurSapat.App.Services;
using NurSapat.App.Services.Settings;
using NurSapat.App.ViewModels.Dialogs;
using NurSapat.App.ViewModels.Settings;
using NurSapat.App.Views.Dialogs;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels;

public sealed partial class EnvironmentSettingsViewModel : ViewModelBase
{
    private readonly IEnvironmentSettingsService _settingsService;
    private readonly IAppMessageService _messageService;

    public EnvironmentSettingsViewModel(
        IEnvironmentSettingsService settingsService,
        IAppMessageService messageService,
        UpdateSettingsViewModel updateSettings)
    {
        _settingsService = settingsService;
        _messageService = messageService;
        UpdateSettings = updateSettings;
        CurrencyOptions = ["KGS", "USD", "EUR", "CNY", "грн"];
        LanguageOptions = ["Русский", "Кыргызча"];
        VatModeOptions = VatModeExtensions.AllOptions;
        ProfileCutLevelOptions = CuttingOptionExtensions.ProfileCutLevelOptions;
        ProfileWasteModeOptions = CuttingOptionExtensions.ProfileWasteModeOptions;
        GlassWasteModeOptions = CuttingOptionExtensions.GlassWasteModeOptions;
        DbfExportDuplicateCheckFieldOptions =
        [
            "Номер заказа",
            "Договор",
            "Заказчик",
            "Адрес объекта"
        ];
        CalculationWarehouseOptions =
        [
            string.Empty,
            "Встроенный склад",
            "Внешний склад"
        ];
        AddUtilityCommand = new AsyncRelayCommand(AddUtilityAsync);
        EditUtilityCommand = new AsyncRelayCommand(EditUtilityAsync, () => SelectedExternalUtility is not null);
        DeleteUtilityCommand = new RelayCommand(DeleteUtility, () => SelectedExternalUtility is not null);
        ChangeOrderNumberingCommand = new AsyncRelayCommand(ChangeOrderNumberingAsync);
        RestoreOrderNumberingCommand = new RelayCommand(RestoreOrderNumbering);
        CompressAllOrdersCommand = new RelayCommand(CompressAllOrders);
        ApplyFromModel(_settingsService.Current);
    }

    public UpdateSettingsViewModel UpdateSettings { get; }

    public IReadOnlyList<string> CalculationWarehouseOptions { get; }

    public IReadOnlyList<string> DbfExportDuplicateCheckFieldOptions { get; }

    public IReadOnlyList<string> CurrencyOptions { get; }

    public IReadOnlyList<string> LanguageOptions { get; }

    public IReadOnlyList<VatModeOption> VatModeOptions { get; }

    public IReadOnlyList<ProfileCutLevelOption> ProfileCutLevelOptions { get; }

    public IReadOnlyList<ProfileWasteModeOption> ProfileWasteModeOptions { get; }

    public IReadOnlyList<GlassWasteModeOption> GlassWasteModeOptions { get; }

    public Window? OwnerWindow { get; set; }

    public Func<Task<string?>>? PickColorHexAsync { get; set; }

    public event Action? RequestClose;

    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string _legalAddress = string.Empty;

    [ObservableProperty]
    private string _checkingAccount = string.Empty;

    [ObservableProperty]
    private string _mfo = string.Empty;

    [ObservableProperty]
    private string _okpoCode = string.Empty;

    [ObservableProperty]
    private string _bankName = string.Empty;

    [ObservableProperty]
    private string _calculatorPath = string.Empty;

    [ObservableProperty]
    private string _backupPath = string.Empty;

    [ObservableProperty]
    private string _nationalCurrency = "KGS";

    [ObservableProperty]
    private string _orderViewCurrency = "KGS";

    [ObservableProperty]
    private decimal _vatPercent = 20m;

    [ObservableProperty]
    private VatModeOption? _selectedVatModeOption;

    [ObservableProperty]
    private double _impostStepMm = 10;

    [ObservableProperty]
    private int _archiveDays = 5;

    [ObservableProperty]
    private int _priceAccuracy = 2;

    [ObservableProperty]
    private string _interfaceLanguage = "Русский";

    [ObservableProperty]
    private bool _isOrderBackgroundGray;

    [ObservableProperty]
    private bool _isCustomFontEnabled;

    [ObservableProperty]
    private double _fontSize = 12;

    [ObservableProperty]
    private string _selectedElementsColor = "#2563EB";

    [ObservableProperty]
    private ProfileCutLevelOption? _selectedProfileCutLevelOption;

    [ObservableProperty]
    private ProfileWasteModeOption? _selectedProfileWasteModeOption;

    [ObservableProperty]
    private bool _avoidDeadZoneCuts;

    [ObservableProperty]
    private bool _parallelCutTwoBars;

    [ObservableProperty]
    private double _minGlassCutLengthMm = 150;

    [ObservableProperty]
    private double _minGlassCutAreaM2 = 0.05;

    [ObservableProperty]
    private GlassWasteModeOption? _selectedGlassWasteModeOption;

    [ObservableProperty]
    private string _centralOfficeEmail = string.Empty;

    [ObservableProperty]
    private string _ownEmail = string.Empty;

    [ObservableProperty]
    private string _smtpServer = string.Empty;

    [ObservableProperty]
    private int _smtpPort = 465;

    [ObservableProperty]
    private string _pop3Server = string.Empty;

    [ObservableProperty]
    private int _pop3Port = 995;

    [ObservableProperty]
    private string _mailUser = string.Empty;

    [ObservableProperty]
    private string _mailPassword = string.Empty;

    [ObservableProperty]
    private int _standardLineWidth = 3;

    [ObservableProperty]
    private int _daysToProduction = 3;

    [ObservableProperty]
    private int _daysToInstallation = 7;

    private int _dealerExpiryWarningDays = 3;
    private string _calculationWarehouseName = string.Empty;
    private string _branchCode = string.Empty;
    private string _orderNumberSeparator = "-";
    private bool _includeBranchCodeInOrderNumber;
    private int _exportImageWidth = 400;
    private int _exportImageHeight = 300;
    private int _exportImageDpi = 96;
    private string _exportImageFormat = "BMP";
    private string _infoField1Name = "Информация 1";
    private string _infoField2Name = "Информация 2";
    private string _infoField3Name = "Информация 3";
    private string _infoField4Name = "Информация 4";

    public int DealerExpiryWarningDays
    {
        get => _dealerExpiryWarningDays;
        set => SetProperty(ref _dealerExpiryWarningDays, value);
    }

    public string CalculationWarehouseName
    {
        get => _calculationWarehouseName;
        set => SetProperty(ref _calculationWarehouseName, value);
    }

    public string BranchCode
    {
        get => _branchCode;
        set => SetProperty(ref _branchCode, value);
    }

    public string OrderNumberSeparator
    {
        get => _orderNumberSeparator;
        set => SetProperty(ref _orderNumberSeparator, value);
    }

    public bool IncludeBranchCodeInOrderNumber
    {
        get => _includeBranchCodeInOrderNumber;
        set => SetProperty(ref _includeBranchCodeInOrderNumber, value);
    }

    public int ExportImageWidth
    {
        get => _exportImageWidth;
        set => SetProperty(ref _exportImageWidth, value);
    }

    public int ExportImageHeight
    {
        get => _exportImageHeight;
        set => SetProperty(ref _exportImageHeight, value);
    }

    public int ExportImageDpi
    {
        get => _exportImageDpi;
        set => SetProperty(ref _exportImageDpi, value);
    }

    public string ExportImageFormat
    {
        get => _exportImageFormat;
        set
        {
            if (SetProperty(ref _exportImageFormat, value))
            {
                OnPropertyChanged(nameof(IsExportImageFormatBmp));
                OnPropertyChanged(nameof(IsExportImageFormatJpg));
                OnPropertyChanged(nameof(IsExportImageFormatEmf));
            }
        }
    }

    public bool IsExportImageFormatBmp
    {
        get => ExportImageFormat == "BMP";
        set { if (value) ExportImageFormat = "BMP"; }
    }

    public bool IsExportImageFormatJpg
    {
        get => ExportImageFormat == "JPG";
        set { if (value) ExportImageFormat = "JPG"; }
    }

    public bool IsExportImageFormatEmf
    {
        get => ExportImageFormat == "EMF";
        set { if (value) ExportImageFormat = "EMF"; }
    }

    public string InfoField1Name
    {
        get => _infoField1Name;
        set => SetProperty(ref _infoField1Name, value);
    }

    public string InfoField2Name
    {
        get => _infoField2Name;
        set => SetProperty(ref _infoField2Name, value);
    }

    public string InfoField3Name
    {
        get => _infoField3Name;
        set => SetProperty(ref _infoField3Name, value);
    }

    public string InfoField4Name
    {
        get => _infoField4Name;
        set => SetProperty(ref _infoField4Name, value);
    }

    public IAsyncRelayCommand ChangeOrderNumberingCommand { get; }

    public IRelayCommand RestoreOrderNumberingCommand { get; }

    public IRelayCommand CompressAllOrdersCommand { get; }

    [ObservableProperty]
    private int _nextOrderNumber = 1;

    [ObservableProperty]
    private int _originalNextOrderNumber = 1;

    [ObservableProperty]
    private bool _isCustomOrderNumbering;

    [ObservableProperty]
    private double _glassStickerWidth = 67.2;

    [ObservableProperty]
    private double _glassStickerHeight = 21.2;

    [ObservableProperty]
    private double _glassStickerFrameWidth = 66.0;

    [ObservableProperty]
    private double _glassStickerFrameHeight = 20.0;

    [ObservableProperty]
    private double _glassStickerLeftMargin = 3.6;

    [ObservableProperty]
    private double _glassStickerTopMargin = 12.7;

    [ObservableProperty]
    private double _glassStickerHorizontalGap;

    [ObservableProperty]
    private double _glassStickerVerticalGap;

    [ObservableProperty]
    private int _glassStickersColumns = 3;

    [ObservableProperty]
    private int _glassStickersRows = 13;

    [ObservableProperty]
    private double _profileStickerWidth = 67.2;

    [ObservableProperty]
    private double _profileStickerHeight = 28.0;

    [ObservableProperty]
    private double _profileStickerFrameWidth = 66.0;

    [ObservableProperty]
    private double _profileStickerFrameHeight = 27.0;

    [ObservableProperty]
    private double _profileStickerLeftMargin = 3.6;

    [ObservableProperty]
    private double _profileStickerTopMargin = 12.7;

    [ObservableProperty]
    private double _profileStickerHorizontalGap;

    [ObservableProperty]
    private double _profileStickerVerticalGap;

    [ObservableProperty]
    private int _profileStickersColumns = 3;

    [ObservableProperty]
    private int _profileStickersRows = 10;

    [ObservableProperty]
    private double _headerWidth;

    [ObservableProperty]
    private double _headerHeight;

    [ObservableProperty]
    private double _headerLeftMargin;

    [ObservableProperty]
    private double _headerTopMargin;

    [ObservableProperty]
    private bool _isHeaderLoaded;

    private bool _isFifoInventoryWriteOff;
    private bool _checkMaterialAvailabilityOnAutoWriteOff;
    private string _dbfExportDuplicateCheckField = "Номер заказа";
    private bool _preventDbfExportWithoutOrderNumber = true;
    private bool _useAlternativeDeleteInDbfExport;
    private bool _enableDatabaseLogging;
    private bool _generateMaterialRequestsWithoutPrices;
    private bool _remindDatabaseCompression = true;
    private bool _autoRecalculateOrderOnSaveOrPrint = true;
    private bool _autoUpdateOrdersByTemplateByDefault;
    private bool _restrictMosquitoNetToSashOnly = true;
    private bool _impostConnectorOriginAtFrameEdge;
    private bool _lockOrderOnExternalStockExport;
    private bool _autoChangeTemplateOnDealerChange;
    private bool _setAgreedAmountOnContractEnd = true;
    private bool _lockOrderModificationsOnContractEnd = true;
    private bool _autoWriteOffFromInternalStockOnMaterialWriteOffEnd = true;
    private bool _autoExportToExternalStockOnMaterialWriteOffEnd;
    private ExternalUtilityItem? _selectedExternalUtility;

    public bool IsFifoInventoryWriteOff
    {
        get => _isFifoInventoryWriteOff;
        set => SetProperty(ref _isFifoInventoryWriteOff, value);
    }

    public bool CheckMaterialAvailabilityOnAutoWriteOff
    {
        get => _checkMaterialAvailabilityOnAutoWriteOff;
        set => SetProperty(ref _checkMaterialAvailabilityOnAutoWriteOff, value);
    }

    public string DbfExportDuplicateCheckField
    {
        get => _dbfExportDuplicateCheckField;
        set => SetProperty(ref _dbfExportDuplicateCheckField, value);
    }

    public bool PreventDbfExportWithoutOrderNumber
    {
        get => _preventDbfExportWithoutOrderNumber;
        set => SetProperty(ref _preventDbfExportWithoutOrderNumber, value);
    }

    public bool UseAlternativeDeleteInDbfExport
    {
        get => _useAlternativeDeleteInDbfExport;
        set => SetProperty(ref _useAlternativeDeleteInDbfExport, value);
    }

    public bool EnableDatabaseLogging
    {
        get => _enableDatabaseLogging;
        set => SetProperty(ref _enableDatabaseLogging, value);
    }

    public bool GenerateMaterialRequestsWithoutPrices
    {
        get => _generateMaterialRequestsWithoutPrices;
        set => SetProperty(ref _generateMaterialRequestsWithoutPrices, value);
    }

    public bool RemindDatabaseCompression
    {
        get => _remindDatabaseCompression;
        set => SetProperty(ref _remindDatabaseCompression, value);
    }

    public bool AutoRecalculateOrderOnSaveOrPrint
    {
        get => _autoRecalculateOrderOnSaveOrPrint;
        set => SetProperty(ref _autoRecalculateOrderOnSaveOrPrint, value);
    }

    public bool AutoUpdateOrdersByTemplateByDefault
    {
        get => _autoUpdateOrdersByTemplateByDefault;
        set => SetProperty(ref _autoUpdateOrdersByTemplateByDefault, value);
    }

    public bool RestrictMosquitoNetToSashOnly
    {
        get => _restrictMosquitoNetToSashOnly;
        set => SetProperty(ref _restrictMosquitoNetToSashOnly, value);
    }

    public bool ImpostConnectorOriginAtFrameEdge
    {
        get => _impostConnectorOriginAtFrameEdge;
        set => SetProperty(ref _impostConnectorOriginAtFrameEdge, value);
    }

    public bool LockOrderOnExternalStockExport
    {
        get => _lockOrderOnExternalStockExport;
        set => SetProperty(ref _lockOrderOnExternalStockExport, value);
    }

    public bool AutoChangeTemplateOnDealerChange
    {
        get => _autoChangeTemplateOnDealerChange;
        set => SetProperty(ref _autoChangeTemplateOnDealerChange, value);
    }

    public bool SetAgreedAmountOnContractEnd
    {
        get => _setAgreedAmountOnContractEnd;
        set => SetProperty(ref _setAgreedAmountOnContractEnd, value);
    }

    public bool LockOrderModificationsOnContractEnd
    {
        get => _lockOrderModificationsOnContractEnd;
        set => SetProperty(ref _lockOrderModificationsOnContractEnd, value);
    }

    public bool AutoWriteOffFromInternalStockOnMaterialWriteOffEnd
    {
        get => _autoWriteOffFromInternalStockOnMaterialWriteOffEnd;
        set => SetProperty(ref _autoWriteOffFromInternalStockOnMaterialWriteOffEnd, value);
    }

    public bool AutoExportToExternalStockOnMaterialWriteOffEnd
    {
        get => _autoExportToExternalStockOnMaterialWriteOffEnd;
        set => SetProperty(ref _autoExportToExternalStockOnMaterialWriteOffEnd, value);
    }

    public ExternalUtilityItem? SelectedExternalUtility
    {
        get => _selectedExternalUtility;
        set
        {
            if (SetProperty(ref _selectedExternalUtility, value))
            {
                EditUtilityCommand.NotifyCanExecuteChanged();
                DeleteUtilityCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<ExternalUtilityItem> ExternalUtilities { get; } = [];

    public IAsyncRelayCommand AddUtilityCommand { get; }

    public IAsyncRelayCommand EditUtilityCommand { get; }

    public IRelayCommand DeleteUtilityCommand { get; }

    public IBrush SelectedElementsBrush => CreateBrush(SelectedElementsColor);

    public string HeaderStatusText => IsHeaderLoaded
        ? "Состояние: Шапка загружена"
        : "Состояние: Шапка НЕ загружена (SHAP.BMP)";

    partial void OnSelectedElementsColorChanged(string value) =>
        OnPropertyChanged(nameof(SelectedElementsBrush));

    partial void OnIsHeaderLoadedChanged(bool value) =>
        OnPropertyChanged(nameof(HeaderStatusText));

    [RelayCommand]
    private void Save()
    {
        _settingsService.Save(BuildModel());
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();

    private async Task ChangeOrderNumberingAsync()
    {
        if (OwnerWindow is null)
        {
            return;
        }

        var dialogViewModel = new InputNumberDialogViewModel
        {
            Title = "Изменить нумерацию заказов",
            Prompt = "Введите номер, с которого начнётся нумерация следующих новых заказов в Оше:",
            Value = NextOrderNumber,
            Minimum = 1
        };

        var dialog = new InputNumberDialog(dialogViewModel);
        var result = await dialog.ShowDialog<int?>(OwnerWindow);
        if (result is null or < 1)
        {
            return;
        }

        if (!IsCustomOrderNumbering)
        {
            OriginalNextOrderNumber = NextOrderNumber;
            IsCustomOrderNumbering = true;
        }

        NextOrderNumber = result.Value;
        _settingsService.Save(BuildModel());
    }

    private void RestoreOrderNumbering()
    {
        if (!IsCustomOrderNumbering)
        {
            return;
        }

        NextOrderNumber = OriginalNextOrderNumber;
        IsCustomOrderNumbering = false;
        _settingsService.Save(BuildModel());
    }

    private void CompressAllOrders()
    {
        _messageService.LogInfo("Запущено пересохранение всех заказов в сжатом формате.");
    }

    [RelayCommand]
    private async Task LoadHeaderAsync()
    {
        if (OwnerWindow?.StorageProvider is not { } storage)
        {
            return;
        }

        var imageFilter = new FilePickerFileType("Изображения BMP")
        {
            Patterns = ["*.bmp", "*.BMP"],
            MimeTypes = ["image/bmp"]
        };

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите файл шапки SHAP.BMP",
            AllowMultiple = false,
            FileTypeFilter = [imageFilter]
        });

        if (files.Count == 0)
        {
            return;
        }

        var path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _settingsService.LoadHeaderImage(path);
        IsHeaderLoaded = true;
    }

    private async Task AddUtilityAsync()
    {
        var item = await ShowUtilityEditorAsync(null, isNew: true);
        if (item is null)
        {
            return;
        }

        ExternalUtilities.Add(item);
        RenumberUtilities();
        SelectedExternalUtility = item;
        EditUtilityCommand.NotifyCanExecuteChanged();
        DeleteUtilityCommand.NotifyCanExecuteChanged();
    }

    private async Task EditUtilityAsync()
    {
        if (SelectedExternalUtility is null)
        {
            return;
        }

        var item = await ShowUtilityEditorAsync(SelectedExternalUtility, isNew: false);
        if (item is null)
        {
            return;
        }

        var index = ExternalUtilities.IndexOf(SelectedExternalUtility);
        if (index < 0)
        {
            return;
        }

        item.Id = SelectedExternalUtility.Id;
        item.Number = SelectedExternalUtility.Number;
        ExternalUtilities[index] = item;
        SelectedExternalUtility = item;
    }

    private void DeleteUtility()
    {
        if (SelectedExternalUtility is null)
        {
            return;
        }

        ExternalUtilities.Remove(SelectedExternalUtility);
        SelectedExternalUtility = null;
        RenumberUtilities();
        EditUtilityCommand.NotifyCanExecuteChanged();
        DeleteUtilityCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task PickBackupPathAsync()
    {
        if (OwnerWindow?.StorageProvider is not { } storage)
        {
            return;
        }

        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Выберите папку для копий архивов",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            BackupPath = folders[0].Path.LocalPath;
        }
    }

    [RelayCommand]
    private async Task PickSelectedColorAsync()
    {
        if (PickColorHexAsync is null)
        {
            return;
        }

        var hex = await PickColorHexAsync();
        if (!string.IsNullOrWhiteSpace(hex))
        {
            SelectedElementsColor = hex;
        }
    }

    private void ApplyFromModel(Models.Settings.EnvironmentSettings source)
    {
        CompanyName = source.CompanyName;
        LegalAddress = source.LegalAddress;
        CheckingAccount = source.CheckingAccount;
        Mfo = source.Mfo;
        OkpoCode = source.OkpoCode;
        BankName = source.BankName;
        CalculatorPath = source.CalculatorPath;
        BackupPath = source.BackupPath;
        NationalCurrency = source.NationalCurrency;
        OrderViewCurrency = source.OrderViewCurrency;
        VatPercent = source.VatPercent;
        SelectedVatModeOption = VatModeOptions.FirstOrDefault(option => option.Value == source.VatMode)
            ?? VatModeOptions.First();
        ImpostStepMm = source.ImpostStepMm;
        ArchiveDays = source.ArchiveDays;
        PriceAccuracy = source.PriceAccuracy;
        InterfaceLanguage = source.InterfaceLanguage;
        IsOrderBackgroundGray = source.IsOrderBackgroundGray;
        IsCustomFontEnabled = source.IsCustomFontEnabled;
        FontSize = source.FontSize;
        SelectedElementsColor = source.SelectedElementsColor;
        SelectedProfileCutLevelOption = ProfileCutLevelOptions.FirstOrDefault(option => option.Value == source.ProfileCutLevel)
            ?? ProfileCutLevelOptions.Last();
        SelectedProfileWasteModeOption = ProfileWasteModeOptions.FirstOrDefault(option => option.Value == source.ProfileWasteMode)
            ?? ProfileWasteModeOptions.First();
        AvoidDeadZoneCuts = source.AvoidDeadZoneCuts;
        ParallelCutTwoBars = source.ParallelCutTwoBars;
        MinGlassCutLengthMm = source.MinGlassCutLengthMm;
        MinGlassCutAreaM2 = source.MinGlassCutAreaM2;
        SelectedGlassWasteModeOption = GlassWasteModeOptions.FirstOrDefault(option => option.Value == source.GlassWasteMode)
            ?? GlassWasteModeOptions.First();
        CentralOfficeEmail = source.CentralOfficeEmail;
        OwnEmail = source.OwnEmail;
        SmtpServer = source.SmtpServer;
        SmtpPort = source.SmtpPort;
        Pop3Server = source.Pop3Server;
        Pop3Port = source.Pop3Port;
        MailUser = source.MailUser;
        MailPassword = source.MailPassword;
        StandardLineWidth = source.StandardLineWidth;
        DaysToProduction = source.DaysToProduction;
        DaysToInstallation = source.DaysToInstallation;
        DealerExpiryWarningDays = source.DealerExpiryWarningDays;
        CalculationWarehouseName = source.CalculationWarehouseName;
        BranchCode = source.BranchCode;
        OrderNumberSeparator = source.OrderNumberSeparator;
        IncludeBranchCodeInOrderNumber = source.IncludeBranchCodeInOrderNumber;
        ExportImageWidth = source.ExportImageWidth;
        ExportImageHeight = source.ExportImageHeight;
        ExportImageDpi = source.ExportImageDpi;
        ExportImageFormat = source.ExportImageFormat;
        InfoField1Name = source.InfoField1Name;
        InfoField2Name = source.InfoField2Name;
        InfoField3Name = source.InfoField3Name;
        InfoField4Name = source.InfoField4Name;
        NextOrderNumber = source.NextOrderNumber;
        OriginalNextOrderNumber = source.OriginalNextOrderNumber;
        IsCustomOrderNumbering = source.IsCustomOrderNumbering;
        GlassStickerWidth = source.GlassStickerWidth;
        GlassStickerHeight = source.GlassStickerHeight;
        GlassStickerFrameWidth = source.GlassStickerFrameWidth;
        GlassStickerFrameHeight = source.GlassStickerFrameHeight;
        GlassStickerLeftMargin = source.GlassStickerLeftMargin;
        GlassStickerTopMargin = source.GlassStickerTopMargin;
        GlassStickerHorizontalGap = source.GlassStickerHorizontalGap;
        GlassStickerVerticalGap = source.GlassStickerVerticalGap;
        GlassStickersColumns = source.GlassStickersColumns;
        GlassStickersRows = source.GlassStickersRows;
        ProfileStickerWidth = source.ProfileStickerWidth;
        ProfileStickerHeight = source.ProfileStickerHeight;
        ProfileStickerFrameWidth = source.ProfileStickerFrameWidth;
        ProfileStickerFrameHeight = source.ProfileStickerFrameHeight;
        ProfileStickerLeftMargin = source.ProfileStickerLeftMargin;
        ProfileStickerTopMargin = source.ProfileStickerTopMargin;
        ProfileStickerHorizontalGap = source.ProfileStickerHorizontalGap;
        ProfileStickerVerticalGap = source.ProfileStickerVerticalGap;
        ProfileStickersColumns = source.ProfileStickersColumns;
        ProfileStickersRows = source.ProfileStickersRows;
        HeaderWidth = source.HeaderWidth;
        HeaderHeight = source.HeaderHeight;
        HeaderLeftMargin = source.HeaderLeftMargin;
        HeaderTopMargin = source.HeaderTopMargin;
        IsHeaderLoaded = source.IsHeaderLoaded;
        IsFifoInventoryWriteOff = source.IsFifoInventoryWriteOff;
        CheckMaterialAvailabilityOnAutoWriteOff = source.CheckMaterialAvailabilityOnAutoWriteOff;
        DbfExportDuplicateCheckField = source.DbfExportDuplicateCheckField;
        PreventDbfExportWithoutOrderNumber = source.PreventDbfExportWithoutOrderNumber;
        UseAlternativeDeleteInDbfExport = source.UseAlternativeDeleteInDbfExport;
        EnableDatabaseLogging = source.EnableDatabaseLogging;
        GenerateMaterialRequestsWithoutPrices = source.GenerateMaterialRequestsWithoutPrices;
        RemindDatabaseCompression = source.RemindDatabaseCompression;
        AutoRecalculateOrderOnSaveOrPrint = source.AutoRecalculateOrderOnSaveOrPrint;
        AutoUpdateOrdersByTemplateByDefault = source.AutoUpdateOrdersByTemplateByDefault;
        RestrictMosquitoNetToSashOnly = source.RestrictMosquitoNetToSashOnly;
        ImpostConnectorOriginAtFrameEdge = source.ImpostConnectorOriginAtFrameEdge;
        LockOrderOnExternalStockExport = source.LockOrderOnExternalStockExport;
        AutoChangeTemplateOnDealerChange = source.AutoChangeTemplateOnDealerChange;
        SetAgreedAmountOnContractEnd = source.SetAgreedAmountOnContractEnd;
        LockOrderModificationsOnContractEnd = source.LockOrderModificationsOnContractEnd;
        AutoWriteOffFromInternalStockOnMaterialWriteOffEnd = source.AutoWriteOffFromInternalStockOnMaterialWriteOffEnd;
        AutoExportToExternalStockOnMaterialWriteOffEnd = source.AutoExportToExternalStockOnMaterialWriteOffEnd;
        ExternalUtilities.Clear();
        foreach (var utility in source.ExternalUtilities.OrderBy(item => item.Number))
        {
            ExternalUtilities.Add(utility.Clone());
        }

        RenumberUtilities();
        SelectedExternalUtility = null;
    }

    private Models.Settings.EnvironmentSettings BuildModel() => new()
    {
        CompanyName = CompanyName.Trim(),
        LegalAddress = LegalAddress.Trim(),
        CheckingAccount = CheckingAccount.Trim(),
        Mfo = Mfo.Trim(),
        OkpoCode = OkpoCode.Trim(),
        BankName = BankName.Trim(),
        CalculatorPath = CalculatorPath.Trim(),
        BackupPath = BackupPath.Trim(),
        NationalCurrency = NationalCurrency,
        OrderViewCurrency = OrderViewCurrency,
        VatPercent = VatPercent,
        VatMode = SelectedVatModeOption?.Value ?? VatMode.DoNotUse,
        ImpostStepMm = ImpostStepMm,
        ArchiveDays = ArchiveDays,
        PriceAccuracy = PriceAccuracy,
        InterfaceLanguage = InterfaceLanguage,
        IsOrderBackgroundGray = IsOrderBackgroundGray,
        IsCustomFontEnabled = IsCustomFontEnabled,
        FontSize = FontSize,
        SelectedElementsColor = SelectedElementsColor,
        ProfileCutLevel = SelectedProfileCutLevelOption?.Value ?? ProfileCutLevel.Complex,
        ProfileWasteMode = SelectedProfileWasteModeOption?.Value ?? ProfileWasteMode.DoNotUse,
        AvoidDeadZoneCuts = AvoidDeadZoneCuts,
        ParallelCutTwoBars = ParallelCutTwoBars,
        MinGlassCutLengthMm = MinGlassCutLengthMm,
        MinGlassCutAreaM2 = MinGlassCutAreaM2,
        GlassWasteMode = SelectedGlassWasteModeOption?.Value ?? GlassWasteMode.DoNotUse,
        CentralOfficeEmail = CentralOfficeEmail.Trim(),
        OwnEmail = OwnEmail.Trim(),
        SmtpServer = SmtpServer.Trim(),
        SmtpPort = SmtpPort,
        Pop3Server = Pop3Server.Trim(),
        Pop3Port = Pop3Port,
        MailUser = MailUser.Trim(),
        MailPassword = MailPassword,
        StandardLineWidth = StandardLineWidth,
        DaysToProduction = DaysToProduction,
        DaysToInstallation = DaysToInstallation,
        DealerExpiryWarningDays = DealerExpiryWarningDays,
        CalculationWarehouseName = CalculationWarehouseName.Trim(),
        BranchCode = BranchCode.Trim(),
        OrderNumberSeparator = OrderNumberSeparator,
        IncludeBranchCodeInOrderNumber = IncludeBranchCodeInOrderNumber,
        ExportImageWidth = ExportImageWidth,
        ExportImageHeight = ExportImageHeight,
        ExportImageDpi = ExportImageDpi,
        ExportImageFormat = ExportImageFormat,
        InfoField1Name = InfoField1Name.Trim(),
        InfoField2Name = InfoField2Name.Trim(),
        InfoField3Name = InfoField3Name.Trim(),
        InfoField4Name = InfoField4Name.Trim(),
        NextOrderNumber = NextOrderNumber,
        OriginalNextOrderNumber = OriginalNextOrderNumber,
        IsCustomOrderNumbering = IsCustomOrderNumbering,
        GlassStickerWidth = GlassStickerWidth,
        GlassStickerHeight = GlassStickerHeight,
        GlassStickerFrameWidth = GlassStickerFrameWidth,
        GlassStickerFrameHeight = GlassStickerFrameHeight,
        GlassStickerLeftMargin = GlassStickerLeftMargin,
        GlassStickerTopMargin = GlassStickerTopMargin,
        GlassStickerHorizontalGap = GlassStickerHorizontalGap,
        GlassStickerVerticalGap = GlassStickerVerticalGap,
        GlassStickersColumns = GlassStickersColumns,
        GlassStickersRows = GlassStickersRows,
        ProfileStickerWidth = ProfileStickerWidth,
        ProfileStickerHeight = ProfileStickerHeight,
        ProfileStickerFrameWidth = ProfileStickerFrameWidth,
        ProfileStickerFrameHeight = ProfileStickerFrameHeight,
        ProfileStickerLeftMargin = ProfileStickerLeftMargin,
        ProfileStickerTopMargin = ProfileStickerTopMargin,
        ProfileStickerHorizontalGap = ProfileStickerHorizontalGap,
        ProfileStickerVerticalGap = ProfileStickerVerticalGap,
        ProfileStickersColumns = ProfileStickersColumns,
        ProfileStickersRows = ProfileStickersRows,
        HeaderWidth = HeaderWidth,
        HeaderHeight = HeaderHeight,
        HeaderLeftMargin = HeaderLeftMargin,
        HeaderTopMargin = HeaderTopMargin,
        IsHeaderLoaded = IsHeaderLoaded,
        IsFifoInventoryWriteOff = IsFifoInventoryWriteOff,
        CheckMaterialAvailabilityOnAutoWriteOff = CheckMaterialAvailabilityOnAutoWriteOff,
        DbfExportDuplicateCheckField = DbfExportDuplicateCheckField,
        PreventDbfExportWithoutOrderNumber = PreventDbfExportWithoutOrderNumber,
        UseAlternativeDeleteInDbfExport = UseAlternativeDeleteInDbfExport,
        EnableDatabaseLogging = EnableDatabaseLogging,
        GenerateMaterialRequestsWithoutPrices = GenerateMaterialRequestsWithoutPrices,
        RemindDatabaseCompression = RemindDatabaseCompression,
        AutoRecalculateOrderOnSaveOrPrint = AutoRecalculateOrderOnSaveOrPrint,
        AutoUpdateOrdersByTemplateByDefault = AutoUpdateOrdersByTemplateByDefault,
        RestrictMosquitoNetToSashOnly = RestrictMosquitoNetToSashOnly,
        ImpostConnectorOriginAtFrameEdge = ImpostConnectorOriginAtFrameEdge,
        LockOrderOnExternalStockExport = LockOrderOnExternalStockExport,
        AutoChangeTemplateOnDealerChange = AutoChangeTemplateOnDealerChange,
        SetAgreedAmountOnContractEnd = SetAgreedAmountOnContractEnd,
        LockOrderModificationsOnContractEnd = LockOrderModificationsOnContractEnd,
        AutoWriteOffFromInternalStockOnMaterialWriteOffEnd = AutoWriteOffFromInternalStockOnMaterialWriteOffEnd,
        AutoExportToExternalStockOnMaterialWriteOffEnd = AutoExportToExternalStockOnMaterialWriteOffEnd,
        ExternalUtilities = ExternalUtilities.Select(item => item.Clone()).ToList()
    };

    private async Task<ExternalUtilityItem?> ShowUtilityEditorAsync(ExternalUtilityItem? source, bool isNew)
    {
        if (OwnerWindow is null)
        {
            return null;
        }

        var dialogViewModel = new ExternalUtilityEditDialogViewModel();
        dialogViewModel.Load(source, isNew);
        var dialog = new ExternalUtilityEditDialog(dialogViewModel);
        return await dialog.ShowDialog<ExternalUtilityItem?>(OwnerWindow);
    }

    private void RenumberUtilities()
    {
        for (var index = 0; index < ExternalUtilities.Count; index++)
        {
            ExternalUtilities[index].Number = index + 1;
        }
    }

    private static IBrush CreateBrush(string hex) =>
        Color.TryParse(hex, out var color)
            ? new SolidColorBrush(color)
            : Brushes.Transparent;
}
