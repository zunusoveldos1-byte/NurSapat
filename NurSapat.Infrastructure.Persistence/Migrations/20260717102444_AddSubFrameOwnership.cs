using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSapat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubFrameOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HostGridLineId",
                table: "geometry_elements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubFrameId",
                table: "geometry_elements",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostGridLineId",
                table: "geometry_elements");

            migrationBuilder.DropColumn(
                name: "SubFrameId",
                table: "geometry_elements");
        }
    }
}
