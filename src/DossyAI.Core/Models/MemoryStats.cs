namespace DossyAI.Core.Models;

public class MemoryStats
{
    public int TotalMemories { get; set; }
    public int ActiveMemories { get; set; }
    public int ArchivedMemories { get; set; }
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> ByCreatedBy { get; set; } = new();
    public DateTimeOffset? OldestMemory { get; set; }
    public DateTimeOffset? NewestMemory { get; set; }
}
