using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSapat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Turnover = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Debt = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OkpoCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BankName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CheckingAccount = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Mfo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LegalAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Article = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Section = table.Column<int>(type: "integer", nullable: false),
                    MainUnit = table.Column<int>(type: "integer", nullable: false),
                    LinearGroup = table.Column<int>(type: "integer", nullable: true),
                    SquareGroup = table.Column<int>(type: "integer", nullable: true),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WastePercent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    PrintOnAssemblyDrawing = table.Column<bool>(type: "boolean", nullable: false),
                    PrintOnCommercialOffer = table.Column<bool>(type: "boolean", nullable: false),
                    PrintOnShippingSheet = table.Column<bool>(type: "boolean", nullable: false),
                    GroupPrintPositions = table.Column<bool>(type: "boolean", nullable: false),
                    IsWarmProfile = table.Column<bool>(type: "boolean", nullable: false),
                    InnerCoatingAreaPerMeter = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OuterCoatingAreaPerMeter = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    EnableLengthCutOptimization = table.Column<bool>(type: "boolean", nullable: false),
                    BarLengthMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IlliquidThresholdMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ScrapMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    EnablePlaneCutOptimization = table.Column<bool>(type: "boolean", nullable: false),
                    SheetWidthMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SheetHeightMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GlassTint = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    MinimumCalculatedAreaSqM = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UseSecondaryUnit = table.Column<bool>(type: "boolean", nullable: false),
                    SecondaryUnitCoefficient = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    PriceByKilogram = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_materials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stock_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    UseCustomExchangeRates = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LegalAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContractTerms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfitabilityPercent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DealerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentPriceMode = table.Column<int>(type: "integer", nullable: false),
                    TotalDiscountPercent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    TotalDiscountSum = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SiteAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DealerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OrderManagerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CustomerCity = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CustomerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContractDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProductionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InstallationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HasDelivery = table.Column<bool>(type: "boolean", nullable: false),
                    HasInstallation = table.Column<bool>(type: "boolean", nullable: false),
                    ProfileNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ProductionNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ConstructionsSubtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount2Percent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    DeliveryCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdditionalMaterialsCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdditionalMaterialsInstallationCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatPercent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    VatMode = table.Column<int>(type: "integer", nullable: false),
                    AgreedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentCurrencyCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    SelectedConstructionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orders_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "construction_elements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<int>(type: "integer", nullable: false),
                    WidthMm = table.Column<double>(type: "double precision", nullable: false),
                    HeightMm = table.Column<double>(type: "double precision", nullable: false),
                    WallThicknessMm = table.Column<double>(type: "double precision", nullable: false),
                    Article = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WeightKg = table.Column<double>(type: "double precision", nullable: false),
                    GlassFormula = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GlassThicknessMm = table.Column<double>(type: "double precision", nullable: false),
                    GlassStructure = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_construction_elements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_construction_elements_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_color_prices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColorArticle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ColorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    LinkArticle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    UseCustomCutLength = table.Column<bool>(type: "boolean", nullable: false),
                    CustomBarLengthMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_color_prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_material_color_prices_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_dimension_prices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    WidthMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    HeightMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_dimension_prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_material_dimension_prices_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_document_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Article = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Color = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_document_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_document_lines_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_document_lines_stock_documents_StockDocumentId",
                        column: x => x.StockDocumentId,
                        principalTable: "stock_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "finished_goods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CellAddress = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    DateIncoming = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_finished_goods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_finished_goods_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_constructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsSelectedForCalculation = table.Column<bool>(type: "boolean", nullable: false),
                    OuterWidthMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OuterHeightMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DrawingJson = table.Column<string>(type: "text", nullable: false),
                    GlazingAreaSqM = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ProductAreaSqM = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ManufacturingTimeTicks = table.Column<long>(type: "bigint", nullable: false),
                    ProfileCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ComponentsCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SealantCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GlazingCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    HardwareCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallationMaterialsCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Expenses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpensesMode = table.Column<int>(type: "integer", nullable: false),
                    LaborCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaterialsWithoutMarkup = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProfitabilityPercent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    AdditionalExpensesPercent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    AdditionalExpensesFixed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount1Percent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    DemolitionCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallationCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ProfileSystemName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ColorName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ChangeProfile = table.Column<bool>(type: "boolean", nullable: false),
                    ChangeImposts = table.Column<bool>(type: "boolean", nullable: false),
                    ChangeSashes = table.Column<bool>(type: "boolean", nullable: false),
                    ChangeLayout = table.Column<bool>(type: "boolean", nullable: false),
                    ChangeDecorativeElements = table.Column<bool>(type: "boolean", nullable: false),
                    ManufactureDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Manager = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Supervisor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_constructions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_constructions_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "construction_element_series",
                columns: table => new
                {
                    ConstructionElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_construction_element_series", x => new { x.ConstructionElementId, x.SeriesId });
                    table.ForeignKey(
                        name: "FK_construction_element_series_construction_elements_Construct~",
                        column: x => x.ConstructionElementId,
                        principalTable: "construction_elements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hardware_auto_pick_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstructionElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinWidthFalzMm = table.Column<double>(type: "double precision", nullable: false),
                    MaxWidthFalzMm = table.Column<double>(type: "double precision", nullable: false),
                    MinHeightFalzMm = table.Column<double>(type: "double precision", nullable: false),
                    MaxHeightFalzMm = table.Column<double>(type: "double precision", nullable: false),
                    ScissorsCount = table.Column<int>(type: "integer", nullable: false),
                    MiddleLockCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hardware_auto_pick_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hardware_auto_pick_rules_construction_elements_Construction~",
                        column: x => x.ConstructionElementId,
                        principalTable: "construction_elements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cost_specification_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderConstructionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstructionMaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    MaterialName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Article = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WastePercent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cost_specification_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cost_specification_lines_order_constructions_OrderConstruct~",
                        column: x => x.OrderConstructionId,
                        principalTable: "order_constructions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "geometry_elements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderConstructionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    X = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Y = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WidthMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    HeightMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OpeningType = table.Column<int>(type: "integer", nullable: true),
                    HingeSide = table.Column<int>(type: "integer", nullable: true),
                    HasHardware = table.Column<bool>(type: "boolean", nullable: false),
                    HasHandle = table.Column<bool>(type: "boolean", nullable: false),
                    HasMosquitoNet = table.Column<bool>(type: "boolean", nullable: false),
                    HostSashId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentGlazingId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpokeType = table.Column<int>(type: "integer", nullable: true),
                    SpokeThicknessMm = table.Column<double>(type: "double precision", nullable: true),
                    SpokeColor = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SpokeRowsCount = table.Column<int>(type: "integer", nullable: true),
                    SpokeColumnsCount = table.Column<int>(type: "integer", nullable: true),
                    SpokeRayCount = table.Column<int>(type: "integer", nullable: true),
                    SpokeIsArched = table.Column<bool>(type: "boolean", nullable: false),
                    SpokeArcCenterX = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    SpokeArcCenterY = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    SpokeArcRadius = table.Column<double>(type: "double precision", nullable: true),
                    ProfileSeriesId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceToolId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EndX = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    EndY = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IsExtremeImpost = table.Column<bool>(type: "boolean", nullable: false),
                    EdgeAlignment = table.Column<int>(type: "integer", nullable: true),
                    ExtremeImpostOffsetMm = table.Column<double>(type: "double precision", nullable: true),
                    HostFrameNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtremeImpostSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    FillType = table.Column<int>(type: "integer", nullable: true),
                    Shape = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geometry_elements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_geometry_elements_order_constructions_OrderConstructionId",
                        column: x => x.OrderConstructionId,
                        principalTable: "order_constructions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_construction_elements_Article",
                table: "construction_elements",
                column: "Article");

            migrationBuilder.CreateIndex(
                name: "IX_construction_elements_MaterialId",
                table: "construction_elements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_cost_specification_lines_MaterialId",
                table: "cost_specification_lines",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_cost_specification_lines_OrderConstructionId",
                table: "cost_specification_lines",
                column: "OrderConstructionId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_FullName",
                table: "customers",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Phone",
                table: "customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_finished_goods_OrderId",
                table: "finished_goods",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_finished_goods_ProductCode",
                table: "finished_goods",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_geometry_elements_MaterialId",
                table: "geometry_elements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_geometry_elements_OrderConstructionId",
                table: "geometry_elements",
                column: "OrderConstructionId");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_auto_pick_rules_ConstructionElementId",
                table: "hardware_auto_pick_rules",
                column: "ConstructionElementId");

            migrationBuilder.CreateIndex(
                name: "IX_material_color_prices_MaterialId",
                table: "material_color_prices",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_material_dimension_prices_MaterialId",
                table: "material_dimension_prices",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_materials_Article",
                table: "materials",
                column: "Article",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_materials_Name",
                table: "materials",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_order_constructions_OrderId_Number",
                table: "order_constructions",
                columns: new[] { "OrderId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_CustomerId",
                table: "orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderDate",
                table: "orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderNumber",
                table: "orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_document_lines_MaterialId",
                table: "stock_document_lines",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_document_lines_StockDocumentId",
                table: "stock_document_lines",
                column: "StockDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_documents_Date",
                table: "stock_documents",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_stock_documents_Number",
                table: "stock_documents",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_Name",
                table: "suppliers",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "construction_element_series");

            migrationBuilder.DropTable(
                name: "cost_specification_lines");

            migrationBuilder.DropTable(
                name: "finished_goods");

            migrationBuilder.DropTable(
                name: "geometry_elements");

            migrationBuilder.DropTable(
                name: "hardware_auto_pick_rules");

            migrationBuilder.DropTable(
                name: "material_color_prices");

            migrationBuilder.DropTable(
                name: "material_dimension_prices");

            migrationBuilder.DropTable(
                name: "stock_document_lines");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "order_constructions");

            migrationBuilder.DropTable(
                name: "construction_elements");

            migrationBuilder.DropTable(
                name: "stock_documents");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "materials");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
