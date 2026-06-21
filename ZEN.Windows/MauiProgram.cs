using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using ZEN.Application;
using ZEN.Infrastructure;

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

		// MudBlazor
		builder.Services.AddMudServices();

		// Application and Infrastructure layers
		var sqlitePath = Path.Combine(FileSystem.AppDataDirectory, "zen.db");
		builder.Services.AddInfrastructureWindows(sqlitePath);
		builder.Services.AddApplication();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
