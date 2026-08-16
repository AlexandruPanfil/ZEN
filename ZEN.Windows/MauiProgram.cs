using System.Net.Http;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components.Authorization;
using ZEN.Application;
using ZEN.Infrastructure;
using ZEN.Windows.Services.Sync;
using ZEN.Windows.Services;
using ZEN.Domain.Interfaces;

namespace ZEN.Windows;

public static class MauiProgram
{
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
		builder.Services.AddSingleton<PosAuthState>();
		builder.Services.AddSingleton<AuthenticationStateProvider, PosAuthenticationStateProvider>();
		builder.Services.AddAuthorizationCore();
		builder.Services.AddHttpContextAccessor();

		// SQLite path for future use (database init disabled due to MAUI Windows ContentRootPath bug)
		var sqlitePath = Path.Combine(FileSystem.AppDataDirectory, "zen.db");
		builder.Services.AddInfrastructureWindows(sqlitePath);

		builder.Services.AddSingleton<ICurrentUserService, PosCurrentUserService>();
		builder.Services.AddSingleton<ServerSettingsService>();
		builder.Services.AddApplication();

		// Add dummy JWT config for local auth
		builder.Configuration["Jwt:Key"] = "SuperSecretKeyForLocalMauiApp12345!";
		builder.Configuration["Jwt:Issuer"] = "ZEN.POS";
		builder.Configuration["Jwt:Audience"] = "ZEN.POS";

		builder.Services.AddHttpClient<SyncApiClient>(client =>
		{
			client.BaseAddress = new Uri("http://localhost:5378/");
			client.Timeout = TimeSpan.FromSeconds(10);
		}).ConfigurePrimaryHttpMessageHandler(() =>
		{
			var handler = new HttpClientHandler();
			handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
			return handler;
		});
		builder.Services.AddScoped<IPosSyncService, PosSyncService>();
		builder.Services.AddSingleton<PosSyncBackgroundWorker>();

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
				TextPrimary = "#f3f4f6",
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

		return builder.Build();
	}
}