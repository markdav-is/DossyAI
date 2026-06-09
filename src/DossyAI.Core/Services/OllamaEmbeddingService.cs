using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DossyAI.Core.Services;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public int Dimensions { get; }

    public OllamaEmbeddingService(HttpClient http, IConfiguration config, ILogger<OllamaEmbeddingService> logger)
    {
        _http = http;
        _baseUrl = config["DossyAi:Embedding:OllamaBaseUrl"] ?? "http://localhost:11434";
        _model = config["DossyAi:Embedding:Model"] ?? "nomic-embed-text";
        Dimensions = int.TryParse(config["DossyAi:Embedding:Dimensions"], out var d) ? d : 1536;
        _logger = logger;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new { model = _model, prompt = text });
        var response = await _http.PostAsync($"{_baseUrl}/api/embeddings",
            new StringContent(payload, Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var arr = doc.RootElement.GetProperty("embedding");
        return arr.EnumerateArray().Select(e => e.GetSingle()).ToArray();
    }
}
