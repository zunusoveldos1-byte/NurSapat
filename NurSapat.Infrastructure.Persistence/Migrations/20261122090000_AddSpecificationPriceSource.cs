using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NurSapat.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NurSapatDbContext))]
[Migration("20261122090000_AddSpecificationPriceSource")]
public sealed class AddSpecificationPriceSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "PriceSourceJson", table: "cost_specification_lines", type: "jsonb", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "PriceSourceJson", table: "cost_specification_lines");
}
