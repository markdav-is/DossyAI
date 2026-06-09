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

public class MetadataExtractor(HttpClient http, IConfiguration config, ILogger<MetadataExtractor> logger)
{
    public async Task<ExtractedMetadata> ExtractAsync(string content, string category, CancellationToken ct = default)
    {
        try
        {
            var provider = config["DossyAi:LLM:Provider"] ?? "ollama";
            if (provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                return await ExtractViaOllamaAsync(content, category, ct);
            else
                return await ExtractViaOpenRouterAsync(content, category, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Metadata extraction failed, returning defaults");
            return new ExtractedMetadata(category, [], [], [], "active");
        }
    }

    private async Task<ExtractedMetadata> ExtractViaOllamaAsync(string content, string category, CancellationToken ct)
    {
        var baseUrl = config["DossyAi:LLM:OllamaBaseUrl"] ?? "http://localhost:11434";
        var model = config["DossyAi:LLM:Model"] ?? "mistral";
        var prompt = BuildPrompt(content, category);
        var payload = JsonSerializer.Serialize(new { model, prompt, stream = false });
        var resp = await http.PostAsync($"{baseUrl}/api/generate",
            new StringContent(payload, Encoding.UTF8, "application/json"), ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var responseText = doc.RootElement.GetProperty("response").GetString() ?? "{}";
        return ParseLlmResponse(responseText, category);
    }

    private async Task<ExtractedMetadata> ExtractViaOpenRouterAsync(string content, string category, CancellationToken ct)
    {
        var apiKey = config["DossyAi:LLM:OpenRouterApiKey"] ?? string.Empty;
        var model = config["DossyAi:LLM:Model"] ?? "mistralai/mistral-7b-instruct";
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
        var resp = await http.SendAsync(request, ct);
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

    private static string[] ParseStringArray(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var el) && el.ValueKind == JsonValueKind.Array
            ? el.EnumerateArray().Select(e => e.GetString() ?? string.Empty).Where(s => s.Length > 0).ToArray()
            : [];

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
                    Domain: ParseStringArray(root, "domain"),
                    Stakeholders: ParseStringArray(root, "stakeholders"),
                    ActionItems: ParseStringArray(root, "action_items"),
                    Status: root.TryGetProperty("status", out var st) ? st.GetString() ?? "active" : "active"
                );
            }
        }
        catch { }
        return new ExtractedMetadata(category, [], [], [], "active");
    }
}
