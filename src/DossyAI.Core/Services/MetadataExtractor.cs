using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DossyAI.Core.Services;

public record ExtractedMetadata(
    string Type,
    string[] Domain,
    string[] Stakeholders,
    string[] ActionItems,
    string Status);

public class MetadataExtractor
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<MetadataExtractor> _logger;

    public MetadataExtractor(HttpClient http, IConfiguration config, ILogger<MetadataExtractor> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<ExtractedMetadata> ExtractAsync(string content, string category, CancellationToken ct = default)
    {
        try
        {
            var provider = _config["DossyAi:LLM:Provider"] ?? "ollama";
            if (provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                return await ExtractViaOllamaAsync(content, category, ct);
            else
                return await ExtractViaOpenRouterAsync(content, category, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Metadata extraction failed, returning defaults");
            return new ExtractedMetadata(category, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), "active");
        }
    }

    private async Task<ExtractedMetadata> ExtractViaOllamaAsync(string content, string category, CancellationToken ct)
    {
        var baseUrl = _config["DossyAi:LLM:OllamaBaseUrl"] ?? "http://localhost:11434";
        var model = _config["DossyAi:LLM:Model"] ?? "mistral";
        var prompt = BuildPrompt(content, category);
        var payload = JsonSerializer.Serialize(new { model, prompt, stream = false });
        var resp = await _http.PostAsync($"{baseUrl}/api/generate",
            new StringContent(payload, Encoding.UTF8, "application/json"), ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var responseText = doc.RootElement.GetProperty("response").GetString() ?? "{}";
        return ParseLlmResponse(responseText, category);
    }

    private async Task<ExtractedMetadata> ExtractViaOpenRouterAsync(string content, string category, CancellationToken ct)
    {
        var apiKey = _config["DossyAi:LLM:OpenRouterApiKey"] ?? string.Empty;
        var model = _config["DossyAi:LLM:Model"] ?? "mistralai/mistral-7b-instruct";
        var prompt = BuildPrompt(content, category);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        if (!string.IsNullOrEmpty(apiKey))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        var payload = JsonSerializer.Serialize(new
        {
            model,
            messages = new[] { new { role = "user", content = prompt } }
        });
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        var resp = await _http.SendAsync(request, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var responseText = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";
        return ParseLlmResponse(responseText, category);
    }

    private static string BuildPrompt(string content, string category) =>
        $$"""
        Extract metadata from the following content. Return only valid JSON with this exact schema:
        {"type": "string", "domain": ["string"], "stakeholders": ["string"], "action_items": ["string"], "status": "string"}

        Category hint: {{category}}
        Content: {{content}}

        JSON:
        """;

    private static ExtractedMetadata ParseLlmResponse(string text, string category)
    {
        try
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                var jsonStr = text[start..(end + 1)];
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;
                return new ExtractedMetadata(
                    Type: root.TryGetProperty("type", out var t) ? t.GetString() ?? category : category,
                    Domain: root.TryGetProperty("domain", out var d) ? d.EnumerateArray().Select(e => e.GetString() ?? "").ToArray() : Array.Empty<string>(),
                    Stakeholders: root.TryGetProperty("stakeholders", out var s) ? s.EnumerateArray().Select(e => e.GetString() ?? "").ToArray() : Array.Empty<string>(),
                    ActionItems: root.TryGetProperty("action_items", out var a) ? a.EnumerateArray().Select(e => e.GetString() ?? "").ToArray() : Array.Empty<string>(),
                    Status: root.TryGetProperty("status", out var st) ? st.GetString() ?? "active" : "active"
                );
            }
        }
        catch { }
        return new ExtractedMetadata(category, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), "active");
    }
}
