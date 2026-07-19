using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Chat;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

/// <summary>
/// Minimal in-memory <see cref="IApplicationDbContext"/> for exercising document-processing
/// idempotency without pgvector. Only Documents/DocumentChunks are mapped; the pgvector Embedding
/// column is ignored so the EF Core InMemory provider can be used.
/// </summary>
public class MessagingTestDbContext : DbContext, IApplicationDbContext
{
    public MessagingTestDbContext(DbContextOptions<MessagingTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(builder =>
        {
            builder.HasKey(d => d.Id);
            builder.Ignore(d => d.Company);
            builder.Ignore(d => d.UploadedByUser);
            builder.Ignore(d => d.Chunks);
        });

        modelBuilder.Entity<DocumentChunk>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Ignore(c => c.Embedding);
            builder.Ignore(c => c.Document);
        });

        modelBuilder.Ignore<Company>();
        modelBuilder.Ignore<User>();
        modelBuilder.Ignore<RefreshToken>();
        modelBuilder.Ignore<Conversation>();
        modelBuilder.Ignore<ConversationMessage>();
    }
}
