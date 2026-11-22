using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NurSapat.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NurSapatDbContext))]
[Migration("20261121010400_StoreOrderWorkspace")]
public sealed class StoreOrderWorkspace : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(
            name: "WorkspaceStateJson", table: "orders", type: "jsonb", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "WorkspaceStateJson", table: "orders");
}
