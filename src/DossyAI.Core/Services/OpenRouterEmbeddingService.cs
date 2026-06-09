using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DossyAI.Core.Services;

public class OpenRouterEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<OpenRouterEmbeddingService> _logger;

    public int Dimensions { get; }

    public OpenRouterEmbeddingService(HttpClient http, IConfiguration config, ILogger<OpenRouterEmbeddingService> logger)
    {
        _http = http;
        var apiKey = config["DossyAi:Embedding:OpenRouterApiKey"] ?? string.Empty;
        _model = config["DossyAi:Embedding:Model"] ?? "openai/text-embedding-3-small";
        Dimensions = int.TryParse(config["DossyAi:Embedding:Dimensions"], out var d) ? d : 1536;
        _logger = logger;
        _http.BaseAddress = new Uri("https://openrouter.ai");
        if (!string.IsNullOrEmpty(apiKey))
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new { model = _model, input = text });
        var response = await _http.PostAsync("/api/v1/embeddings",
            new StringContent(payload, Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
        return arr.EnumerateArray().Select(e => e.GetSingle()).ToArray();
    }
}
