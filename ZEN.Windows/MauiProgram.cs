using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using MudBlazor.Services;
using MudBlazor;
using ZEN.Windows.Services;
using ZEN.Windows.Services.Sync;
using ZEN.Infrastructure;
using ZEN.Application;
using ZEN.Domain.Interfaces;
using System.IO;

namespace ZEN.Windows;

public static class MauiProgram
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddHttpClient(); // Register IHttpClientFactory for FiscalCloudService
        builder.Services.AddSingleton<PosAuthState>();
        builder.Services.AddSingleton<TokenSyncService>();
        builder.Services.AddSingleton<AuthenticationStateProvider, PosAuthenticationStateProvider>();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddHttpContextAccessor();

        // SQLite path for future use (database init disabled due to MAUI Windows ContentRootPath bug)
        var sqlitePath = Path.Combine(FileSystem.AppDataDirectory, "zen.db");
        builder.Services.AddInfrastructureWindows(sqlitePath);

        builder.Services.AddSingleton<ICurrentUserService, PosCurrentUserService>();
        builder.Services.AddSingleton<ServerSettingsService>();
        builder.Services.AddApplication();
        builder.Services.AddScoped<PosPrinterService>();
        builder.Services.AddScoped<PosScannerService>();
        builder.Services.AddScoped<PosCustomerDisplayService>();
        builder.Services.AddScoped<PosCashDeskService>();
        builder.Services.AddScoped<PosPaymentTerminalService>();
        builder.Services.AddScoped<PosStoreService>();
        builder.Services.AddScoped<PosMenuService>();
        builder.Services.AddScoped<MenuNavigationState>();

        // Add dummy JWT config for local auth
        builder.Configuration["Jwt:Key"] = "SuperSecretKeyForLocalMauiApp12345!";
        builder.Configuration["Jwt:Issuer"] = "ZEN.POS";
        builder.Configuration["Jwt:Audience"] = "ZEN.POS";

        var serverSettings = new ServerSettingsService();
        builder.Services.AddSingleton(serverSettings);
        
        // Create HttpClient as singleton for SyncApiClient
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        var httpClient = new HttpClient(httpClientHandler)
        {
            BaseAddress = new Uri(serverSettings.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
        builder.Services.AddSingleton(httpClient);
        builder.Services.AddSingleton<SyncApiClient>();
        
        builder.Services.AddScoped<IPosSyncService, PosSyncService>();
        builder.Services.AddSingleton<PosSyncBackgroundWorker>();
        builder.Services.AddSingleton<CashDeskHeartbeatService>();
        builder.Services.AddSingleton<SignalRService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SignalRService>>();
            var serverSettings = sp.GetRequiredService<ServerSettingsService>();
            var authState = sp.GetRequiredService<PosAuthState>();

            return new SignalRService(serverSettings.ApiBaseUrl, authState.Token ?? "", logger);
        });

        // Configure custom violet theme
        var violetTheme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#8b5cf6",
                Secondary = "#a78bfa",
                Tertiary = "#7c3aed",
                AppbarBackground = "#7c3aed",
                AppbarText = "#ffffff",
                DrawerBackground = "#f9f7fe",
                DrawerText = "#1f2937",
                DrawerIcon = "#7c3aed",
                Success = "#10b981",
                Info = "#3b82f6",
                Warning = "#f59e0b",
                Error = "#ef4444",
                Dark = "#1f2937",
                TextPrimary = "#1f2937",
                TextSecondary = "#6b7280",
                Background = "#ffffff",
                BackgroundGray = "#f9fafb",
                Surface = "#ffffff",
                ActionDefault = "#7c3aed",
                ActionDisabled = "#9ca3af",
                ActionDisabledBackground = "#e5e7eb"
            },
            PaletteDark = new PaletteDark()
            {
                Primary = "#8b5cf6",
                Secondary = "#a78bfa",
                Tertiary = "#7c3aed",
                AppbarBackground = "#1f2937",
                AppbarText = "#ffffff",
                DrawerBackground = "#111827",
                DrawerText = "#f3f4f6",
                DrawerIcon = "#a78bfa",
                Success = "#10b981",
                Info = "#3b82f6",
                Warning = "#f59e0b",
                Error = "#ef4444",
                Dark = "#0f172a",
                TextPrimary = "#f3f46f",
                TextSecondary = "#d1d5db",
                Background = "#0f172a",
                BackgroundGray = "#1f2937",
                Surface = "#1f2937",
                ActionDefault = "#a78bfa",
                ActionDisabled = "#4b5563",
                ActionDisabledBackground = "#374151"
            }
        };

        builder.Services.AddSingleton(violetTheme);
        builder.Services.AddMudServices();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ServiceProvider = app.Services;
        
        return app;
    }
}