using DossyAI.Core.Models;
using DossyAI.Core.Services;
using DossyAI.Mcp.Tools;
using Moq;
using Xunit;

namespace DossyAI.Tests;

public class McpToolTests
{
    private static Mock<IMemoryService> CreateMockService() => new Mock<IMemoryService>();

    private static Memory CreateTestMemory(string content = "Test", string category = "test") =>
        new Memory
        {
            Id = Guid.NewGuid(),
            Content = content,
            Category = category,
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ContentHash = "abc123"
        };

    [Fact]
    public async Task StoreContextTool_ShouldReturnIdAndStatus()
    {
        var mock = CreateMockService();
        var memory = CreateTestMemory("Some content", "notes");
        mock.Setup(s => s.StoreContextAsync("Some content", "notes", null, default))
            .ReturnsAsync(memory);

        var tool = new StoreContextTool(mock.Object);
        var result = await tool.StoreContextAsync("Some content", "notes");

        Assert.NotNull(result);
        var dict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.Equal(memory.Id, dict["id"]);
        Assert.Equal("stored", dict["status"]);
    }

    [Fact]
    public async Task FindRelatedTool_ShouldReturnResults()
    {
        var mock = CreateMockService();
        var searchResults = new List<MemorySearchResult>
        {
            new() { Memory = CreateTestMemory(), SimilarityScore = 0.9f }
        };
        mock.Setup(s => s.FindRelatedAsync("query", 10, 0.7f, default))
            .ReturnsAsync(searchResults);

        var tool = new FindRelatedTool(mock.Object);
        var result = await tool.FindRelatedAsync("query");

        Assert.NotNull(result);
        var dict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.Equal(1, dict["count"]);
    }

    [Fact]
    public async Task DeleteMemoryTool_ShouldReturnArchivedStatus()
    {
        var mock = CreateMockService();
        var id = Guid.NewGuid();
        mock.Setup(s => s.DeleteMemoryAsync(id, "test", default)).Returns(Task.CompletedTask);

        var tool = new DeleteMemoryTool(mock.Object);
        var result = await tool.DeleteMemoryAsync(id, "test");

        Assert.NotNull(result);
        var dict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.Equal("archived", dict["status"]);
        Assert.Equal(id, dict["id"]);
    }

    [Fact]
    public async Task GetMemoryStatsTool_ShouldReturnStats()
    {
        var mock = CreateMockService();
        var stats = new MemoryStats
        {
            TotalMemories = 5,
            ActiveMemories = 4,
            ArchivedMemories = 1
        };
        mock.Setup(s => s.GetStatsAsync(default)).ReturnsAsync(stats);

        var tool = new GetMemoryStatsTool(mock.Object);
        var result = await tool.GetMemoryStatsAsync();

        Assert.NotNull(result);
        var dict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.Equal(5, dict["total_memories"]);
        Assert.Equal(4, dict["active_memories"]);
    }

    [Fact]
    public async Task UpdateMemoryTool_ShouldReturnUpdatedMemory()
    {
        var mock = CreateMockService();
        var memory = CreateTestMemory("Updated", "test");
        mock.Setup(s => s.UpdateMemoryAsync(memory.Id, "Updated", null, null, default))
            .ReturnsAsync(memory);

        var tool = new UpdateMemoryTool(mock.Object);
        var result = await tool.UpdateMemoryAsync(memory.Id, "Updated");

        Assert.NotNull(result);
        var dict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.Equal(memory.Id, dict["id"]);
    }

    [Fact]
    public async Task ListMemoryTool_ShouldReturnPaginatedResults()
    {
        var mock = CreateMockService();
        var memories = new List<Memory> { CreateTestMemory(), CreateTestMemory() };
        mock.Setup(s => s.ListMemoryAsync(null, null, 0, 20, default))
            .ReturnsAsync((2, memories));

        var tool = new ListMemoryTool(mock.Object);
        var result = await tool.ListMemoryAsync();

        Assert.NotNull(result);
        var dict = result.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(result));
        Assert.Equal(2, dict["total"]);
    }
}
