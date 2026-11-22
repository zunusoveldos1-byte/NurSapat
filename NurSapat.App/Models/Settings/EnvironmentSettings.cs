namespace NurSapat.App.Models.Settings;

public sealed class EnvironmentSettings
{
    public string CompanyName { get; set; } = string.Empty;

    public string LegalAddress { get; set; } = string.Empty;

    public string CheckingAccount { get; set; } = string.Empty;

    public string Mfo { get; set; } = string.Empty;

    public string OkpoCode { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string CalculatorPath { get; set; } = string.Empty;

    public string BackupPath { get; set; } = string.Empty;

    public string NationalCurrency { get; set; } = "KGS";

    public string OrderViewCurrency { get; set; } = "KGS";

    public decimal VatPercent { get; set; } = 20m;

    public VatMode VatMode { get; set; } = VatMode.DoNotUse;

    public double ImpostStepMm { get; set; } = 10;

    public int ArchiveDays { get; set; } = 5;

    public int PriceAccuracy { get; set; } = 2;

    public string InterfaceLanguage { get; set; } = "Русский";

    public bool IsOrderBackgroundGray { get; set; }

    public bool IsCustomFontEnabled { get; set; }

    public double FontSize { get; set; } = 12;

    public string SelectedElementsColor { get; set; } = "#2563EB";

    public ProfileCutLevel ProfileCutLevel { get; set; } = ProfileCutLevel.Complex;

    public ProfileWasteMode ProfileWasteMode { get; set; } = ProfileWasteMode.OffcutsFirstThenWhole;

    public bool AvoidDeadZoneCuts { get; set; }

    public bool ParallelCutTwoBars { get; set; }

    public double MinGlassCutLengthMm { get; set; } = 150;

    public double MinGlassCutAreaM2 { get; set; } = 0.05;

    public GlassWasteMode GlassWasteMode { get; set; } = GlassWasteMode.DoNotUse;

    public string CentralOfficeEmail { get; set; } = string.Empty;

    public string OwnEmail { get; set; } = string.Empty;

    public string SmtpServer { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 465;

    public string Pop3Server { get; set; } = string.Empty;

    public int Pop3Port { get; set; } = 995;

    public string MailUser { get; set; } = string.Empty;

    public string MailPassword { get; set; } = string.Empty;

    public int StandardLineWidth { get; set; } = 3;

    public int DaysToProduction { get; set; } = 3;

    public int DaysToInstallation { get; set; } = 7;

    public int DealerExpiryWarningDays { get; set; } = 3;

    public string CalculationWarehouseName { get; set; } = string.Empty;

    public string BranchCode { get; set; } = string.Empty;

    public string OrderNumberSeparator { get; set; } = "-";

    public bool IncludeBranchCodeInOrderNumber { get; set; }

    public int ExportImageWidth { get; set; } = 400;

    public int ExportImageHeight { get; set; } = 300;

    public int ExportImageDpi { get; set; } = 96;

    public string ExportImageFormat { get; set; } = "BMP";

    public string InfoField1Name { get; set; } = "Информация 1";

    public string InfoField2Name { get; set; } = "Информация 2";

    public string InfoField3Name { get; set; } = "Информация 3";

    public string InfoField4Name { get; set; } = "Информация 4";

    public int NextOrderNumber { get; set; } = 1;

    public int OriginalNextOrderNumber { get; set; } = 1;

    public bool IsCustomOrderNumbering { get; set; }

    public double GlassStickerWidth { get; set; } = 67.2;

    public double GlassStickerHeight { get; set; } = 21.2;

    public double GlassStickerFrameWidth { get; set; } = 66.0;

    public double GlassStickerFrameHeight { get; set; } = 20.0;

    public double GlassStickerLeftMargin { get; set; } = 3.6;

    public double GlassStickerTopMargin { get; set; } = 12.7;

    public double GlassStickerHorizontalGap { get; set; }

    public double GlassStickerVerticalGap { get; set; }

    public int GlassStickersColumns { get; set; } = 3;

    public int GlassStickersRows { get; set; } = 13;

    public double ProfileStickerWidth { get; set; } = 67.2;

    public double ProfileStickerHeight { get; set; } = 28.0;

    public double ProfileStickerFrameWidth { get; set; } = 66.0;

    public double ProfileStickerFrameHeight { get; set; } = 27.0;

    public double ProfileStickerLeftMargin { get; set; } = 3.6;

    public double ProfileStickerTopMargin { get; set; } = 12.7;

    public double ProfileStickerHorizontalGap { get; set; }

    public double ProfileStickerVerticalGap { get; set; }

    public int ProfileStickersColumns { get; set; } = 3;

    public int ProfileStickersRows { get; set; } = 10;

    public double HeaderWidth { get; set; }

    public double HeaderHeight { get; set; }

    public double HeaderLeftMargin { get; set; }

    public double HeaderTopMargin { get; set; }

    public bool IsHeaderLoaded { get; set; }

    public bool IsFifoInventoryWriteOff { get; set; }

    public bool CheckMaterialAvailabilityOnAutoWriteOff { get; set; }

    public string DbfExportDuplicateCheckField { get; set; } = "Номер заказа";

    public bool PreventDbfExportWithoutOrderNumber { get; set; } = true;

    public bool UseAlternativeDeleteInDbfExport { get; set; }

    public bool EnableDatabaseLogging { get; set; }

    public bool GenerateMaterialRequestsWithoutPrices { get; set; }

    public bool RemindDatabaseCompression { get; set; } = true;

    public bool AutoRecalculateOrderOnSaveOrPrint { get; set; } = true;

    public bool AutoUpdateOrdersByTemplateByDefault { get; set; }

    public bool RestrictMosquitoNetToSashOnly { get; set; } = true;

    public bool ImpostConnectorOriginAtFrameEdge { get; set; }

    public bool LockOrderOnExternalStockExport { get; set; }

    public bool AutoChangeTemplateOnDealerChange { get; set; }

    public bool SetAgreedAmountOnContractEnd { get; set; } = true;

    public bool LockOrderModificationsOnContractEnd { get; set; } = true;

    public bool AutoWriteOffFromInternalStockOnMaterialWriteOffEnd { get; set; } = true;

    public bool AutoExportToExternalStockOnMaterialWriteOffEnd { get; set; }

    public List<ExternalUtilityItem> ExternalUtilities { get; set; } = [];

    public EnvironmentSettings Clone() => new()
    {
        CompanyName = CompanyName,
        LegalAddress = LegalAddress,
        CheckingAccount = CheckingAccount,
        Mfo = Mfo,
        OkpoCode = OkpoCode,
        BankName = BankName,
        CalculatorPath = CalculatorPath,
        BackupPath = BackupPath,
        NationalCurrency = NationalCurrency,
        OrderViewCurrency = OrderViewCurrency,
        VatPercent = VatPercent,
        VatMode = VatMode,
        ImpostStepMm = ImpostStepMm,
        ArchiveDays = ArchiveDays,
        PriceAccuracy = PriceAccuracy,
        InterfaceLanguage = InterfaceLanguage,
        IsOrderBackgroundGray = IsOrderBackgroundGray,
        IsCustomFontEnabled = IsCustomFontEnabled,
        FontSize = FontSize,
        SelectedElementsColor = SelectedElementsColor,
        ProfileCutLevel = ProfileCutLevel,
        ProfileWasteMode = ProfileWasteMode,
        AvoidDeadZoneCuts = AvoidDeadZoneCuts,
        ParallelCutTwoBars = ParallelCutTwoBars,
        MinGlassCutLengthMm = MinGlassCutLengthMm,
        MinGlassCutAreaM2 = MinGlassCutAreaM2,
        GlassWasteMode = GlassWasteMode,
        CentralOfficeEmail = CentralOfficeEmail,
        OwnEmail = OwnEmail,
        SmtpServer = SmtpServer,
        SmtpPort = SmtpPort,
        Pop3Server = Pop3Server,
        Pop3Port = Pop3Port,
        MailUser = MailUser,
        MailPassword = MailPassword,
        StandardLineWidth = StandardLineWidth,
        DaysToProduction = DaysToProduction,
        DaysToInstallation = DaysToInstallation,
        DealerExpiryWarningDays = DealerExpiryWarningDays,
        CalculationWarehouseName = CalculationWarehouseName,
        BranchCode = BranchCode,
        OrderNumberSeparator = OrderNumberSeparator,
        IncludeBranchCodeInOrderNumber = IncludeBranchCodeInOrderNumber,
        ExportImageWidth = ExportImageWidth,
        ExportImageHeight = ExportImageHeight,
        ExportImageDpi = ExportImageDpi,
        ExportImageFormat = ExportImageFormat,
        InfoField1Name = InfoField1Name,
        InfoField2Name = InfoField2Name,
        InfoField3Name = InfoField3Name,
        InfoField4Name = InfoField4Name,
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
}
