using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZEN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ServerAuthoritativeEntityVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Warehouses",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "VariantAttributes",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Taxes",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SyncStates",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SyncOutboxItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Suppliers",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SupplierPayments",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Stores",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "StockTransfers",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "StockTransferItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "StockPurchases",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "StockPurchaseItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "StockMovements",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Shifts",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SellOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SellOrderItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Scanners",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Sales",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SalePayments",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SaleItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "PurchaseOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "PurchaseOrderItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Promotions",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ProductVariants",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ProductImages",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ProcessedSyncMessages",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Printers",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Permissions",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "PaymentTerminals",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Menus",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "MenuProducts",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "MenuGroups",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "MembershipCards",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "LoyaltyTransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "InventoryItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "InventoryCounts",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "InventoryCountItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "HeldSales",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Groups",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "GroupProducts",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "FiscalReceipts",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "FiscalReceiptItems",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Discounts",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Customers",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CustomerGroups",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CustomerDisplays",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Currencies",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Coupons",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Categories",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CashMovements",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CashDesks",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CashCounts",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Brands",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "VariantAttributes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Taxes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SyncOutboxItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StockTransferItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StockPurchases");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StockPurchaseItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SellOrders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SellOrderItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Scanners");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SalePayments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ProcessedSyncMessages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PaymentTerminals");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Menus");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MenuProducts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MenuGroups");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MembershipCards");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "InventoryCounts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "InventoryCountItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "HeldSales");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GroupProducts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "FiscalReceipts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "FiscalReceiptItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomerGroups");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomerDisplays");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Currencies");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CashDesks");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CashCounts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Brands");
        }
    }
}
