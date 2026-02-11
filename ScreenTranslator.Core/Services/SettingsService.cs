using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenTranslator.Core.Models;
using ScreenTranslator.Core.Services.Interfaces;

namespace ScreenTranslator.Core.Services;

public class SettingsService
{
    private readonly string _settingsDir;
    private readonly string _settingsFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public SettingsService(IPlatformService platformService)
    {
        _settingsDir = platformService.GetSettingsDirectory();
        _settingsFile = Path.Combine(_settingsDir, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsFile))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(_settingsDir);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsFile, json);
    }
}
