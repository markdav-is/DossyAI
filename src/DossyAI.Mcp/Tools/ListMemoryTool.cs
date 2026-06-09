using DossyAI.Core.Services;
using ModelContextProtocol.Server;

namespace DossyAI.Mcp.Tools;

[McpServerToolType]
public class ListMemoryTool
{
    private readonly IMemoryService _memoryService;

    public ListMemoryTool(IMemoryService memoryService)
    {
        _memoryService = memoryService;
    }

    /// <summary>List stored memories with optional filtering by category and creator</summary>
    [McpServerTool(Name = "list_memory")]
    public async Task<object> ListMemoryAsync(
        string? category = null,
        string? createdBy = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var (total, items) = await _memoryService.ListMemoryAsync(category, createdBy, skip, take, cancellationToken);
        return new
        {
            total,
            skip,
            take,
            items = items.Select(m => new
            {
                id = m.Id,
                content = m.Content,
                category = m.Category,
                created_by = m.CreatedBy,
                created_at = m.CreatedAt,
                updated_at = m.UpdatedAt,
                status = m.Status,
                source = m.Source,
                metadata = m.JsonMetadata
            })
        };
    }
}
