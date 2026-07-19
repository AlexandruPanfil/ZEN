using System.Net.Http;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
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

		builder.Services.AddMudServices();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}