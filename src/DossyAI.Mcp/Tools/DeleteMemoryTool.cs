using DossyAI.Core.Services;
using ModelContextProtocol.Server;

namespace DossyAI.Mcp.Tools;

[McpServerToolType]
public class DeleteMemoryTool
{
    private readonly IMemoryService _memoryService;

    public DeleteMemoryTool(IMemoryService memoryService)
    {
        _memoryService = memoryService;
    }

    /// <summary>Soft-delete (archive) a memory by ID with optional reason</summary>
    [McpServerTool(Name = "delete_memory")]
    public async Task<object> DeleteMemoryAsync(
        Guid id,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await _memoryService.DeleteMemoryAsync(id, reason, cancellationToken);
        return new
        {
            id,
            status = "archived",
            archived_at = DateTimeOffset.UtcNow,
            reason = reason ?? "deleted via MCP tool"
        };
    }
}
