using DossyAI.Core.Services;
using ModelContextProtocol.Server;

namespace DossyAI.Mcp.Tools;

[McpServerToolType]
public class StoreContextTool(IMemoryService memoryService)
{
    /// <summary>Store a piece of intelligence or context with semantic embedding for future retrieval</summary>
    [McpServerTool(Name = "store_context")]
    public async Task<object> StoreContextAsync(
        string content,
        string category,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var memory = await memoryService.StoreContextAsync(content, category, metadata, cancellationToken);
        return new
        {
            id = memory.Id,
            created_at = memory.CreatedAt,
            embedding_dimensions = 1536,
            status = "stored",
            category = memory.Category,
            content_hash = memory.ContentHash
        };
    }
}
