using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncUpdateForZENWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastEquipmentSyncUtc",
                table: "SyncStates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInventorySyncUtc",
                table: "SyncStates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSalesSyncUtc",
                table: "SyncStates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastShiftsSyncUtc",
                table: "SyncStates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PullCursor",
                table: "SyncStates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "SyncOutboxItems",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<long>(
                name: "BaseVersion",
                table: "SyncOutboxItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAtUtc",
                table: "SyncOutboxItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncStates_Id",
                table: "SyncStates",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncOutboxItems_IdempotencyKey",
                table: "SyncOutboxItems",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncStates_Id",
                table: "SyncStates");

            migrationBuilder.DropIndex(
                name: "IX_SyncOutboxItems_IdempotencyKey",
                table: "SyncOutboxItems");

            migrationBuilder.DropColumn(
                name: "LastEquipmentSyncUtc",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "LastInventorySyncUtc",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "LastSalesSyncUtc",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "LastShiftsSyncUtc",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "PullCursor",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "BaseVersion",
                table: "SyncOutboxItems");

            migrationBuilder.DropColumn(
                name: "SyncedAtUtc",
                table: "SyncOutboxItems");

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "SyncOutboxItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
