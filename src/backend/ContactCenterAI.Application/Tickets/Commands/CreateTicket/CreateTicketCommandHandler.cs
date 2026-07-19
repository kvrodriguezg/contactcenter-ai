using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Tickets.Common;
using ContactCenterAI.Application.Tickets.DTOs;
using ContactCenterAI.Application.Tickets.Events;
using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tickets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Tickets.Commands.CreateTicket;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, TicketDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITicketEventPublisher _ticketEventPublisher;

    public CreateTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITicketEventPublisher ticketEventPublisher)
    {
        _context = context;
        _currentUserService = currentUserService;
        _ticketEventPublisher = ticketEventPublisher;
    }

    public async Task<TicketDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        TicketAuthorization.EnsureAuthenticated(_currentUserService);

        var role = _currentUserService.Role!.Value;
        if (role is not (Role.Agent or Role.CompanyAdmin or Role.SuperAdmin))
        {
            throw new UnauthorizedAccessException("No tiene permisos para crear tickets.");
        }

        var companyId = TicketAuthorization.ResolveCompanyIdForCreate(
            _currentUserService,
            request.CompanyId);

        var companyExists = await _context.Companies
            .AnyAsync(c => c.Id == companyId, cancellationToken);

        if (!companyExists)
        {
            throw new FluentValidation.ValidationException("La empresa del usuario no existe.");
        }

        if (request.ConversationId.HasValue)
        {
            await EnsureConversationAuthorizedAsync(
                request.ConversationId.Value,
                companyId,
                cancellationToken);
        }

        var priority = Enum.Parse<TicketPriority>(request.Priority, ignoreCase: true);
        var now = DateTime.UtcNow;

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CreatedByUserId = _currentUserService.UserId!.Value,
            ConversationId = request.ConversationId,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            Priority = priority,
            Status = TicketStatus.Pending,
            CreatedAt = now
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);

        var correlationId = Guid.NewGuid();
        await _ticketEventPublisher.PublishTicketCreatedAsync(
            new TicketCreatedEvent(
                ticket.Id,
                ticket.CompanyId,
                ticket.CreatedByUserId,
                ticket.Subject,
                ticket.Priority.ToString(),
                now,
                correlationId),
            cancellationToken);

        var created = await TicketAuthorization.IncludeDetails(_context.Tickets.AsNoTracking())
            .FirstAsync(t => t.Id == ticket.Id, cancellationToken);

        return TicketAuthorization.ToDto(created);
    }

    private async Task EnsureConversationAuthorizedAsync(
        Guid conversationId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null)
        {
            throw new KeyNotFoundException("La conversación especificada no existe.");
        }

        if (conversation.CompanyId != companyId)
        {
            throw new UnauthorizedAccessException(
                "La conversación no pertenece a la empresa del ticket.");
        }

        var role = _currentUserService.Role!.Value;
        if (role == Role.Agent && conversation.UserId != _currentUserService.UserId)
        {
            throw new UnauthorizedAccessException(
                "No tiene permisos para asociar esta conversación al ticket.");
        }

        if (role == Role.CompanyAdmin &&
            (_currentUserService.CompanyId is null || conversation.CompanyId != _currentUserService.CompanyId))
        {
            throw new UnauthorizedAccessException(
                "No tiene permisos para asociar esta conversación al ticket.");
        }
    }
}
