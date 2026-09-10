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
}