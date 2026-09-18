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

        // Seed ProductGroups Hierarchy
        // Reorganize existing ProductGroup data to match desired hierarchy
        var existingGroups = await context.ProductGroups.Where(g => g.IsActive).ToListAsync();
        
        if (existingGroups.Any())
        {
            // We'll update existing groups to match our desired structure
            // First, let's identify or create the root groups we need
            
            // Get or create Main Group
            var mainGroup = existingGroups.FirstOrDefault(g => g.Name == "Main Group" && !g.ParentGroupId.HasValue);
            if (mainGroup == null)
            {
                mainGroup = existingGroups.FirstOrDefault(g => !g.ParentGroupId.HasValue); // Take first root as Main Group
                if (mainGroup != null)
                {
                    mainGroup.Name = "Main Group";
                    mainGroup.SortOrder = 1;
                }
            }
            
            // Get or create Second Group  
            var secondGroup = existingGroups.FirstOrDefault(g => g.Name == "Second Group" && !g.ParentGroupId.HasValue);
            if (secondGroup == null && mainGroup != null)
            {
                secondGroup = existingGroups.FirstOrDefault(g => g.Id != mainGroup.Id && !g.ParentGroupId.HasValue);
                if (secondGroup != null)
                {
                    secondGroup.Name = "Second Group";
                    secondGroup.SortOrder = 2;
                }
            }
            
            // Get or create ThirdGroup
            var thirdGroup = existingGroups.FirstOrDefault(g => g.Name == "ThirdGroup" && !g.ParentGroupId.HasValue);
            if (thirdGroup == null && mainGroup != null && secondGroup != null)
            {
                thirdGroup = existingGroups.FirstOrDefault(g => 
                    g.Id != mainGroup.Id && 
                    g.Id != secondGroup.Id && 
                    !g.ParentGroupId.HasValue);
                if (thirdGroup != null)
                {
                    thirdGroup.Name = "ThirdGroup";
                    thirdGroup.SortOrder = 3;
                }
            }
            
            // Get or create FourGroup
            var fourGroup = existingGroups.FirstOrDefault(g => g.Name == "FourGroup" && !g.ParentGroupId.HasValue);
            if (fourGroup == null && mainGroup != null && secondGroup != null && thirdGroup != null)
            {
                fourGroup = existingGroups.FirstOrDefault(g => 
                    g.Id != mainGroup.Id && 
                    g.Id != secondGroup.Id && 
                    g.Id != thirdGroup.Id && 
                    !g.ParentGroupId.HasValue);
                if (fourGroup != null)
                {
                    fourGroup.Name = "FourGroup";
                    fourGroup.SortOrder = 4;
                }
            }
            
            // Now handle child groups
            // MainGroup children
            if (mainGroup != null)
            {
                var mainChild1 = existingGroups.FirstOrDefault(g => g.Name == "MainChildGroup1" && g.ParentGroupId == mainGroup.Id);
                if (mainChild1 == null)
                {
                    mainChild1 = existingGroups.FirstOrDefault(g => 
                        g.ParentGroupId == mainGroup.Id && 
                        g.Name != "MainChildGroup2");
                }
                if (mainChild1 != null)
                {
                    mainChild1.Name = "MainChildGroup1";
                    mainChild1.SortOrder = 1;
                }
                
                var mainChild2 = existingGroups.FirstOrDefault(g => g.Name == "MainChildGroup2" && g.ParentGroupId == mainGroup.Id);
                if (mainChild2 == null)
                {
                    mainChild2 = existingGroups.FirstOrDefault(g => 
                        g.ParentGroupId == mainGroup.Id && 
                        g.Id != (mainChild1?.Id ?? Guid.Empty) &&
                        g.Name != "MainChildGroup1");
                }
                if (mainChild2 != null)
                {
                    mainChild2.Name = "MainChildGroup2";
                    mainChild2.SortOrder = 2;
                }
            }
            
            // SecondGroup children
            if (secondGroup != null)
            {
                var secondChild1 = existingGroups.FirstOrDefault(g => g.Name == "SecondChildGroup1" && g.ParentGroupId == secondGroup.Id);
                if (secondChild1 == null)
                {
                    secondChild1 = existingGroups.FirstOrDefault(g => 
                        g.ParentGroupId == secondGroup.Id && 
                        g.Name != "SecondChildGroup2" && 
                        g.Name != "SecondChildGroup3");
                }
                if (secondChild1 != null)
                {
                    secondChild1.Name = "SecondChildGroup1";
                    secondChild1.SortOrder = 1;
                }
                
                var secondChild2 = existingGroups.FirstOrDefault(g => g.Name == "SecondChildGroup2" && g.ParentGroupId == secondGroup.Id);
                if (secondChild2 == null)
                {
                    secondChild2 = existingGroups.FirstOrDefault(g => 
                        g.ParentGroupId == secondGroup.Id && 
                        g.Id != (secondChild1?.Id ?? Guid.Empty) &&
                        g.Name != "SecondChildGroup1" &&
                        g.Name != "SecondChildGroup3");
                }
                if (secondChild2 != null)
                {
                    secondChild2.Name = "SecondChildGroup2";
                    secondChild2.SortOrder = 2;
                }
                
                var secondChild3 = existingGroups.FirstOrDefault(g => g.Name == "SecondChildGroup3" && g.ParentGroupId == secondGroup.Id);
                if (secondChild3 == null)
                {
                    secondChild3 = existingGroups.FirstOrDefault(g => 
                        g.ParentGroupId == secondGroup.Id && 
                        g.Id != (secondChild1?.Id ?? Guid.Empty) &&
                        g.Id != (secondChild2?.Id ?? Guid.Empty) &&
                        g.Name != "SecondChildGroup1" &&
                        g.Name != "SecondChildGroup2");
                }
                if (secondChild3 != null)
                {
                    secondChild3.Name = "SecondChildGroup3";
                    secondChild3.SortOrder = 3;
                }
            }
            
            // FourGroup children
            if (fourGroup != null)
            {
                var fourChild1 = existingGroups.FirstOrDefault(g => g.Name == "FourChildGroup1" && g.ParentGroupId == fourGroup.Id);
                if (fourChild1 == null)
                {
                    fourChild1 = existingGroups.FirstOrDefault(g => 
                        g.ParentGroupId == fourGroup.Id);
                }
                if (fourChild1 != null)
                {
                    fourChild1.Name = "FourChildGroup1";
                    fourChild1.SortOrder = 1;
                }
            }
            
            // Deactivate any extra groups that don't fit our structure
            var usedGroupIds = new HashSet<Guid?>();
            if (mainGroup != null) usedGroupIds.Add(mainGroup.Id);
            if (secondGroup != null) usedGroupIds.Add(secondGroup.Id);
            if (thirdGroup != null) usedGroupIds.Add(thirdGroup.Id);
            if (fourGroup != null) usedGroupIds.Add(fourGroup.Id);
            
            // Add children to used IDs
            var allGroups = new List<ProductGroup> { mainGroup, secondGroup, thirdGroup, fourGroup };
            foreach (var group in allGroups.Where(g => g != null))
            {
                var children = existingGroups.Where(g => g.ParentGroupId == group.Id).ToList();
                foreach (var child in children)
                {
                    usedGroupIds.Add(child.Id);
                }
            }
            
            // Deactivate unused active groups
            var groupsToDeactivate = existingGroups.Where(g => g.IsActive && !usedGroupIds.Contains(g.Id));
            foreach (var group in groupsToDeactivate)
            {
                group.IsActive = false;
            }
            
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
                Type = PrinterType.Fiscal,
                FiscalDeviceType = FiscalDeviceType.FiscalCloud,
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

        // Recover existing "Completed" sales that were persisted before the fiscal
        // fix shipped. Those sales had Status = Completed set optimistically and may
        // never have received a fiscal receipt. Mark them Failed so they are not
        // reported as successful taxable transactions.
        var salesWithoutReceipts = await context.Sales
            .Where(s => s.Status == SaleStatus.Completed && !s.FiscalReceipts.Any())
            .ToListAsync();

        foreach (var sale in salesWithoutReceipts)
        {
            sale.Status = SaleStatus.Failed;
            sale.Notes = "Fiscal receipt not found. This sale predates the fiscal fix. Verify in FiscalCloud agent.";
        }

        if (salesWithoutReceipts.Any())
        {
            await context.SaveChangesAsync();
        }
    }
}
