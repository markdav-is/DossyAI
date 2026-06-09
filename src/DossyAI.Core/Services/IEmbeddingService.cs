namespace DossyAI.Core.Services;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
    int Dimensions { get; }
}
