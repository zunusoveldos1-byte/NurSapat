using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSapat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistenceCutover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExportFilePath",
                table: "orders",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualAddress",
                table: "customers",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "customers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "customers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DealerCode",
                table: "customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "customers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "order_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: true),
                    WindPressure = table.Column<double>(type: "double precision", nullable: false),
                    ProfitMargin = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    AdditionalExpensesPercent = table.Column<double>(type: "double precision", nullable: false),
                    AdditionalExpensesFixed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DismantlingPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallationPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    DefaultOrderNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DefaultProductionDays = table.Column<int>(type: "integer", nullable: false),
                    DefaultPrepaymentPercent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    ApplyDealerDiscount = table.Column<bool>(type: "boolean", nullable: false),
                    DealerDiscountPercent = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    GroupNumber = table.Column<int>(type: "integer", nullable: false),
                    PermissionsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_IsDeleted",
                table: "customers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_order_templates_Name",
                table: "order_templates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "order_templates");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropIndex(
                name: "IX_customers_IsDeleted",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "ExportFilePath",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ActualAddress",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "DealerCode",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "customers");
        }
    }
}
