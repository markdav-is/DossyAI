using System.Text.Json;
using DossyAI.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DossyAI.Core.Data;

public class DossyAiDbContext : DbContext
{
    public DossyAiDbContext(DbContextOptions<DossyAiDbContext> options) : base(options) { }

    public DbSet<Memory> Memories => Set<Memory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var embeddingConverter = new ValueConverter<float[], string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<float>()
        );

        modelBuilder.Entity<Memory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Embedding)
                .HasConversion(embeddingConverter)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.JsonMetadata).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Content).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
        });
    }
}
