using DossyAI.Core.Services;
using ModelContextProtocol.Server;

namespace DossyAI.Mcp.Tools;

[McpServerToolType]
public class RetrieveByCategoryTool(IMemoryService memoryService)
{
    /// <summary>Retrieve memories filtered by category with optional domain filter</summary>
    [McpServerTool(Name = "retrieve_by_category")]
    public async Task<object> RetrieveByCategoryAsync(
        string category,
        string? domainFilter = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var memories = await memoryService.RetrieveByCategoryAsync(category, domainFilter, skip, take, cancellationToken);
        return new
        {
            category,
            count = memories.Count,
            skip,
            take,
            items = memories.Select(m => new
            {
                id = m.Id,
                content = m.Content,
                category = m.Category,
                created_at = m.CreatedAt,
                status = m.Status,
                metadata = m.JsonMetadata
            })
        };
    }
}
