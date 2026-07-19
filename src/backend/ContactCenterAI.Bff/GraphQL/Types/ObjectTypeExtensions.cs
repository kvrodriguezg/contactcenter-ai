using ContactCenterAI.Bff.Clients;
using ContactCenterAI.Bff.GraphQL.Models;
using ContactCenterAI.Bff.Security;

namespace ContactCenterAI.Bff.GraphQL.Types;

[ExtendObjectType(typeof(Company))]
public class CompanyTypeExtensions
{
    public async Task<IReadOnlyList<User>> UsersAsync(
        [Parent] Company company,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        BffCallerContext.EnsureCanAccessCompany(caller, company.Id);

        if (BffCallerContext.IsAgent(caller))
        {
            return [];
        }

        var users = await core.GetUsersAsync(ct);
        return users.Where(u => u.CompanyId == company.Id).ToList();
    }

    public async Task<IReadOnlyList<Document>> DocumentsAsync(
        [Parent] Company company,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        BffCallerContext.EnsureCanAccessCompany(caller, company.Id);
        var documents = await core.GetDocumentsAsync(ct);
        return documents.Where(d => d.CompanyId == company.Id).ToList();
    }

    public async Task<IReadOnlyList<Ticket>> TicketsAsync(
        [Parent] Company company,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        BffCallerContext.EnsureCanAccessCompany(caller, company.Id);
        var tickets = await core.GetTicketsAsync(ct);
        return tickets.Where(t => t.CompanyId == company.Id).ToList();
    }
}

[ExtendObjectType(typeof(User))]
public class UserTypeExtensions
{
    public async Task<Company?> CompanyAsync(
        [Parent] User user,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        if (user.CompanyId is null)
        {
            return null;
        }

        var caller = await access.RequireCallerAsync(ct);
        BffCallerContext.EnsureCanAccessCompany(caller, user.CompanyId.Value);
        return await core.GetCompanyByIdAsync(user.CompanyId.Value, ct);
    }
}

[ExtendObjectType(typeof(Document))]
public class DocumentTypeExtensions
{
    public async Task<Company?> CompanyAsync(
        [Parent] Document document,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        BffCallerContext.EnsureCanAccessCompany(caller, document.CompanyId);
        return await core.GetCompanyByIdAsync(document.CompanyId, ct);
    }
}

[ExtendObjectType(typeof(Ticket))]
public class TicketTypeExtensions
{
    public async Task<Company?> CompanyAsync(
        [Parent] Ticket ticket,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        BffCallerContext.EnsureCanAccessCompany(caller, ticket.CompanyId);
        return await core.GetCompanyByIdAsync(ticket.CompanyId, ct);
    }

    public async Task<User?> CreatedByAsync(
        [Parent] Ticket ticket,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        await access.RequireCallerAsync(ct);
        return await core.GetUserByIdAsync(ticket.CreatedByUserId, ct);
    }

    public async Task<User?> AssignedToAsync(
        [Parent] Ticket ticket,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        if (ticket.AssignedToUserId is null)
        {
            return null;
        }

        await access.RequireCallerAsync(ct);
        return await core.GetUserByIdAsync(ticket.AssignedToUserId.Value, ct);
    }
}

[ExtendObjectType(typeof(Conversation))]
public class ConversationTypeExtensions
{
    public async Task<IReadOnlyList<ConversationMessage>> MessagesAsync(
        [Parent] Conversation conversation,
        [Service] BffCallerContext access,
        [Service] IChatApiClient chat,
        CancellationToken ct)
    {
        if (conversation.Messages is not null)
        {
            return conversation.Messages;
        }

        var caller = await access.RequireCallerAsync(ct);
        var detail = await chat.GetConversationByIdAsync(conversation.Id, ct);
        if (detail is null)
        {
            return [];
        }

        if (!BffCallerContext.IsSuperAdmin(caller) && detail.CompanyId != caller.CompanyId)
        {
            return [];
        }

        return detail.Messages ?? [];
    }
}
