using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DossyAI.Core.Services;

public class OpenRouterEmbeddingService(HttpClient http, IConfiguration config, ILogger<OpenRouterEmbeddingService> logger) : IEmbeddingService
{
    private readonly string _model = config["DossyAi:Embedding:Model"] ?? "openai/text-embedding-3-small";

    public int Dimensions { get; } = int.TryParse(config["DossyAi:Embedding:Dimensions"], out var d) ? d : 1536;

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var apiKey = config["DossyAi:Embedding:OpenRouterApiKey"] ?? string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/embeddings");
        if (!string.IsNullOrEmpty(apiKey))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        var payload = JsonSerializer.Serialize(new { model = _model, input = text });
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
        return arr.EnumerateArray().Select(e => e.GetSingle()).ToArray();
    }
}
