namespace DossyAI.Core.Models;

public class MemoryResponse
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public object? Metadata { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public float? SimilarityScore { get; set; }
}

public class MemorySearchResult
{
    public Memory Memory { get; set; } = null!;
    public float SimilarityScore { get; set; }
}
