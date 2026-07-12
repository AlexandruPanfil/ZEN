using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentRelationshipRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDisplays_Stores_StoreId",
                table: "CustomerDisplays");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTerminals_Stores_StoreId",
                table: "PaymentTerminals");

            migrationBuilder.DropForeignKey(
                name: "FK_Printers_Stores_StoreId",
                table: "Printers");

            migrationBuilder.DropForeignKey(
                name: "FK_Scanners_Stores_StoreId",
                table: "Scanners");

            migrationBuilder.DropIndex(
                name: "IX_Scanners_StoreId",
                table: "Scanners");

            migrationBuilder.DropIndex(
                name: "IX_Printers_StoreId",
                table: "Printers");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTerminals_StoreId",
                table: "PaymentTerminals");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDisplays_StoreId",
                table: "CustomerDisplays");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Scanners");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "PaymentTerminals");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "CustomerDisplays");

            migrationBuilder.AlterColumn<Guid>(
                name: "FiscalPrinterId",
                table: "CashDesks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerDisplayId",
                table: "CashDesks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "StoreId",
                table: "CashDesks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Scanners",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Printers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "PaymentTerminals",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "CustomerDisplays",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "FiscalPrinterId",
                table: "CashDesks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerDisplayId",
                table: "CashDesks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StoreId",
                table: "CashDesks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scanners_StoreId",
                table: "Scanners",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Printers_StoreId",
                table: "Printers",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTerminals_StoreId",
                table: "PaymentTerminals",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDisplays_StoreId",
                table: "CustomerDisplays",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDisplays_Stores_StoreId",
                table: "CustomerDisplays",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTerminals_Stores_StoreId",
                table: "PaymentTerminals",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Printers_Stores_StoreId",
                table: "Printers",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Scanners_Stores_StoreId",
                table: "Scanners",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");
        }
    }
}
