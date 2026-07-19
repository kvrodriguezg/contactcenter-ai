using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Chat;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using ContactCenterAI.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

/// <summary>
/// Minimal in-memory <see cref="IApplicationDbContext"/> for messaging/escalation tests without pgvector.
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

    public DbSet<Ticket> Tickets => Set<Ticket>();

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

        modelBuilder.Entity<Ticket>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Ignore(t => t.Company);
            builder.Ignore(t => t.CreatedByUser);
            builder.Ignore(t => t.AssignedToUser);
            builder.Ignore(t => t.Conversation);
            builder.Property(t => t.Priority).HasConversion<string>();
            builder.Property(t => t.Status).HasConversion<string>();
        });

        modelBuilder.Ignore<Company>();
        modelBuilder.Ignore<User>();
        modelBuilder.Ignore<RefreshToken>();
        modelBuilder.Ignore<Conversation>();
        modelBuilder.Ignore<ConversationMessage>();
    }
}
