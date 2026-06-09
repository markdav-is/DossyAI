namespace DossyAI.Core.Models;

public class Memory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string Category { get; set; } = string.Empty;
    public string JsonMetadata { get; set; } = "{}";
    public string CreatedBy { get; set; } = "agent";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "active";
    public Guid? SupersedesId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string Source { get; set; } = "mcp";
}
