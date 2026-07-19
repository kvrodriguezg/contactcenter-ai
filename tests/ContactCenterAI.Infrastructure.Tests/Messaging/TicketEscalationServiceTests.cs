using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Tickets;
using ContactCenterAI.Infrastructure.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

public class TicketEscalationServiceTests
{
    private static MessagingTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseInMemoryDatabase($"escalation-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task Prepares_escalation_and_records_timestamp()
    {
        await using var context = CreateContext();
        var ticketId = Guid.NewGuid();
        context.Tickets.Add(new Ticket
        {
            Id = ticketId,
            CompanyId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            Subject = "Escalación",
            Description = "Detalle",
            Priority = TicketPriority.High,
            Status = TicketStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new TicketEscalationService(context, NullLogger<TicketEscalationService>.Instance);
        var outcome = await service.ProcessEscalationAsync(
            ticketId,
            nameof(TicketPriority.High),
            Guid.NewGuid());

        Assert.Equal(TicketEscalationOutcome.Prepared, outcome);
        var ticket = await context.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.NotNull(ticket.EscalationProcessedAt);
        Assert.Equal(TicketEscalationService.PreparedForAssignmentStatus, ticket.EscalationStatus);
    }

    [Fact]
    public async Task Is_idempotent_when_already_processed()
    {
        await using var context = CreateContext();
        var ticketId = Guid.NewGuid();
        var firstProcessedAt = DateTime.UtcNow.AddMinutes(-5);
        context.Tickets.Add(new Ticket
        {
            Id = ticketId,
            CompanyId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            Subject = "Escalación",
            Description = "Detalle",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            EscalationProcessedAt = firstProcessedAt,
            EscalationStatus = TicketEscalationService.PreparedForAssignmentStatus
        });
        await context.SaveChangesAsync();

        var service = new TicketEscalationService(context, NullLogger<TicketEscalationService>.Instance);
        var outcome = await service.ProcessEscalationAsync(
            ticketId,
            nameof(TicketPriority.Medium),
            Guid.NewGuid());

        Assert.Equal(TicketEscalationOutcome.SkippedAlreadyProcessed, outcome);
        var ticket = await context.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.Equal(firstProcessedAt, ticket.EscalationProcessedAt);
    }

    [Fact]
    public async Task Returns_not_found_for_missing_ticket()
    {
        await using var context = CreateContext();
        var service = new TicketEscalationService(context, NullLogger<TicketEscalationService>.Instance);

        var outcome = await service.ProcessEscalationAsync(
            Guid.NewGuid(),
            nameof(TicketPriority.Low),
            Guid.NewGuid());

        Assert.Equal(TicketEscalationOutcome.NotFound, outcome);
    }
}
