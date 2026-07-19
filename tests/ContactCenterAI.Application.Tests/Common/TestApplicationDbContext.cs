using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Chat;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Common;

/// <summary>
/// Minimal in-memory <see cref="IApplicationDbContext"/> for handler unit tests.
/// Mirrors the mapping approach used by the Infrastructure test context: only the
/// entities exercised by the admin feature (Company/User) are mapped; the rest are ignored.
/// </summary>
public class TestApplicationDbContext : DbContext, IApplicationDbContext
{
    public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options)
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
        modelBuilder.Entity<Company>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50);
            builder.Ignore(c => c.Users);
            builder.Ignore(c => c.Documents);
            builder.Ignore(c => c.Conversations);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
            builder.Property(u => u.Name).HasMaxLength(200);
            builder.Property(u => u.PasswordHash).HasMaxLength(500);
            builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(50);
            builder.Property(u => u.ExternalSubject).HasMaxLength(256);
            builder.Property(u => u.AuthenticationProvider).HasConversion<string>().HasMaxLength(50);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Ignore(u => u.RefreshTokens);
            builder.Ignore(u => u.UploadedDocuments);
            builder.Ignore(u => u.Conversations);
            builder.HasOne(u => u.Company)
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Ignore<RefreshToken>();
        modelBuilder.Ignore<Document>();
        modelBuilder.Ignore<DocumentChunk>();
        modelBuilder.Ignore<Conversation>();
        modelBuilder.Ignore<ConversationMessage>();
    }
}
