using DossyAI.Core.Services;
using ModelContextProtocol.Server;

namespace DossyAI.Mcp.Tools;

[McpServerToolType]
public class FindRelatedTool
{
    private readonly IMemoryService _memoryService;

    public FindRelatedTool(IMemoryService memoryService)
    {
        _memoryService = memoryService;
    }

    /// <summary>Find memories semantically related to the given query using cosine similarity</summary>
    [McpServerTool(Name = "find_related")]
    public async Task<object> FindRelatedAsync(
        string query,
        int limit = 10,
        float minSimilarity = 0.7f,
        CancellationToken cancellationToken = default)
    {
        var results = await _memoryService.FindRelatedAsync(query, limit, minSimilarity, cancellationToken);
        return new
        {
            count = results.Count,
            results = results.Select(r => new
            {
                id = r.Memory.Id,
                content = r.Memory.Content,
                category = r.Memory.Category,
                similarity_score = r.SimilarityScore,
                created_at = r.Memory.CreatedAt,
                status = r.Memory.Status,
                metadata = r.Memory.JsonMetadata
            })
        };
    }
}
