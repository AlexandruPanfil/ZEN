using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTerminals_Stores_StoreId",
                table: "PaymentTerminals");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PaymentTerminals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "CashDeskId",
                table: "PaymentTerminals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashDesks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FiscalPrinterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerDisplayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDesks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashDesks_Printers_FiscalPrinterId",
                        column: x => x.FiscalPrinterId,
                        principalTable: "Printers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CashDesks_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "CustomerDisplays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConnectionType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CashDeskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDisplays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerDisplays_CashDesks_CashDeskId",
                        column: x => x.CashDeskId,
                        principalTable: "CashDesks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerDisplays_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Scanners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScannerType = table.Column<int>(type: "int", nullable: false),
                    ConnectionType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CashDeskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scanners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scanners_CashDesks_CashDeskId",
                        column: x => x.CashDeskId,
                        principalTable: "CashDesks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Scanners_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTerminals_CashDeskId",
                table: "PaymentTerminals",
                column: "CashDeskId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDesks_CustomerDisplayId",
                table: "CashDesks",
                column: "CustomerDisplayId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDesks_FiscalPrinterId",
                table: "CashDesks",
                column: "FiscalPrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDesks_StoreId",
                table: "CashDesks",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDisplays_CashDeskId",
                table: "CustomerDisplays",
                column: "CashDeskId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDisplays_StoreId",
                table: "CustomerDisplays",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Scanners_CashDeskId",
                table: "Scanners",
                column: "CashDeskId");

            migrationBuilder.CreateIndex(
                name: "IX_Scanners_StoreId",
                table: "Scanners",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTerminals_CashDesks_CashDeskId",
                table: "PaymentTerminals",
                column: "CashDeskId",
                principalTable: "CashDesks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTerminals_Stores_StoreId",
                table: "PaymentTerminals",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_CashDesks_CustomerDisplays_CustomerDisplayId",
                table: "CashDesks",
                column: "CustomerDisplayId",
                principalTable: "CustomerDisplays",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTerminals_CashDesks_CashDeskId",
                table: "PaymentTerminals");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTerminals_Stores_StoreId",
                table: "PaymentTerminals");

            migrationBuilder.DropForeignKey(
                name: "FK_CashDesks_CustomerDisplays_CustomerDisplayId",
                table: "CashDesks");

            migrationBuilder.DropTable(
                name: "Scanners");

            migrationBuilder.DropTable(
                name: "CustomerDisplays");

            migrationBuilder.DropTable(
                name: "CashDesks");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTerminals_CashDeskId",
                table: "PaymentTerminals");

            migrationBuilder.DropColumn(
                name: "CashDeskId",
                table: "PaymentTerminals");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PaymentTerminals",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTerminals_Stores_StoreId",
                table: "PaymentTerminals",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
