using Microsoft.Extensions.DependencyInjection;
using ZEN.Windows.Services;

namespace ZEN.Windows;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState)
	{
		// Ensure TokenSyncService is instantiated to subscribe to PosAuthState.Changed
		var services = MauiProgram.ServiceProvider;
		if (services != null)
		{
			var tokenSyncService = services.GetRequiredService<TokenSyncService>();
		}
		
		return new Microsoft.Maui.Controls.Window(new MainPage()) { Title = "ZEN.Windows" };
	}
}
