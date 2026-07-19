using ContactCenterAI.Domain.Chat;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using ContactCenterAI.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }

    DbSet<User> Users { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Document> Documents { get; }

    DbSet<DocumentChunk> DocumentChunks { get; }

    DbSet<Conversation> Conversations { get; }

    DbSet<ConversationMessage> ConversationMessages { get; }

    DbSet<Ticket> Tickets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
