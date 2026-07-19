using System.Text.Json;

namespace ZEN.Windows.Services;

public class ServerSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5378/";
}

public class ServerSettingsService
{
    private readonly string _settingsFilePath;
    private ServerSettings _settings = new();

    public ServerSettingsService()
    {
        var appDataDir = FileSystem.AppDataDirectory;
        _settingsFilePath = Path.Combine(appDataDir, "server-settings.json");
        Load();
    }

    public string ApiBaseUrl => _settings.ApiBaseUrl;

    public void UpdateApiBaseUrl(string url)
    {
        if (!url.EndsWith("/")) url += "/";
        _settings.ApiBaseUrl = url;
        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                _settings = JsonSerializer.Deserialize<ServerSettings>(json) ?? new();
            }
        }
        catch
        {
            _settings = new();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}