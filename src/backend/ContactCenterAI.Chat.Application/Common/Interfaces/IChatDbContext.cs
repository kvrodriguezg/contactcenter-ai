using ContactCenterAI.Chat.Domain;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Chat.Application.Common.Interfaces;

public interface IChatDbContext
{
    DbSet<Conversation> Conversations { get; }

    DbSet<ConversationMessage> ConversationMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
