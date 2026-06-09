using DossyAI.Core.Services;
using ModelContextProtocol.Server;

namespace DossyAI.Mcp.Tools;

[McpServerToolType]
public class UpdateMemoryTool(IMemoryService memoryService)
{
    /// <summary>Update an existing memory's content, metadata, or status</summary>
    [McpServerTool(Name = "update_memory")]
    public async Task<object> UpdateMemoryAsync(
        Guid id,
        string? newContent = null,
        Dictionary<string, object>? newMetadata = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var memory = await memoryService.UpdateMemoryAsync(id, newContent, newMetadata, status, cancellationToken);
        return new
        {
            id = memory.Id,
            updated_at = memory.UpdatedAt,
            status = memory.Status,
            content_hash = memory.ContentHash
        };
    }
}
