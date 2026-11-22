using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSapat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currency_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    RateDate = table.Column<DateTime>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency_rates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_currency_rates_CurrencyCode_RateDate",
                table: "currency_rates",
                columns: new[] { "CurrencyCode", "RateDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_currency_rates_RateDate",
                table: "currency_rates",
                column: "RateDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "currency_rates");
        }
    }
}
