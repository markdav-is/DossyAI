using DossyAI.Core.Data;
using DossyAI.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DossyAI.Tests;

public class MemoryServiceTests
{
    private static DossyAiDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<DossyAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DossyAiDbContext(options);
    }

    private static (IMemoryService service, DossyAiDbContext db) CreateService(float[]? embedding = null)
    {
        var db = CreateInMemoryDb();
        var mockEmbedding = new Mock<IEmbeddingService>();
        mockEmbedding.Setup(e => e.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding ?? new float[] { 1f, 0f, 0f });
        mockEmbedding.Setup(e => e.Dimensions).Returns(3);

        var mockExtractor = new Mock<MetadataExtractor>(null!, null!, null!);
        // Use a concrete MetadataExtractor with a mocked http client - skip via real instance  
        // For tests, we use a workaround: create a subclass or test the service directly
        var loggerFactory = LoggerFactory.Create(_ => { });
        var logger = loggerFactory.CreateLogger<MemoryService>();

        var mockHttp = new System.Net.Http.HttpClient(new MockHttpHandler());
        var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        mockConfig.Setup(c => c["DossyAi:LLM:Provider"]).Returns("ollama");
        var extractorLogger = loggerFactory.CreateLogger<MetadataExtractor>();
        var extractor = new MetadataExtractor(mockHttp, mockConfig.Object, extractorLogger);

        var service = new MemoryService(db, mockEmbedding.Object, extractor, logger);
        return (service, db);
    }

    [Fact]
    public async Task StoreContextAsync_ShouldPersistMemory()
    {
        var (service, db) = CreateService();

        var memory = await service.StoreContextAsync("Test content", "test-category", null);

        Assert.NotEqual(Guid.Empty, memory.Id);
        Assert.Equal("Test content", memory.Content);
        Assert.Equal("test-category", memory.Category);
        Assert.Equal("active", memory.Status);
        Assert.NotEmpty(memory.ContentHash);

        var stored = await db.Memories.FindAsync(memory.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task FindRelatedAsync_ShouldReturnSimilarMemories()
    {
        var (service, _) = CreateService(new float[] { 1f, 0f, 0f });

        await service.StoreContextAsync("Related content", "category1", null);
        await service.StoreContextAsync("Another item", "category2", null);

        var results = await service.FindRelatedAsync("query", limit: 10, minSimilarity: 0.5f);

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.SimilarityScore >= 0.5f));
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnCorrectCounts()
    {
        var (service, _) = CreateService();

        await service.StoreContextAsync("Content 1", "cat1", null);
        await service.StoreContextAsync("Content 2", "cat1", null);
        await service.StoreContextAsync("Content 3", "cat2", null);

        var stats = await service.GetStatsAsync();

        Assert.Equal(3, stats.TotalMemories);
        Assert.Equal(3, stats.ActiveMemories);
        Assert.Equal(0, stats.ArchivedMemories);
        Assert.Equal(2, stats.ByCategory["cat1"]);
        Assert.Equal(1, stats.ByCategory["cat2"]);
    }

    [Fact]
    public async Task DeleteMemoryAsync_ShouldArchiveMemory()
    {
        var (service, db) = CreateService();
        var memory = await service.StoreContextAsync("To be deleted", "test", null);

        await service.DeleteMemoryAsync(memory.Id, "test reason");

        var stored = await db.Memories.FindAsync(memory.Id);
        Assert.Equal("archived", stored!.Status);
    }

    [Fact]
    public async Task UpdateMemoryAsync_ShouldUpdateContent()
    {
        var (service, db) = CreateService();
        var memory = await service.StoreContextAsync("Original content", "test", null);
        var originalHash = memory.ContentHash; // capture before update (same object reference in EF InMemory)

        var updated = await service.UpdateMemoryAsync(memory.Id, newContent: "Updated content");

        Assert.Equal("Updated content", updated.Content);
        Assert.NotEqual(originalHash, updated.ContentHash);
    }
}

// Minimal HTTP handler for tests
class MockHttpHandler : System.Net.Http.HttpMessageHandler
{
    protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
        System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent("{\"response\": \"{}\"}")
        };
        return Task.FromResult(response);
    }
}
