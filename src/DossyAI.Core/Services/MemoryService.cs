using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DossyAI.Core.Data;
using DossyAI.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DossyAI.Core.Services;

public class MemoryService(
    DossyAiDbContext db,
    IEmbeddingService embeddingService,
    MetadataExtractor metadataExtractor,
    ILogger<MemoryService> logger) : IMemoryService
{
    public async Task<Memory> StoreContextAsync(string content, string category, Dictionary<string, object>? metadata, CancellationToken ct = default)
    {
        var embedding = await embeddingService.GetEmbeddingAsync(content, ct);
        var extracted = await metadataExtractor.ExtractAsync(content, category, ct);

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

        db.Memories.Add(memory);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Stored memory {Id} in category {Category}", memory.Id, category);
        return memory;
    }

    public async Task<List<MemorySearchResult>> FindRelatedAsync(string query, int limit = 10, float minSimilarity = 0.7f, CancellationToken ct = default)
    {
        var queryEmbedding = await embeddingService.GetEmbeddingAsync(query, ct);
        // Note: cosine similarity scoring is done in-memory since EF Core does not support
        // vector similarity functions for the nvarchar(max) JSON-encoded embedding column.
        // This loads all active memories into memory for scoring — acceptable for moderate
        // dataset sizes but would need a dedicated vector store for large-scale deployments.
        var memories = await db.Memories
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
        var query = db.Memories.Where(m => m.Category == category && m.Status == "active");

        if (!string.IsNullOrEmpty(domainFilter))
            query = query.Where(m => m.JsonMetadata.Contains(domainFilter));

        return await query.OrderByDescending(m => m.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<(int Total, List<Memory> Items)> ListMemoryAsync(string? category = null, string? createdBy = null, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var query = db.Memories.AsQueryable();

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
        var memory = await db.Memories.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"Memory {id} not found");

        if (!string.IsNullOrEmpty(newContent))
        {
            memory.Content = newContent;
            memory.Embedding = await embeddingService.GetEmbeddingAsync(newContent, ct);
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
        await db.SaveChangesAsync(ct);
        return memory;
    }

    public async Task DeleteMemoryAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        var affected = await db.Memories
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, "archived")
                .SetProperty(m => m.UpdatedAt, DateTimeOffset.UtcNow), ct);

        if (affected == 0)
            throw new KeyNotFoundException($"Memory {id} not found");
    }

    public async Task<MemoryStats> GetStatsAsync(CancellationToken ct = default)
    {
        var total = await db.Memories.CountAsync(ct);
        var byCategory = await db.Memories
            .GroupBy(m => m.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Category, g => g.Count, ct);
        var activeCount = await db.Memories.CountAsync(m => m.Status == "active", ct);
        var archivedCount = await db.Memories.CountAsync(m => m.Status == "archived", ct);
        var oldest = await db.Memories.OrderBy(m => m.CreatedAt).Select(m => (DateTimeOffset?)m.CreatedAt).FirstOrDefaultAsync(ct);
        var newest = await db.Memories.OrderByDescending(m => m.CreatedAt).Select(m => (DateTimeOffset?)m.CreatedAt).FirstOrDefaultAsync(ct);

        return new MemoryStats
        {
            TotalMemories = total,
            ActiveMemories = activeCount,
            ArchivedMemories = archivedCount,
            ByCategory = byCategory,
            OldestMemory = oldest,
            NewestMemory = newest,
            TotalEmbeddingsStored = total,
            EmbeddingDimensions = embeddingService.Dimensions
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
