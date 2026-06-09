namespace DossyAI.Core.Models;

public class MemoryStats
{
    public int TotalMemories { get; set; }
    public int ActiveMemories { get; set; }
    public int ArchivedMemories { get; set; }
    public Dictionary<string, int> ByCategory { get; set; } = [];
    public Dictionary<string, int> ByCreatedBy { get; set; } = [];
    public int TotalEmbeddingsStored { get; set; }
    public int EmbeddingDimensions { get; set; }
    public DateTimeOffset? OldestMemory { get; set; }
    public DateTimeOffset? NewestMemory { get; set; }
}
