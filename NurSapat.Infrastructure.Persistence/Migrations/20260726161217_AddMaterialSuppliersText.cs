using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSapat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialSuppliersText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SuppliersText",
                table: "materials",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuppliersText",
                table: "materials");
        }
    }
}
