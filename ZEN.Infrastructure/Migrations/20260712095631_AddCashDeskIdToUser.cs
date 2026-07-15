using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashDeskIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashDeskId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CashDeskId",
                table: "AspNetUsers",
                column: "CashDeskId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_CashDesks_CashDeskId",
                table: "AspNetUsers",
                column: "CashDeskId",
                principalTable: "CashDesks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_CashDesks_CashDeskId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CashDeskId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CashDeskId",
                table: "AspNetUsers");
        }
    }
}
