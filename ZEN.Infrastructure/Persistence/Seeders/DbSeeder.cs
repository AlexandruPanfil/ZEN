using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Infrastructure.Identity;

namespace ZEN.Infrastructure.Persistence.Seeders;

public static class DbSeeder
{
    public static async Task SeedAsync(ZenDbContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        // Seed Store
        if (!await context.Stores.AnyAsync())
        {
            var defaultStore = new Store
            {
                Id = Guid.NewGuid(),
                Name = "Main Store",
                Code = "MAIN",
                Address = "123 Main Street",
                City = "City",
                Phone = "+1234567890",
                Email = "store@zen.pos",
                IsActive = true
            };
            context.Stores.Add(defaultStore);
            await context.SaveChangesAsync();
        }

        // Seed Currencies
        if (!await context.Currencies.AnyAsync())
        {
            context.Currencies.AddRange(
                new Currency { Id = Guid.NewGuid(), Code = "USD", Name = "US Dollar", Symbol = "$", IsDefault = true },
                new Currency { Id = Guid.NewGuid(), Code = "EUR", Name = "Euro", Symbol = "€", IsDefault = false }
            );
            await context.SaveChangesAsync();
        }

        // Seed Taxes
        if (!await context.Taxes.AnyAsync())
        {
            context.Taxes.AddRange(
                new Tax { Id = Guid.NewGuid(), Name = "Standard", Rate = 20.0m, IsDefault = true },
                new Tax { Id = Guid.NewGuid(), Name = "Zero", Rate = 0.0m, IsDefault = false }
            );
            await context.SaveChangesAsync();
        }

        // Seed Warehouse
        if (!await context.Warehouses.AnyAsync())
        {
            var defaultStore = await context.Stores.FirstAsync();
            context.Warehouses.Add(new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = "Main Warehouse",
                Code = "WH-MAIN",
                StoreId = defaultStore.Id,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        // Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = "General",
                SortOrder = 0,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        // Seed Products for Menu demo
        if (!await context.Products.AnyAsync())
        {
            var defaultTax = await context.Taxes.FirstAsync(t => t.IsDefault);
            var defaultCategory = await context.Categories.FirstAsync();

            context.Products.AddRange(
                new Product { Id = Guid.NewGuid(), Name = "Coffee", Barcode = "1001", CostPrice = 2.00m, SalePrice = 3.50m, TaxId = defaultTax.Id, CategoryId = defaultCategory.Id, IsActive = true },
                new Product { Id = Guid.NewGuid(), Name = "Tea", Barcode = "1002", CostPrice = 1.50m, SalePrice = 3.00m, TaxId = defaultTax.Id, CategoryId = defaultCategory.Id, IsActive = true },
                new Product { Id = Guid.NewGuid(), Name = "Sandwich", Barcode = "1003", CostPrice = 4.00m, SalePrice = 8.00m, TaxId = defaultTax.Id, CategoryId = defaultCategory.Id, IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        // Seed CashDesk
        if (!await context.CashDesks.AnyAsync())
        {
            var defaultStore = await context.Stores.FirstAsync();
            var cashDesk = new CashDesk
            {
                Id = Guid.NewGuid(),
                StoreId = defaultStore.Id,
                Name = "Main Cash Desk",
                IsActive = true
            };
            context.CashDesks.Add(cashDesk);
            await context.SaveChangesAsync();
        }

        // Seed default FiscalCloud printer and link it to the cash desk so sales
        // are forwarded to the local FiscalCloud agent (http://localhost:50700).
        // IMPORTANT: Replace placeholder credentials with actual FiscalCloud credentials!
        if (!await context.Printers.AnyAsync(p => p.Type == PrinterType.Fiscal))
        {
            var fiscalPrinter = new Printer
            {
                Id = Guid.NewGuid(),
                Name = "FiscalCloud (Local)",
                PrinterName = "FiscalCloud",
                Type = PrinterType.Fiscal,
                ConnectionType = "FiscalCloud",
                IsDefault = true,
                IsActive = true,
                ApiKey = "REPLACE_WITH_ACTUAL_API_KEY",
                ApiSecret = "REPLACE_WITH_ACTUAL_API_SECRET",
                DeviceId = "REPLACE_WITH_ACTUAL_DEVICE_ID_GUID",
                PointOfSaleId = "REPLACE_WITH_ACTUAL_POS_ID_GUID",
                FiscalCloudBaseUrl = "http://localhost:50700"
            };
            context.Printers.Add(fiscalPrinter);
            await context.SaveChangesAsync();

            var cashDesk = await context.CashDesks.FirstAsync();
            cashDesk.FiscalPrinterId = fiscalPrinter.Id;
            await context.SaveChangesAsync();
        }

        // Seed Menu with products for CashDesk
        if (!await context.Menus.AnyAsync())
        {
            var products = await context.Products.ToListAsync();
            var menuId = Guid.NewGuid();
            var menu = new Menu
            {
                Id = menuId,
                Name = "Default POS Menu",
                StoreId = (await context.Stores.FirstAsync()).Id
            };

            // Add first 3 products to menu (or create demo products if none exist)
            for (int i = 0; i < Math.Min(3, products.Count); i++)
            {
                menu.Products.Add(new MenuProduct
                {
                    Id = Guid.NewGuid(),
                    ProductId = products[i].Id,
                    Position = i
                });
            }

            context.Menus.Add(menu);
            await context.SaveChangesAsync();

            // Assign menu to cash desk
            var cashDesk = await context.CashDesks.FirstAsync();
            cashDesk.MenuId = menuId;
            await context.SaveChangesAsync();
        }

        // Seed Roles
        var roleNames = new[] { "Administrator", "Cashier", "Manager", "Accountant", "InventoryManager", "Technician" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new AppRole { Name = roleName });
            }
        }

        // Seed Permissions and RolePermissions
        if (!await context.Permissions.AnyAsync())
        {
            var permissions = new List<Permission>();
            var permissionNames = new[]
            {
                "ViewDashboard", "ManageUsers", "ManageRoles", "ViewReports", "ManageProducts",
                "ManageInventory", "ProcessSales", "ManageCustomers", "ManageSuppliers",
                "ViewAuditLogs", "ManagePrinters", "ManagePaymentTerminals", "ManageStores",
                "ManageShifts", "ManageCashDrawer", "ApplyDiscounts", "RefundSales", "HoldSales"
            };

            foreach (var name in permissionNames)
            {
                permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"Permission to {name}"
                });
            }
            context.Permissions.AddRange(permissions);
            await context.SaveChangesAsync();

            // Admin gets all permissions
            var adminRole = await roleManager.FindByNameAsync("Administrator");
            if (adminRole != null)
            {
                foreach (var permission in permissions)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        // Seed Admin User
        var adminUser = await userManager.FindByEmailAsync("admin@zen.pos");
        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = "admin@zen.pos",
                Email = "admin@zen.pos",
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                PIN = "1234",
                IsActive = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
        }

        // Seed Manager User
        var managerUser = await userManager.FindByEmailAsync("manager@zen.pos");
        if (managerUser == null)
        {
            managerUser = new AppUser
            {
                UserName = "manager@zen.pos",
                Email = "manager@zen.pos",
                EmailConfirmed = true,
                FirstName = "Store",
                LastName = "Manager",
                PIN = "3333",
                IsActive = true
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }
        }

        // Seed Cashier User
        var cashierUser = await userManager.FindByEmailAsync("cashier@zen.pos");
        if (cashierUser == null)
        {
            var mainCashDesk = await context.CashDesks.FirstOrDefaultAsync();
            cashierUser = new AppUser
            {
                UserName = "cashier@zen.pos",
                Email = "cashier@zen.pos",
                EmailConfirmed = true,
                FirstName = "Cashier",
                LastName = "User",
                PIN = "2222",
                CashDeskId = mainCashDesk?.Id,
                IsActive = true
            };
            var result = await userManager.CreateAsync(cashierUser, "Cashier@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(cashierUser, "Cashier");
            }
        }
    }
}
