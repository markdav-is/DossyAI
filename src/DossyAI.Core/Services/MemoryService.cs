using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DossyAI.Core.Data;
using DossyAI.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DossyAI.Core.Services;

public class MemoryService : IMemoryService
{
    private readonly DossyAiDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly MetadataExtractor _metadataExtractor;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(
        DossyAiDbContext db,
        IEmbeddingService embeddingService,
        MetadataExtractor metadataExtractor,
        ILogger<MemoryService> logger)
    {
        _db = db;
        _embeddingService = embeddingService;
        _metadataExtractor = metadataExtractor;
        _logger = logger;
    }

    public async Task<Memory> StoreContextAsync(string content, string category, Dictionary<string, object>? metadata, CancellationToken ct = default)
    {
        var embedding = await _embeddingService.GetEmbeddingAsync(content, ct);
        var extracted = await _metadataExtractor.ExtractAsync(content, category, ct);

        var mergedMetadata = new Dictionary<string, object>
        {
            ["type"] = extracted.Type,
            ["domain"] = extracted.Domain,
            ["stakeholders"] = extracted.Stakeholders,
            ["action_items"] = extracted.ActionItems,
            ["status"] = extracted.Status
        };
        if (metadata != null)
        {
            foreach (var kv in metadata)
                mergedMetadata[kv.Key] = kv.Value;
        }

        var memory = new Memory
        {
            Content = content,
            Embedding = embedding,
            Category = category,
            JsonMetadata = JsonSerializer.Serialize(mergedMetadata),
            ContentHash = ComputeHash(content),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.Memories.Add(memory);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Stored memory {Id} in category {Category}", memory.Id, category);
        return memory;
    }

    public async Task<List<MemorySearchResult>> FindRelatedAsync(string query, int limit = 10, float minSimilarity = 0.7f, CancellationToken ct = default)
    {
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, ct);
        var memories = await _db.Memories
            .Where(m => m.Status != "archived")
            .ToListAsync(ct);

        var results = memories
            .Select(m => new MemorySearchResult
            {
                Memory = m,
                SimilarityScore = CosineSimilarity(queryEmbedding, m.Embedding)
            })
            .Where(r => r.SimilarityScore >= minSimilarity)
            .OrderByDescending(r => r.SimilarityScore)
            .Take(limit)
            .ToList();

        return results;
    }

    public async Task<List<Memory>> RetrieveByCategoryAsync(string category, string? domainFilter = null, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var query = _db.Memories.Where(m => m.Category == category && m.Status == "active");

        if (!string.IsNullOrEmpty(domainFilter))
            query = query.Where(m => m.JsonMetadata.Contains(domainFilter));

        return await query.OrderByDescending(m => m.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<(int Total, List<Memory> Items)> ListMemoryAsync(string? category = null, string? createdBy = null, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var query = _db.Memories.AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(m => m.Category == category);
        if (!string.IsNullOrEmpty(createdBy))
            query = query.Where(m => m.CreatedBy == createdBy);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(m => m.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (total, items);
    }

    public async Task<Memory> UpdateMemoryAsync(Guid id, string? newContent = null, Dictionary<string, object>? newMetadata = null, string? status = null, CancellationToken ct = default)
    {
        var memory = await _db.Memories.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"Memory {id} not found");

        if (!string.IsNullOrEmpty(newContent))
        {
            memory.Content = newContent;
            memory.Embedding = await _embeddingService.GetEmbeddingAsync(newContent, ct);
            memory.ContentHash = ComputeHash(newContent);
        }

        if (newMetadata != null)
        {
            var existing = JsonSerializer.Deserialize<Dictionary<string, object>>(memory.JsonMetadata) ?? new();
            foreach (var kv in newMetadata)
                existing[kv.Key] = kv.Value;
            memory.JsonMetadata = JsonSerializer.Serialize(existing);
        }

        if (!string.IsNullOrEmpty(status))
            memory.Status = status;

        memory.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return memory;
    }

    public async Task DeleteMemoryAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        var memory = await _db.Memories.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"Memory {id} not found");

        memory.Status = "archived";
        memory.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(reason))
        {
            var meta = JsonSerializer.Deserialize<Dictionary<string, object>>(memory.JsonMetadata) ?? new();
            meta["archive_reason"] = reason;
            memory.JsonMetadata = JsonSerializer.Serialize(meta);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<MemoryStats> GetStatsAsync(CancellationToken ct = default)
    {
        var all = await _db.Memories.ToListAsync(ct);
        return new MemoryStats
        {
            TotalMemories = all.Count,
            ActiveMemories = all.Count(m => m.Status == "active"),
            ArchivedMemories = all.Count(m => m.Status == "archived"),
            ByCategory = all.GroupBy(m => m.Category).ToDictionary(g => g.Key, g => g.Count()),
            ByCreatedBy = all.GroupBy(m => m.CreatedBy).ToDictionary(g => g.Key, g => g.Count()),
            OldestMemory = all.Any() ? all.Min(m => m.CreatedAt) : null,
            NewestMemory = all.Any() ? all.Max(m => m.CreatedAt) : null
        };
    }

    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0) return 0f;
        var len = Math.Min(a.Length, b.Length);
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        if (normA == 0 || normB == 0) return 0f;
        return (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
