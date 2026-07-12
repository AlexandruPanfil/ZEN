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
            cashierUser = new AppUser
            {
                UserName = "cashier@zen.pos",
                Email = "cashier@zen.pos",
                EmailConfirmed = true,
                FirstName = "Cashier",
                LastName = "User",
                PIN = "2222",
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
