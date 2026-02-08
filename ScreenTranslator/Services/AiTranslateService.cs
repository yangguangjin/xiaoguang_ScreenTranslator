using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenTranslator.Models;

namespace ScreenTranslator.Services;

public class AiTranslateService : IDisposable
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private class ApiCallInfo
    {
        public string Platform { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string RequestUrl { get; set; } = string.Empty;
        public int? HttpStatusCode { get; set; }
    }

    public async Task<TranslationResult> TranslateImageAsync(
        Bitmap image, string targetLanguage, AiSettings settings)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var logInfo = new ApiCallInfo
        {
            Platform = settings.Platform.ToString(),
            Model = settings.Model
        };

        try
        {
            var base64 = BitmapToBase64(image);
            var prompt = settings.VisionSystemPrompt.Replace("{targetLang}", targetLanguage);
            var response = settings.Platform switch
            {
                AiPlatform.Claude => await CallClaudeWithImageAsync(base64, prompt, settings, logInfo),
                AiPlatform.Gemini => await CallGeminiWithImageAsync(base64, prompt, settings, logInfo),
                _ => await CallOpenAiWithImageAsync(base64, prompt, settings, logInfo)
            };

            sw.Stop();
            return new TranslationResult
            {
                OriginalText = "[截图翻译]",
                TranslatedText = response,
                TargetLanguage = targetLanguage,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                IsSuccess = true,
                Platform = logInfo.Platform,
                Model = logInfo.Model,
                RequestUrl = logInfo.RequestUrl,
                HttpStatusCode = logInfo.HttpStatusCode
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            var statusCode = logInfo.HttpStatusCode;
            if (statusCode == null && ex is HttpRequestException httpEx && httpEx.StatusCode.HasValue)
                statusCode = (int)httpEx.StatusCode.Value;

            return new TranslationResult
            {
                OriginalText = "[截图翻译]",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ErrorDetail = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}",
                Platform = logInfo.Platform,
                Model = logInfo.Model,
                RequestUrl = logInfo.RequestUrl,
                HttpStatusCode = statusCode
            };
        }
    }

    private static string BitmapToBase64(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }

    private async Task<string> CallOpenAiWithImageAsync(string base64, string prompt, AiSettings settings, ApiCallInfo logInfo)
    {
        var endpoint = settings.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/v1/chat/completions";
        logInfo.RequestUrl = url;

        var body = new
        {
            model = settings.Model,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64}" } }
                    }
                }
            },
            temperature = 0.3
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        logInfo.HttpStatusCode = (int)response.StatusCode;
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {json}", null, response.StatusCode);

        using var doc = JsonDocument.Parse(json);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");
        return ExtractOpenAiText(message);
    }

    private async Task<string> CallClaudeWithImageAsync(string base64, string prompt, AiSettings settings, ApiCallInfo logInfo)
    {
        var endpoint = settings.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/v1/messages";
        logInfo.RequestUrl = url;

        var body = new
        {
            model = settings.Model,
            max_tokens = 4096,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new { type = "base64", media_type = "image/png", data = base64 }
                        },
                        new { type = "text", text = prompt }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-api-key", settings.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        logInfo.HttpStatusCode = (int)response.StatusCode;
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {json}", null, response.StatusCode);

        using var doc = JsonDocument.Parse(json);
        var contentArray = doc.RootElement.GetProperty("content");
        return ExtractClaudeText(contentArray);
    }

    private async Task<string> CallGeminiWithImageAsync(string base64, string prompt, AiSettings settings, ApiCallInfo logInfo)
    {
        var endpoint = settings.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/v1beta/models/{settings.Model}:generateContent?key={settings.ApiKey}";
        logInfo.RequestUrl = url;

        var body = new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { inline_data = new { mime_type = "image/png", data = base64 } }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        logInfo.HttpStatusCode = (int)response.StatusCode;
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {json}", null, response.StatusCode);

        using var doc = JsonDocument.Parse(json);
        var parts = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts");
        return ExtractGeminiText(parts);
    }

    private static string ExtractClaudeText(JsonElement contentArray)
    {
        string result = string.Empty;
        foreach (var block in contentArray.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var typeProp)
                && typeProp.GetString() == "text"
                && block.TryGetProperty("text", out var textProp))
            {
                result = textProp.GetString() ?? string.Empty;
            }
        }
        return result;
    }

    private static string ExtractOpenAiText(JsonElement message)
    {
        var contentEl = message.GetProperty("content");

        if (contentEl.ValueKind == JsonValueKind.String)
            return contentEl.GetString() ?? string.Empty;

        if (contentEl.ValueKind == JsonValueKind.Array)
        {
            string result = string.Empty;
            foreach (var block in contentEl.EnumerateArray())
            {
                if (block.ValueKind == JsonValueKind.String)
                {
                    result = block.GetString() ?? string.Empty;
                    continue;
                }
                if (block.TryGetProperty("type", out var typeProp)
                    && typeProp.GetString() == "text"
                    && block.TryGetProperty("text", out var textProp))
                {
                    result = textProp.GetString() ?? string.Empty;
                }
            }
            return result;
        }

        return string.Empty;
    }

    private static string ExtractGeminiText(JsonElement parts)
    {
        string result = string.Empty;
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("thought", out var thoughtProp)
                && thoughtProp.GetBoolean())
                continue;

            if (part.TryGetProperty("text", out var textProp))
                result = textProp.GetString() ?? string.Empty;
        }
        return result;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
