namespace ScreenTranslator.Models;

public enum TranslationProvider
{
    AIVision
}

public enum AiPlatform
{
    OpenAI,
    Claude,
    Gemini,
    Custom
}

public enum WindowMode
{
    Independent,
    Sidebar
}

public enum SidebarPosition
{
    Left,
    Right,
    Top,
    Bottom
}

public enum AppTheme
{
    Dark,
    Light
}

public enum TargetLanguage
{
    Chinese,
    English,
    Japanese,
    Korean,
    French,
    German,
    Spanish,
    Russian,
    Portuguese,
    Italian,
    Arabic,
    Thai,
    Vietnamese
}

public static class TargetLanguageExtensions
{
    public static string ToLanguageCode(this TargetLanguage lang) => lang switch
    {
        TargetLanguage.Chinese => "zh",
        TargetLanguage.English => "en",
        TargetLanguage.Japanese => "ja",
        TargetLanguage.Korean => "ko",
        TargetLanguage.French => "fr",
        TargetLanguage.German => "de",
        TargetLanguage.Spanish => "es",
        TargetLanguage.Russian => "ru",
        TargetLanguage.Portuguese => "pt",
        TargetLanguage.Italian => "it",
        TargetLanguage.Arabic => "ar",
        TargetLanguage.Thai => "th",
        TargetLanguage.Vietnamese => "vi",
        _ => "en"
    };

    public static TargetLanguage FromLanguageCode(string code) => code?.ToLower() switch
    {
        "zh" => TargetLanguage.Chinese,
        "en" => TargetLanguage.English,
        "ja" => TargetLanguage.Japanese,
        "ko" => TargetLanguage.Korean,
        "fr" => TargetLanguage.French,
        "de" => TargetLanguage.German,
        "es" => TargetLanguage.Spanish,
        "ru" => TargetLanguage.Russian,
        "pt" => TargetLanguage.Portuguese,
        "it" => TargetLanguage.Italian,
        "ar" => TargetLanguage.Arabic,
        "th" => TargetLanguage.Thai,
        "vi" => TargetLanguage.Vietnamese,
        _ => TargetLanguage.English
    };
}
