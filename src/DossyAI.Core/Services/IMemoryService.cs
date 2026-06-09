using DossyAI.Core.Models;

namespace DossyAI.Core.Services;

public interface IMemoryService
{
    Task<Memory> StoreContextAsync(string content, string category, Dictionary<string, object>? metadata, CancellationToken ct = default);
    Task<List<MemorySearchResult>> FindRelatedAsync(string query, int limit = 10, float minSimilarity = 0.7f, CancellationToken ct = default);
    Task<List<Memory>> RetrieveByCategoryAsync(string category, string? domainFilter = null, int skip = 0, int take = 20, CancellationToken ct = default);
    Task<(int Total, List<Memory> Items)> ListMemoryAsync(string? category = null, string? createdBy = null, int skip = 0, int take = 20, CancellationToken ct = default);
    Task<Memory> UpdateMemoryAsync(Guid id, string? newContent = null, Dictionary<string, object>? newMetadata = null, string? status = null, CancellationToken ct = default);
    Task DeleteMemoryAsync(Guid id, string? reason = null, CancellationToken ct = default);
    Task<MemoryStats> GetStatsAsync(CancellationToken ct = default);
}
