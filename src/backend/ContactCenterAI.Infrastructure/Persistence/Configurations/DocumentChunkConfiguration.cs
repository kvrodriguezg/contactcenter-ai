using ContactCenterAI.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace ContactCenterAI.Infrastructure.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public const int DefaultEmbeddingDimensions = 1536;

    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .IsRequired();

        builder.Property(c => c.ChunkIndex)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.Embedding)
            .HasColumnType($"vector({DefaultEmbeddingDimensions})")
            .HasConversion(
                value => value == null ? null : new Vector(value),
                value => value == null ? null : value.ToArray())
            .Metadata.SetValueComparer(new ValueComparer<float[]?>(
                (left, right) =>
                    (left == null && right == null) ||
                    (left != null && right != null && left.SequenceEqual(right)),
                value => value == null
                    ? 0
                    : value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value == null ? null : value.ToArray()));

        builder.Property(c => c.EmbeddingModel)
            .HasMaxLength(100);

        builder.HasIndex(c => c.DocumentId);

        builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex })
            .IsUnique();

        builder.HasOne(c => c.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
