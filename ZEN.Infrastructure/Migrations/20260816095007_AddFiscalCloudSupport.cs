using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalCloudSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "Printers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiSecret",
                table: "Printers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "Printers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalCloudBaseUrl",
                table: "Printers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PointOfSaleId",
                table: "Printers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FiscalReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FiscalReceiptDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalReceipts_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalReceiptItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxGroupCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalReceiptItems_FiscalReceipts_FiscalReceiptId",
                        column: x => x.FiscalReceiptId,
                        principalTable: "FiscalReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReceiptItems_FiscalReceiptId",
                table: "FiscalReceiptItems",
                column: "FiscalReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReceipts_CreatedAt",
                table: "FiscalReceipts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReceipts_FiscalReceiptNumber",
                table: "FiscalReceipts",
                column: "FiscalReceiptNumber");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReceipts_SaleId",
                table: "FiscalReceipts",
                column: "SaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiscalReceiptItems");

            migrationBuilder.DropTable(
                name: "FiscalReceipts");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "ApiSecret",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "FiscalCloudBaseUrl",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "PointOfSaleId",
                table: "Printers");
        }
    }
}
