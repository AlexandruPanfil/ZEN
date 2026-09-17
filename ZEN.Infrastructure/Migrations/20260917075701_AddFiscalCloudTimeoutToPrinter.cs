using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalCloudTimeoutToPrinter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FiscalCloudTimeoutSeconds",
                table: "Printers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiscalCloudTimeoutSeconds",
                table: "Printers");
        }
    }
}
