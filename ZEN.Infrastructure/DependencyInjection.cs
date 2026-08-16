using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Interfaces;
using ZEN.Infrastructure.Identity;
using ZEN.Infrastructure.Persistence;
using ZEN.Infrastructure.Persistence.Repositories;
using ZEN.Infrastructure.Services;

namespace ZEN.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("zendb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'zendb' is not configured. Run the application through ZEN.AppHost or add ConnectionStrings:zendb to configuration.");
        }

        // Database with Aspire SQL Server
        services.AddDbContext<ZenDbContext>(options =>
            options.UseSqlServer(connectionString));

        // ASP.NET Core Identity
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<ZenDbContext>()
        .AddDefaultTokenProviders();

        services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

        // Repositories and Unit of Work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPrinterService, PrinterService>();
        services.AddScoped<IPaymentTerminalService, PaymentTerminalService>();
        services.AddScoped<IScannerService, ScannerService>();
        services.AddScoped<ICustomerDisplayService, CustomerDisplayService>();
        services.AddScoped<ICashDeskService, CashDeskService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IFiscalCloudService, FiscalCloudService>();
        services.AddScoped<IFiscalReceiptService, FiscalReceiptService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureWindows(this IServiceCollection services, string sqlitePath)
    {
        // SQLite for MAUI Windows client
        services.AddDbContext<ZenDbContext>(options =>
            options.UseSqlite($"Data Source={sqlitePath}"));

        // ASP.NET Core Identity
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
        })
        .AddEntityFrameworkStores<ZenDbContext>()
        .AddDefaultTokenProviders();

        services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

        // Repositories and Unit of Work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPrinterService, PrinterService>();
        services.AddScoped<IPaymentTerminalService, PaymentTerminalService>();
        services.AddScoped<IScannerService, ScannerService>();
        services.AddScoped<ICustomerDisplayService, CustomerDisplayService>();
        services.AddScoped<ICashDeskService, CashDeskService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IFiscalCloudService, FiscalCloudService>();
        services.AddScoped<IFiscalReceiptService, FiscalReceiptService>();

        return services;
    }
}