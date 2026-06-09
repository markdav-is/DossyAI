using DossyAI.Core.Services;
using ModelContextProtocol.Server;

namespace DossyAI.Mcp.Tools;

[McpServerToolType]
public class GetMemoryStatsTool
{
    private readonly IMemoryService _memoryService;

    public GetMemoryStatsTool(IMemoryService memoryService)
    {
        _memoryService = memoryService;
    }

    /// <summary>Get statistics about stored memories including counts by category and creator</summary>
    [McpServerTool(Name = "get_memory_stats")]
    public async Task<object> GetMemoryStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _memoryService.GetStatsAsync(cancellationToken);
        return new
        {
            total_memories = stats.TotalMemories,
            active_memories = stats.ActiveMemories,
            archived_memories = stats.ArchivedMemories,
            by_category = stats.ByCategory,
            by_created_by = stats.ByCreatedBy,
            oldest_memory = stats.OldestMemory,
            newest_memory = stats.NewestMemory
        };
    }
}
