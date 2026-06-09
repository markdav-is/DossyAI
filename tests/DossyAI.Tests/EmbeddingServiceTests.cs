using DossyAI.Core.Services;
using Xunit;

namespace DossyAI.Tests;

public class EmbeddingServiceTests
{
    [Fact]
    public void CosineSimilarity_SameVector_ReturnsOne()
    {
        var v = new float[] { 1f, 2f, 3f };
        var result = MemoryService.CosineSimilarity(v, v);
        Assert.True(Math.Abs(result - 1.0f) < 0.0001f);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        var v1 = new float[] { 1f, 0f, 0f };
        var v2 = new float[] { -1f, 0f, 0f };
        var result = MemoryService.CosineSimilarity(v1, v2);
        Assert.True(Math.Abs(result - (-1.0f)) < 0.0001f);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var v1 = new float[] { 1f, 0f, 0f };
        var v2 = new float[] { 0f, 1f, 0f };
        var result = MemoryService.CosineSimilarity(v1, v2);
        Assert.True(Math.Abs(result) < 0.0001f);
    }

    [Fact]
    public void CosineSimilarity_EmptyVector_ReturnsZero()
    {
        var result = MemoryService.CosineSimilarity(Array.Empty<float>(), new float[] { 1f, 2f });
        Assert.Equal(0f, result);
    }

    [Theory]
    [InlineData(new float[] { 1f, 1f, 0f }, new float[] { 1f, 0f, 1f })]
    public void CosineSimilarity_PartialOverlap_ReturnsBetweenZeroAndOne(float[] a, float[] b)
    {
        var result = MemoryService.CosineSimilarity(a, b);
        Assert.True(result >= 0f && result <= 1f);
    }
}
