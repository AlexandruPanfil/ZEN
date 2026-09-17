using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ZEN.Infrastructure.Persistence;
using System.IO;

namespace ZEN.Windows.Services;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZenDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        
        var dbPath = dbContext.Database.GetDbConnection().DataSource;
        logger.LogInformation("Database path: {DbPath}", dbPath);
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            logger.LogInformation("Created database directory: {Directory}", directory);
        }
        
        logger.LogInformation("Ensuring database is created...");
        await dbContext.Database.EnsureCreatedAsync();
        await EnsurePrinterTableAsync(dbContext, logger);
        await EnsureScannerTableAsync(dbContext, logger);
        await EnsureCustomerDisplayTableAsync(dbContext, logger);
        await EnsurePaymentTerminalTableAsync(dbContext, logger);
        await EnsureStoreTableAsync(dbContext, logger);
        await EnsureMenuTableAsync(dbContext, logger);
        await EnsureProductsTableAsync(dbContext, logger);
        await EnsureMenuProductsTableAsync(dbContext, logger);
        await EnsureCashDeskTableAsync(dbContext, logger);
        await EnsureSyncTablesAsync(dbContext, logger);
        logger.LogInformation("Database created successfully");
    }

    private static async Task EnsurePrinterTableAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "Printers" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Printers" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Type" INTEGER NOT NULL,
                "FiscalDeviceType" INTEGER NULL,
                "IsDefault" INTEGER NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "ApiKey" TEXT NULL,
                "ApiSecret" TEXT NULL,
                "DeviceId" TEXT NULL,
                "PointOfSaleId" TEXT NULL,
                "FiscalCloudBaseUrl" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "IsDeleted" INTEGER NOT NULL
            );
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "Printers", logger);
        logger.LogInformation("Verified local Printers table.");
    }

    private static async Task EnsureSyncTablesAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "SyncStates" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_SyncStates" PRIMARY KEY,
                "LastProductsSyncUtc" TEXT NULL,
                "LastSuccessfulSyncUtc" TEXT NULL,
                "LastError" TEXT NULL,
                "LastSalesSyncUtc" TEXT NULL,
                "LastShiftsSyncUtc" TEXT NULL,
                "LastInventorySyncUtc" TEXT NULL,
                "LastEquipmentSyncUtc" TEXT NULL,
                "PullCursor" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "Version" INTEGER NOT NULL DEFAULT 1,
                "IsDeleted" INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "SyncOutboxItems" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_SyncOutboxItems" PRIMARY KEY,
                "EntityType" TEXT NOT NULL,
                "EntityId" TEXT NOT NULL,
                "PayloadJson" TEXT NOT NULL,
                "IdempotencyKey" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "RetryCount" INTEGER NOT NULL,
                "LastAttemptUtc" TEXT NULL,
                "SyncedAtUtc" TEXT NULL,
                "BaseVersion" INTEGER NULL,
                "ConflictRecordJson" TEXT NULL,
                "ConflictVersion" INTEGER NULL,
                "Error" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "Version" INTEGER NOT NULL DEFAULT 1,
                "IsDeleted" INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "ProcessedSyncMessages" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ProcessedSyncMessages" PRIMARY KEY,
                "IdempotencyKey" TEXT NOT NULL,
                "Source" TEXT NOT NULL,
                "ProcessedAt" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "Version" INTEGER NOT NULL DEFAULT 1,
                "IsDeleted" INTEGER NOT NULL
            );
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "SyncStates", logger);
        await EnsureVersionColumnAsync(dbContext, "SyncOutboxItems", logger);
        await EnsureVersionColumnAsync(dbContext, "ProcessedSyncMessages", logger);
        await EnsureColumnAsync(dbContext, "SyncOutboxItems", "SyncedAtUtc", "TEXT", logger);
        await EnsureColumnAsync(dbContext, "SyncOutboxItems", "BaseVersion", "INTEGER", logger);
        await EnsureColumnAsync(dbContext, "SyncOutboxItems", "ConflictRecordJson", "TEXT", logger);
        await EnsureColumnAsync(dbContext, "SyncOutboxItems", "ConflictVersion", "INTEGER", logger);
        await EnsureColumnAsync(dbContext, "SyncStates", "LastSalesSyncUtc", "TEXT", logger);
        await EnsureColumnAsync(dbContext, "SyncStates", "LastShiftsSyncUtc", "TEXT", logger);
        await EnsureColumnAsync(dbContext, "SyncStates", "LastInventorySyncUtc", "TEXT", logger);
        await EnsureColumnAsync(dbContext, "SyncStates", "LastEquipmentSyncUtc", "TEXT", logger);
        await EnsureColumnAsync(dbContext, "SyncStates", "PullCursor", "TEXT", logger);
        logger.LogInformation("Verified local synchronization tables.");
    }

    private static async Task EnsureColumnAsync(
        ZenDbContext dbContext,
        string tableName,
        string columnName,
        string sqliteType,
        ILogger logger)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        var exists = false;
        await using (var reader = await checkCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (exists)
        {
            return;
        }

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {sqliteType} NULL;";
        await alterCommand.ExecuteNonQueryAsync();
        logger.LogInformation("Added missing local database column {Table}.{Column}.", tableName, columnName);
    }

    private static async Task EnsureVersionColumnAsync(
        ZenDbContext dbContext,
        string tableName,
        ILogger logger)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"PRAGMA table_info(\"{tableName}\");";
        var exists = false;
        await using (var reader = await checkCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), "Version", StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }
        if (exists)
        {
            return;
        }
        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"Version\" INTEGER NOT NULL DEFAULT 1;";
        await alterCommand.ExecuteNonQueryAsync();
        logger.LogInformation("Added missing local database column {Table}.{Column}.", tableName, "Version");
    }

    private static async Task EnsureScannerTableAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "Scanners" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Scanners" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "ScannerType" INTEGER NOT NULL,
                "ConnectionType" TEXT NULL,
                "ConnectionString" TEXT NULL,
                "IsActive" INTEGER NOT NULL,
                "CashDeskId" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "IsDeleted" INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_Scanners_CashDeskId"
                ON "Scanners" ("CashDeskId");
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "Scanners", logger);
        logger.LogInformation("Verified local Scanners table.");
    }

    private static async Task EnsureCustomerDisplayTableAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "CustomerDisplays" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_CustomerDisplays" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "ConnectionType" TEXT NULL,
                "ConnectionString" TEXT NULL,
                "Size" TEXT NULL,
                "IsActive" INTEGER NOT NULL,
                "CashDeskId" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "IsDeleted" INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_CustomerDisplays_CashDeskId"
                ON "CustomerDisplays" ("CashDeskId");
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "CustomerDisplays", logger);
        logger.LogInformation("Verified local CustomerDisplays table.");
    }

    private static async Task EnsureCashDeskTableAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "CashDesks" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_CashDesks" PRIMARY KEY,
                "StoreId" TEXT NULL,
                "Name" TEXT NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "FiscalPrinterId" TEXT NULL,
                "CustomerDisplayId" TEXT NULL,
                "LastActivityUtc" TEXT NULL,
                "IsOnline" INTEGER NOT NULL,
                "MenuId" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "IsDeleted" INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_CashDesks_StoreId"
                ON "CashDesks" ("StoreId");
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "CashDesks", logger);
        logger.LogInformation("Verified local CashDesks table.");
    }

    private static async Task EnsurePaymentTerminalTableAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "PaymentTerminals" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_PaymentTerminals" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "TerminalType" TEXT NOT NULL,
                "SerialNumber" TEXT NULL,
                "IpAddress" TEXT NULL,
                "Port" INTEGER NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "CashDeskId" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "IsDeleted" INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_PaymentTerminals_CashDeskId"
                ON "PaymentTerminals" ("CashDeskId");
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "PaymentTerminals", logger);
        logger.LogInformation("Verified local PaymentTerminals table.");
    }

    private static async Task EnsureStoreTableAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "Stores" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Stores" PRIMARY KEY,
                "Code" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "Address" TEXT NULL,
                "City" TEXT NULL,
                "Phone" TEXT NULL,
                "Email" TEXT NULL,
                "TaxNumber" TEXT NULL,
                "LogoPath" TEXT NULL,
                "ReceiptHeader" TEXT NULL,
                "ReceiptFooter" TEXT NULL,
                "CurrencyId" TEXT NULL,
                "IsActive" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "IsDeleted" INTEGER NOT NULL
            );
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "Stores", logger);
        logger.LogInformation("Verified local Stores table.");
    }

    private static async Task EnsureMenuTableAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "Menus" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Menus" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "StoreId" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "IsDeleted" INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_Menus_StoreId"
                ON "Menus" ("StoreId");
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "Menus", logger);
        logger.LogInformation("Verified local Menus table.");
    }

    private static async Task EnsureProductsTableAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "Products" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Products" PRIMARY KEY,
                "ProductGroupId" TEXT NULL,
                "CategoryId" TEXT NULL,
                "Barcode" TEXT NULL,
                "CostPrice" NUMERIC(18,4) NULL,
                "SalePrice" NUMERIC(18,4) NOT NULL,
                "Description" TEXT NULL,
                "HasVariants" INTEGER NOT NULL,
                "IsActive" INTEGER NOT NULL,
                "IsDeleted" INTEGER NOT NULL,
                "SKU" TEXT NULL,
                "Name" TEXT NOT NULL,
                "SupplierId" TEXT NULL,
                "TaxId" TEXT NULL,
                "TrackExpiry" INTEGER NOT NULL,
                "TrackSerial" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "Version" INTEGER NOT NULL DEFAULT 1
            );
            CREATE INDEX IF NOT EXISTS "IX_Products_ProductGroupId"
                ON "Products" ("ProductGroupId");
            CREATE INDEX IF NOT EXISTS "IX_Products_CategoryId"
                ON "Products" ("CategoryId");
            CREATE INDEX IF NOT EXISTS "IX_Products_SKU"
                ON "Products" ("SKU");
            CREATE INDEX IF NOT EXISTS "IX_Products_Name"
                ON "Products" ("Name");
            CREATE INDEX IF NOT EXISTS "IX_Products_SupplierId"
                ON "Products" ("SupplierId");
            CREATE INDEX IF NOT EXISTS "IX_Products_TaxId"
                ON "Products" ("TaxId");
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "Products", logger);
        logger.LogInformation("Verified local Products table.");
    }

    private static async Task EnsureMenuProductsTableAsync(ZenDbContext dbContext, ILogger logger)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "MenuProducts" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_MenuProducts" PRIMARY KEY,
                "MenuId" TEXT NOT NULL,
                "ProductId" TEXT NOT NULL,
                "Position" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "IsDeleted" INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_MenuProducts_MenuId"
                ON "MenuProducts" ("MenuId");
            CREATE INDEX IF NOT EXISTS "IX_MenuProducts_ProductId"
                ON "MenuProducts" ("ProductId");
            """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
        await EnsureVersionColumnAsync(dbContext, "MenuProducts", logger);
        logger.LogInformation("Verified local MenuProducts table.");
    }
}
