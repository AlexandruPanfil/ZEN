using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndependentMenuGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashDesks_Menus_MenuId",
                table: "CashDesks");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuGroups_Groups_GroupId",
                table: "MenuGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Menus_Stores_StoreId",
                table: "Menus");

            migrationBuilder.DropIndex(
                name: "IX_MenuProducts_MenuId",
                table: "MenuProducts");

            migrationBuilder.DropIndex(
                name: "IX_MenuGroups_GroupId",
                table: "MenuGroups");

            migrationBuilder.DropIndex(
                name: "IX_MenuGroups_MenuId",
                table: "MenuGroups");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "MenuGroups");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Menus",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "MenuGroups",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MenuGroupProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuGroupProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuGroupProducts_MenuGroups_MenuGroupId",
                        column: x => x.MenuGroupId,
                        principalTable: "MenuGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuGroupProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuGroupReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuGroupReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuGroupReferences_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuGroupReferences_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuProducts_MenuId_ProductId",
                table: "MenuProducts",
                columns: new[] { "MenuId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroups_MenuId_Position",
                table: "MenuGroups",
                columns: new[] { "MenuId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroupProducts_MenuGroupId_ProductId",
                table: "MenuGroupProducts",
                columns: new[] { "MenuGroupId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroupProducts_ProductId",
                table: "MenuGroupProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroupReferences_GroupId",
                table: "MenuGroupReferences",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroupReferences_MenuId_GroupId",
                table: "MenuGroupReferences",
                columns: new[] { "MenuId", "GroupId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CashDesks_Menus_MenuId",
                table: "CashDesks",
                column: "MenuId",
                principalTable: "Menus",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Menus_Stores_StoreId",
                table: "Menus",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashDesks_Menus_MenuId",
                table: "CashDesks");

            migrationBuilder.DropForeignKey(
                name: "FK_Menus_Stores_StoreId",
                table: "Menus");

            migrationBuilder.DropTable(
                name: "MenuGroupProducts");

            migrationBuilder.DropTable(
                name: "MenuGroupReferences");

            migrationBuilder.DropIndex(
                name: "IX_MenuProducts_MenuId_ProductId",
                table: "MenuProducts");

            migrationBuilder.DropIndex(
                name: "IX_MenuGroups_MenuId_Position",
                table: "MenuGroups");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "MenuGroups");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Menus",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "MenuGroups",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MenuProducts_MenuId",
                table: "MenuProducts",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroups_GroupId",
                table: "MenuGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuGroups_MenuId",
                table: "MenuGroups",
                column: "MenuId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashDesks_Menus_MenuId",
                table: "CashDesks",
                column: "MenuId",
                principalTable: "Menus",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuGroups_Groups_GroupId",
                table: "MenuGroups",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Menus_Stores_StoreId",
                table: "Menus",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");
        }
    }
}
