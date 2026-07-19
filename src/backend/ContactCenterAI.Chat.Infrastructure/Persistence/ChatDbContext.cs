using ContactCenterAI.Chat.Domain;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Chat.Infrastructure.Persistence;

public class ChatDbContext : DbContext, Application.Common.Interfaces.IChatDbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(builder =>
        {
            builder.ToTable("conversations");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.ExternalUserId).IsRequired();
            builder.Property(c => c.UserEmail).IsRequired().HasMaxLength(256);
            builder.Property(c => c.CompanyId).IsRequired();
            builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
            builder.Property(c => c.CreatedAt).IsRequired();

            builder.HasIndex(c => c.CompanyId);
            builder.HasIndex(c => c.ExternalUserId);
            builder.HasIndex(c => new { c.CompanyId, c.ExternalUserId });

            builder.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationMessage>(builder =>
        {
            builder.ToTable("conversation_messages");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(m => m.Content).IsRequired();
            builder.Property(m => m.SourcesJson);
            builder.Property(m => m.CreatedAt).IsRequired();

            builder.HasIndex(m => m.ConversationId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
