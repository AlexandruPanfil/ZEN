using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;
using ZEN.Infrastructure.Identity;

namespace ZEN.Infrastructure.Persistence;

public class ZenDbContext : IdentityDbContext<AppUser, AppRole, string>
{
    private readonly ICurrentUserService? _currentUserService;

    public ZenDbContext(
        DbContextOptions<ZenDbContext> options,
        ICurrentUserService currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

// Configuration
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Tax> Taxes => Set<Tax>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<PaymentTerminal> PaymentTerminals => Set<PaymentTerminal>();
    public DbSet<Scanner> Scanners => Set<Scanner>();
    public DbSet<CustomerDisplay> CustomerDisplays => Set<CustomerDisplay>();
    public DbSet<CashDesk> CashDesks => Set<CashDesk>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupProduct> GroupProducts => Set<GroupProduct>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuProduct> MenuProducts => Set<MenuProduct>();
    public DbSet<MenuGroup> MenuGroups => Set<MenuGroup>();

    // Auth & Security
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();

    // Products
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<VariantAttribute> VariantAttributes => Set<VariantAttribute>();

    // Inventory
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<StockPurchase> StockPurchases => Set<StockPurchase>();
    public DbSet<StockPurchaseItem> StockPurchaseItems => Set<StockPurchaseItem>();
    public DbSet<InventoryCount> InventoryCounts => Set<InventoryCount>();
    public DbSet<InventoryCountItem> InventoryCountItems => Set<InventoryCountItem>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();

    // Customers
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<MembershipCard> MembershipCards => Set<MembershipCard>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();

    // Suppliers
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

    // Sales
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<CashCount> CashCounts => Set<CashCount>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<HeldSale> HeldSales => Set<HeldSale>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    // Sync
    public DbSet<SyncState> SyncStates => Set<SyncState>();
    public DbSet<SyncOutboxItem> SyncOutboxItems => Set<SyncOutboxItem>();
    public DbSet<ProcessedSyncMessage> ProcessedSyncMessages => Set<ProcessedSyncMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZenDbContext).Assembly);

        ConfigureDefaultDecimalPrecision(modelBuilder);

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var comparison = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(comparison, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigureDefaultDecimalPrecision(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()))
        {
            var propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

            if (propertyType == typeof(decimal)
                && property.GetPrecision() is null
                && property.GetColumnType() is null)
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }
}

