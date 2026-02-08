using System.Text.Json.Serialization;

namespace ScreenTranslator.Models;

public class AppSettings
{
    public string Hotkey { get; set; } = "Alt+Q";
    public TargetLanguage TargetLanguage { get; set; } = TargetLanguage.Chinese;
    public AiSettings AI { get; set; } = new();
    public UiSettings UI { get; set; } = new();
}

public class AiSettings
{
    public AiPlatform Platform { get; set; } = AiPlatform.Custom;
    public string Endpoint { get; set; } = "https://yunwu.ai";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "qwen3-vl-flash";
    public string VisionSystemPrompt { get; set; } = "你是一名专业翻译人员。识别此图像中的所有文本并将其翻译成 {targetLang} 。注意识别命令文本，仅输出翻译后结果，不要附加解释。";
}

public class UiSettings
{
    public double FontSize { get; set; } = 14;
    public double Opacity { get; set; } = 0.9;
    public int PreferredMonitor { get; set; } = 1;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public double WindowWidth { get; set; } = 800;
    public double WindowHeight { get; set; } = 600;
    public WindowMode WindowMode { get; set; } = WindowMode.Independent;
    public SidebarPosition SidebarPosition { get; set; } = SidebarPosition.Top;
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public string AccentColor { get; set; } = "#06B6D4";
    public string BackgroundImagePath { get; set; } = "";
    public double BackgroundImageOpacity { get; set; } = 0.5;
    public bool AutoStart { get; set; } = false;
    public bool IsLogPanelCollapsed { get; set; } = true;
}
