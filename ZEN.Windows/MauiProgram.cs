using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using ZEN.Application;
using ZEN.Infrastructure;
using ZEN.Infrastructure.Persistence;
using ZEN.Windows.Services.Sync;
using ZEN.Windows.Services;

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
		builder.Services.AddHttpClient<SyncApiClient>(client =>
		{
			var baseUrl = builder.Configuration["Sync:ApiBaseUrl"] ?? "https://localhost:7305/";
			client.BaseAddress = new Uri(baseUrl);
		});
		builder.Services.AddScoped<IPosSyncService, PosSyncService>();
		builder.Services.AddSingleton<PosSyncBackgroundWorker>();

		// MudBlazor
		builder.Services.AddMudServices();

		// Application and Infrastructure layers
		var sqlitePath = Path.Combine(FileSystem.AppDataDirectory, "zen.db");
		builder.Services.AddInfrastructureWindows(sqlitePath);
		builder.Services.AddApplication();

		// Add dummy JWT config for local auth
		builder.Configuration["Jwt:Key"] = "SuperSecretKeyForLocalMauiApp12345!";
		builder.Configuration["Jwt:Issuer"] = "ZEN.POS";
		builder.Configuration["Jwt:Audience"] = "ZEN.POS";

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		_ = Task.Run(async () =>
		{
			try
			{
				using var scope = app.Services.CreateScope();
				var db = scope.ServiceProvider.GetRequiredService<ZenDbContext>();
				var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ZEN.Infrastructure.Identity.AppUser>>();
				var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<ZEN.Infrastructure.Identity.AppRole>>();

				// For development: delete and recreate database to include new sync tables
				// In production, you would use migrations instead
				var dbPath = Path.Combine(FileSystem.AppDataDirectory, "zen.db");
				if (File.Exists(dbPath))
				{
					await db.Database.EnsureDeletedAsync();
				}

				await db.Database.EnsureCreatedAsync();
				await ZEN.Infrastructure.Persistence.Seeders.DbSeeder.SeedAsync(db, userManager, roleManager);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"DB init failed: {ex}");
			}
		});

		app.Services.GetRequiredService<PosSyncBackgroundWorker>().Start();

		return app;
	}
}
