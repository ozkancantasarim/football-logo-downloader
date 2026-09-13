using FootballLogoDownloader.Models;
using System.IO;
using System.Text.Json;

namespace FootballLogoDownloader.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FootballLogoDownloader");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefaults();
            }
        }
        catch
        {
            // Corrupt settings should never prevent the app from starting.
        }

        return CreateDefaults();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, _jsonOptions));
        }
        catch
        {
            // Settings persistence is non-critical.
        }
    }

    private static AppSettings CreateDefaults()
    {
        var isTurkish = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("tr", StringComparison.OrdinalIgnoreCase);

        return new AppSettings
        {
            Language = isTurkish ? "tr" : "en",
            Theme = "System",
            OutputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Football-Logo-Downloads"),
            LastCountrySlug = "turkey"
        };
    }
}
