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
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
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
                "Error" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "IsDeleted" INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "ProcessedSyncMessages" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ProcessedSyncMessages" PRIMARY KEY,
                "IdempotencyKey" TEXT NOT NULL,
                "Source" TEXT NOT NULL,
                "ProcessedAt" TEXT NOT NULL,
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
        logger.LogInformation("Verified local synchronization tables.");
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
        logger.LogInformation("Verified local Menus table.");
    }
}