using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashDeskIdToShiftAndSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_Stores_StoreId",
                table: "Shifts");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Shifts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningCash",
                table: "Shifts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Shifts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ClosingCash",
                table: "Shifts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "CashDeskId",
                table: "Shifts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashDeskId",
                table: "Sales",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_CashDeskId",
                table: "Shifts",
                column: "CashDeskId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Status",
                table: "Shifts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CashDeskId",
                table: "Sales",
                column: "CashDeskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_CashDesks_CashDeskId",
                table: "Shifts",
                column: "CashDeskId",
                principalTable: "CashDesks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_Stores_StoreId",
                table: "Shifts",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_CashDesks_CashDeskId",
                table: "Shifts");

            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_Stores_StoreId",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_CashDeskId",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_Status",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Sales_CashDeskId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CashDeskId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "CashDeskId",
                table: "Sales");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningCash",
                table: "Shifts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ClosingCash",
                table: "Shifts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_Stores_StoreId",
                table: "Shifts",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
