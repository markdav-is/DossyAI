namespace DossyAI.Core.Models;

public class MemorySearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
    public float MinSimilarity { get; set; } = 0.7f;
    public string? Category { get; set; }
}
