using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenTranslator.Models;

namespace ScreenTranslator.Services;

public class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenTranslator");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TargetLanguageJsonConverter()
        }
    };

    public AppSettings Load()
    {
        if (!File.Exists(SettingsFile))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFile, json);
    }

    /// <summary>
    /// 向后兼容旧配置文件中 TargetLanguage 为语言代码字符串（如 "en"）的情况
    /// </summary>
    private class TargetLanguageJsonConverter : JsonConverter<TargetLanguage>
    {
        public override TargetLanguage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value == null)
                return TargetLanguage.English;

            // 先尝试枚举名（如 "English"）
            if (Enum.TryParse<TargetLanguage>(value, true, out var enumValue))
                return enumValue;

            // 再尝试语言代码（如 "en"）— 向后兼容
            return TargetLanguageExtensions.FromLanguageCode(value);
        }

        public override void Write(Utf8JsonWriter writer, TargetLanguage value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
