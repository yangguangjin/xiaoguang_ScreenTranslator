namespace ScreenTranslator.Core.Models;

public class TranslationResult
{
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetail { get; set; }
    public string? Platform { get; set; }
    public string? Model { get; set; }
    public string? RequestUrl { get; set; }
    public int? HttpStatusCode { get; set; }
}
