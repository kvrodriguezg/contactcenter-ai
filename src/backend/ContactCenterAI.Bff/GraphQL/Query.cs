using ContactCenterAI.Bff.Clients;
using ContactCenterAI.Bff.GraphQL.Models;
using ContactCenterAI.Bff.Security;
using HotChocolate.Authorization;

namespace ContactCenterAI.Bff.GraphQL;

[Authorize]
public class Query
{
    public async Task<CurrentUser?> MeAsync(
        [Service] BffCallerContext access,
        CancellationToken ct)
    {
        return await access.RequireCallerAsync(ct);
    }

    public async Task<IReadOnlyList<Company>> CompaniesAsync(
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        BffCallerContext.EnsureSuperAdmin(caller);
        return await core.GetCompaniesAsync(ct);
    }

    public async Task<Company?> CompanyByIdAsync(
        Guid id,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        BffCallerContext.EnsureCanAccessCompany(caller, id);
        return await core.GetCompanyByIdAsync(id, ct);
    }

    public async Task<IReadOnlyList<User>> UsersAsync(
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        BffCallerContext.EnsureCanListUsers(caller);
        var users = await core.GetUsersAsync(ct);
        return BffCallerContext.FilterUsers(caller, users);
    }

    public async Task<User?> UserByIdAsync(
        Guid id,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        var user = await core.GetUserByIdAsync(id, ct);
        if (user is null)
        {
            return null;
        }

        if (!BffCallerContext.IsSuperAdmin(caller)
            && (user.CompanyId is null || user.CompanyId != caller.CompanyId))
        {
            return null;
        }

        return user;
    }

    public async Task<IReadOnlyList<Document>> DocumentsAsync(
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        var documents = await core.GetDocumentsAsync(ct);
        return BffCallerContext.FilterByCompany(caller, documents, d => d.CompanyId);
    }

    public async Task<Document?> DocumentByIdAsync(
        Guid id,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        var document = await core.GetDocumentByIdAsync(id, ct);
        if (document is null)
        {
            return null;
        }

        if (!BffCallerContext.IsSuperAdmin(caller) && document.CompanyId != caller.CompanyId)
        {
            return null;
        }

        return document;
    }

    public async Task<IReadOnlyList<Conversation>> ConversationsAsync(
        [Service] BffCallerContext access,
        [Service] IChatApiClient chat,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        var conversations = await chat.GetConversationsAsync(ct);
        return BffCallerContext.FilterByCompany(caller, conversations, c => c.CompanyId);
    }

    public async Task<Conversation?> ConversationByIdAsync(
        Guid id,
        [Service] BffCallerContext access,
        [Service] IChatApiClient chat,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        var conversation = await chat.GetConversationByIdAsync(id, ct);
        if (conversation is null)
        {
            return null;
        }

        if (!BffCallerContext.IsSuperAdmin(caller) && conversation.CompanyId != caller.CompanyId)
        {
            return null;
        }

        return conversation;
    }

    public async Task<IReadOnlyList<Ticket>> TicketsAsync(
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        var tickets = await core.GetTicketsAsync(ct);
        return BffCallerContext.FilterByCompany(caller, tickets, t => t.CompanyId);
    }

    public async Task<Ticket?> TicketByIdAsync(
        Guid id,
        [Service] BffCallerContext access,
        [Service] ICoreApiClient core,
        CancellationToken ct)
    {
        var caller = await access.RequireCallerAsync(ct);
        var ticket = await core.GetTicketByIdAsync(id, ct);
        if (ticket is null)
        {
            return null;
        }

        if (!BffCallerContext.IsSuperAdmin(caller) && ticket.CompanyId != caller.CompanyId)
        {
            return null;
        }

        return ticket;
    }
}
