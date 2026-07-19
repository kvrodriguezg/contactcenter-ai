using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Chat;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using ContactCenterAI.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tests.Common;

/// <summary>
/// Minimal in-memory <see cref="IApplicationDbContext"/> for handler unit tests.
/// Maps Company/User/Ticket/Conversation for tickets and admin feature tests.
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

    public DbSet<Ticket> Tickets => Set<Ticket>();

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
            builder.Ignore(c => c.Tickets);
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
            builder.Ignore(u => u.CreatedTickets);
            builder.Ignore(u => u.AssignedTickets);
            builder.HasOne(u => u.Company)
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conversation>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
            builder.Ignore(c => c.Messages);
            builder.Ignore(c => c.Company);
            builder.Ignore(c => c.User);
        });

        modelBuilder.Entity<Ticket>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Subject).IsRequired().HasMaxLength(200);
            builder.Property(t => t.Description).IsRequired().HasMaxLength(4000);
            builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(50);
            builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(t => t.Resolution).HasMaxLength(4000);
            builder.HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(t => t.Conversation)
                .WithMany()
                .HasForeignKey(t => t.ConversationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Ignore<RefreshToken>();
        modelBuilder.Ignore<Document>();
        modelBuilder.Ignore<DocumentChunk>();
        modelBuilder.Ignore<ConversationMessage>();
    }
}
