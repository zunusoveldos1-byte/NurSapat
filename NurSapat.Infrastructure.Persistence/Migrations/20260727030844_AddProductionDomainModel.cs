using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSapat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve statuses written by the legacy sequential enum (0..4).
            migrationBuilder.Sql(
                """
                UPDATE orders
                SET "Status" = CASE "Status"
                    WHEN 1 THEN 10
                    WHEN 2 THEN 30
                    WHEN 3 THEN 60
                    WHEN 4 THEN 70
                    ELSE "Status"
                END
                WHERE "Status" BETWEEN 1 AND 4;
                """);

            migrationBuilder.CreateTable(
                name: "material_reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialArticle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_material_reservations_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "operation_routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false),
                    OperationName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LaborCostPerHour = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    StandardMinutesPerUnit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operation_routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "price_list_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_list_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "production_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "production_hardware_kits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SystemBrand = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MinSashWidth = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxSashWidth = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MinSashHeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxSashHeight = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ComponentsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_hardware_kits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "technology_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileSystemCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Article = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MinRadiusMm = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxSashWidthMm = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxSashHeightMm = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxSashWeightKg = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxInertiaIx = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technology_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "order_calculation_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ItemsSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    TotalCostPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalMarginPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    TotalDiscountPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_calculation_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_calculation_snapshots_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_calculation_snapshots_price_list_versions_PriceListVe~",
                        column: x => x.PriceListVersionId,
                        principalTable: "price_list_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_list_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialArticle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BaseRetailPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DealerPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_list_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_price_list_items_price_list_versions_PriceListVersionId",
                        column: x => x.PriceListVersionId,
                        principalTable: "price_list_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cutting_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialType = table.Column<int>(type: "integer", nullable: false),
                    CuttingDataJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsExecuted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cutting_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cutting_jobs_production_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "production_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_postings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialArticle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_postings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_postings_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_postings_production_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "production_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cutting_jobs_BatchId",
                table: "cutting_jobs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_material_reservations_MaterialArticle",
                table: "material_reservations",
                column: "MaterialArticle");

            migrationBuilder.CreateIndex(
                name: "IX_material_reservations_OrderId",
                table: "material_reservations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_operation_routes_Name_SequenceOrder",
                table: "operation_routes",
                columns: new[] { "Name", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_calculation_snapshots_OrderId",
                table: "order_calculation_snapshots",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_calculation_snapshots_OrderId_SnapshotDate",
                table: "order_calculation_snapshots",
                columns: new[] { "OrderId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_order_calculation_snapshots_PriceListVersionId",
                table: "order_calculation_snapshots",
                column: "PriceListVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_MaterialArticle",
                table: "price_list_items",
                column: "MaterialArticle");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_PriceListVersionId_MaterialArticle",
                table: "price_list_items",
                columns: new[] { "PriceListVersionId", "MaterialArticle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_list_versions_IsActive_EffectiveFrom",
                table: "price_list_versions",
                columns: new[] { "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_production_batches_BatchNumber",
                table: "production_batches",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_batches_PlannedStartDate",
                table: "production_batches",
                column: "PlannedStartDate");

            migrationBuilder.CreateIndex(
                name: "IX_stock_postings_BatchId",
                table: "stock_postings",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_postings_MaterialArticle",
                table: "stock_postings",
                column: "MaterialArticle");

            migrationBuilder.CreateIndex(
                name: "IX_stock_postings_OrderId",
                table: "stock_postings",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_technology_rules_Article",
                table: "technology_rules",
                column: "Article");

            migrationBuilder.CreateIndex(
                name: "IX_technology_rules_ProfileSystemCode_Article",
                table: "technology_rules",
                columns: new[] { "ProfileSystemCode", "Article" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE orders
                SET "Status" = CASE "Status"
                    WHEN 10 THEN 1
                    WHEN 20 THEN 1
                    WHEN 30 THEN 2
                    WHEN 40 THEN 2
                    WHEN 50 THEN 3
                    WHEN 60 THEN 3
                    WHEN 70 THEN 4
                    WHEN 99 THEN 0
                    ELSE "Status"
                END;
                """);

            migrationBuilder.DropTable(
                name: "cutting_jobs");

            migrationBuilder.DropTable(
                name: "material_reservations");

            migrationBuilder.DropTable(
                name: "operation_routes");

            migrationBuilder.DropTable(
                name: "order_calculation_snapshots");

            migrationBuilder.DropTable(
                name: "price_list_items");

            migrationBuilder.DropTable(
                name: "production_hardware_kits");

            migrationBuilder.DropTable(
                name: "stock_postings");

            migrationBuilder.DropTable(
                name: "technology_rules");

            migrationBuilder.DropTable(
                name: "price_list_versions");

            migrationBuilder.DropTable(
                name: "production_batches");
        }
    }
}
