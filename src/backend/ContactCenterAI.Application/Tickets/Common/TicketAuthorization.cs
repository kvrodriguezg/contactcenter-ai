using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Tickets.DTOs;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tickets.Common;

internal static class TicketAuthorization
{
    public static void EnsureAuthenticated(ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null || currentUser.Role is null)
        {
            throw new UnauthorizedAccessException(
                currentUser.AuthorizationFailureMessage ?? "Usuario no autenticado.");
        }
    }

    public static bool IsAdmin(Role role) =>
        role is Role.SuperAdmin or Role.CompanyAdmin;

    public static void EnsureCanManage(ICurrentUserService currentUser)
    {
        EnsureAuthenticated(currentUser);

        if (!IsAdmin(currentUser.Role!.Value))
        {
            throw new UnauthorizedAccessException(
                "No tiene permisos para gestionar tickets. Solo roles administrativos pueden asignar o cambiar estado.");
        }
    }

    public static IQueryable<Ticket> ApplyCompanyScope(
        IQueryable<Ticket> query,
        ICurrentUserService currentUser)
    {
        if (currentUser.Role == Role.SuperAdmin)
        {
            return query;
        }

        if (currentUser.CompanyId is null)
        {
            return query.Where(_ => false);
        }

        return query.Where(t => t.CompanyId == currentUser.CompanyId);
    }

    public static void EnsureTicketCompanyAccess(Ticket ticket, ICurrentUserService currentUser)
    {
        if (currentUser.Role == Role.SuperAdmin)
        {
            return;
        }

        if (currentUser.CompanyId is null || ticket.CompanyId != currentUser.CompanyId)
        {
            throw new UnauthorizedAccessException("No tiene permisos para acceder a este ticket.");
        }
    }

    public static Guid ResolveCompanyIdForCreate(
        ICurrentUserService currentUser,
        Guid? requestedCompanyId)
    {
        EnsureAuthenticated(currentUser);

        if (currentUser.CompanyId is null)
        {
            throw new UnauthorizedAccessException(
                "El usuario autenticado no tiene empresa asociada para crear tickets.");
        }

        // Never trust a CompanyId from the client for tenant users.
        if (requestedCompanyId.HasValue && requestedCompanyId.Value != currentUser.CompanyId.Value)
        {
            throw new UnauthorizedAccessException(
                "No puede crear tickets para otra empresa. El CompanyId se toma del usuario autenticado.");
        }

        return currentUser.CompanyId.Value;
    }

    public static TicketDto ToDto(Ticket ticket) => new()
    {
        Id = ticket.Id,
        CompanyId = ticket.CompanyId,
        CompanyName = ticket.Company?.Name ?? string.Empty,
        CreatedByUserId = ticket.CreatedByUserId,
        CreatedByEmail = ticket.CreatedByUser?.Email ?? string.Empty,
        CreatedByName = ticket.CreatedByUser?.Name,
        ConversationId = ticket.ConversationId,
        Subject = ticket.Subject,
        Description = ticket.Description,
        Priority = ticket.Priority.ToString(),
        Status = ticket.Status.ToString(),
        AssignedToUserId = ticket.AssignedToUserId,
        AssignedToEmail = ticket.AssignedToUser?.Email,
        AssignedToName = ticket.AssignedToUser?.Name,
        Resolution = ticket.Resolution,
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt,
        ResolvedAt = ticket.ResolvedAt
    };

    public static IQueryable<Ticket> IncludeDetails(IQueryable<Ticket> query) =>
        query
            .Include(t => t.Company)
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser);
}
