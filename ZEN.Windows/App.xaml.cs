using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZEN.Windows.Services;
using System.IO;

namespace ZEN.Windows;

public partial class App : Microsoft.Maui.Controls.Application
{
    private static readonly string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZEN.Windows", "startup.log");

    public App()
    {
        InitializeComponent();
        
        // Ensure log directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        
        File.AppendAllText(LogPath, $"[{DateTime.Now}] App constructor\n");
        
        // Add global exception handling
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            File.AppendAllText(LogPath, $"[{DateTime.Now}] UNHANDLED EXCEPTION: {ex}\n");
            System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {ex}");
        };
        
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now}] UNOBSERVED TASK EXCEPTION: {e.Exception}\n");
            System.Diagnostics.Debug.WriteLine($"UNOBSERVED TASK EXCEPTION: {e.Exception}");
            e.SetObserved();
        };
    }

    protected override Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now}] CreateWindow called\n");
            
            // Ensure TokenSyncService is instantiated to subscribe to PosAuthState.Changed
            var services = MauiProgram.ServiceProvider;
            if (services != null)
            {
                var tokenSyncService = services.GetRequiredService<TokenSyncService>();
                File.AppendAllText(LogPath, $"[{DateTime.Now}] TokenSyncService retrieved\n");
            }
            
            var mainPage = new MainPage();
            File.AppendAllText(LogPath, $"[{DateTime.Now}] MainPage created\n");
            
            var window = new Microsoft.Maui.Controls.Window(mainPage) { Title = "ZEN.Windows" };
            File.AppendAllText(LogPath, $"[{DateTime.Now}] Window created\n");
            
            return window;
        }
        catch (Exception ex)
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now}] EXCEPTION in CreateWindow: {ex}\n");
            throw;
        }
    }
}