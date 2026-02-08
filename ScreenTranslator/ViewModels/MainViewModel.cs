using CommunityToolkit.Mvvm.ComponentModel;
using ScreenTranslator.Models;

namespace ScreenTranslator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _originalText = string.Empty;

    [ObservableProperty]
    private string _translatedText = string.Empty;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private bool _isTranslating;

    [ObservableProperty]
    private string _currentLanguagePair = "";

    [ObservableProperty]
    private long _elapsedMs;

    [ObservableProperty]
    private double _fontSize = 14;

    [ObservableProperty]
    private string _errorDetail = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _apiLog = string.Empty;

    [ObservableProperty]
    private bool _isTopmost;

    [ObservableProperty]
    private bool _isClickThrough;

    [ObservableProperty]
    private bool _showOriginalText = true;

    [ObservableProperty]
    private WindowMode _windowMode = WindowMode.Independent;

    [ObservableProperty]
    private SidebarPosition _sidebarPosition = SidebarPosition.Right;

    [ObservableProperty]
    private bool _isToolbarPopupOpen;

    public void UpdateResult(TranslationResult result)
    {
        OriginalText = result.OriginalText;
        ElapsedMs = result.ElapsedMilliseconds;

        if (result.IsSuccess)
        {
            TranslatedText = result.TranslatedText;
            CurrentLanguagePair = $"{result.SourceLanguage} → {result.TargetLanguage}";
            StatusText = $"翻译完成 ({result.ElapsedMilliseconds}ms)";
            HasError = false;
            ErrorDetail = string.Empty;
        }
        else
        {
            TranslatedText = $"翻译失败: {result.ErrorMessage}";
            StatusText = "翻译失败";
            HasError = true;
            ErrorDetail = result.ErrorDetail ?? result.ErrorMessage ?? "未知错误";
        }

        AppendApiLog(result);
    }

    public void AppendApiLog(TranslationResult result)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        var platform = result.Platform ?? "Unknown";
        var model = result.Model ?? "Unknown";
        var url = result.RequestUrl ?? "-";
        var httpCode = result.HttpStatusCode.HasValue ? $"HTTP {result.HttpStatusCode}" : "HTTP -";
        var elapsed = $"{result.ElapsedMilliseconds}ms";
        var status = result.IsSuccess ? "OK" : "FAIL";

        var line = $"[{time}] {platform}/{model} | {url} | {httpCode} | {elapsed} | {status}";

        ApiLog = string.IsNullOrEmpty(ApiLog) ? line : line + "\n" + ApiLog;
    }

    public void SetStatus(string status)
    {
        StatusText = status;
    }
}
