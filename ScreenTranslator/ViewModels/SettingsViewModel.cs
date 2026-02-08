using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenTranslator.Models;
using ScreenTranslator.Services;

namespace ScreenTranslator.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings;

    [ObservableProperty] private string _hotkey = "Alt+Q";
    [ObservableProperty] private TargetLanguage _targetLanguage = TargetLanguage.English;
    [ObservableProperty] private AiPlatform _aiPlatform = AiPlatform.OpenAI;
    [ObservableProperty] private string _aiEndpoint = "https://api.openai.com";
    [ObservableProperty] private string _aiApiKey = "";
    [ObservableProperty] private string _aiModel = "gpt-4o-mini";
    [ObservableProperty] private string _aiVisionSystemPrompt = "";
    [ObservableProperty] private double _fontSize = 14;
    [ObservableProperty] private double _opacity = 1.0;
    [ObservableProperty] private int _preferredMonitor = 1;
    [ObservableProperty] private List<string> _monitorList = new();
    [ObservableProperty] private WindowMode _windowMode = WindowMode.Independent;
    [ObservableProperty] private SidebarPosition _sidebarPosition = SidebarPosition.Right;
    [ObservableProperty] private AppTheme _theme = AppTheme.Dark;
    [ObservableProperty] private string _accentColor = "";
    [ObservableProperty] private string _backgroundImagePath = "";
    [ObservableProperty] private double _backgroundImageOpacity = 0.15;
    [ObservableProperty] private bool _autoStart = false;

    public SettingsViewModel(SettingsService settingsService, MonitorService monitorService, int? currentMonitorIndex = null)
    {
        _settingsService = settingsService;
        _settings = _settingsService.Load();
        LoadFromSettings(_settings);
        LoadMonitorList(monitorService);

        if (currentMonitorIndex.HasValue && currentMonitorIndex.Value >= 0 && currentMonitorIndex.Value < MonitorList.Count)
            PreferredMonitor = currentMonitorIndex.Value;
    }

    private void LoadFromSettings(AppSettings s)
    {
        Hotkey = s.Hotkey;
        TargetLanguage = s.TargetLanguage;
        AiPlatform = s.AI.Platform;
        AiEndpoint = s.AI.Endpoint;
        AiApiKey = s.AI.ApiKey;
        AiModel = s.AI.Model;
        AiVisionSystemPrompt = s.AI.VisionSystemPrompt;
        FontSize = s.UI.FontSize;
        Opacity = s.UI.Opacity;
        PreferredMonitor = s.UI.PreferredMonitor;
        WindowMode = s.UI.WindowMode;
        SidebarPosition = s.UI.SidebarPosition;
        Theme = s.UI.Theme;
        AccentColor = s.UI.AccentColor;
        BackgroundImagePath = s.UI.BackgroundImagePath;
        BackgroundImageOpacity = s.UI.BackgroundImageOpacity;
        AutoStart = s.UI.AutoStart;
    }

    private void LoadMonitorList(MonitorService monitorService)
    {
        var monitors = monitorService.GetAllMonitors();
        var list = new List<string>();
        for (int i = 0; i < monitors.Count; i++)
        {
            var m = monitors[i];
            var label = m.IsPrimary ? "主显示器" : "副显示器";
            list.Add($"{i + 1}: {label} ({m.Bounds.Width}×{m.Bounds.Height})");
        }
        MonitorList = list;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.Hotkey = Hotkey;
        _settings.TargetLanguage = TargetLanguage;
        _settings.AI.Platform = AiPlatform;
        _settings.AI.Endpoint = AiEndpoint;
        _settings.AI.ApiKey = AiApiKey;
        _settings.AI.Model = AiModel;
        _settings.AI.VisionSystemPrompt = AiVisionSystemPrompt;
        _settings.UI.FontSize = FontSize;
        _settings.UI.Opacity = Opacity;
        _settings.UI.PreferredMonitor = PreferredMonitor;
        _settings.UI.WindowMode = WindowMode;
        _settings.UI.SidebarPosition = SidebarPosition;
        _settings.UI.Theme = Theme;
        _settings.UI.AccentColor = AccentColor;
        _settings.UI.BackgroundImagePath = BackgroundImagePath;
        _settings.UI.BackgroundImageOpacity = BackgroundImageOpacity;
        _settings.UI.AutoStart = AutoStart;
        _settingsService.Save(_settings);
    }

    public AppSettings GetSettings() => _settings;
}
