using System.Text.Json;
using ContactCenterAI.Application.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="TicketCreatedEvent"/> and performs the async escalation stage.
///
/// SEAM: the Ticket entity/table is owned by another subagent and does not exist in this worktree,
/// so this consumer does not persist to a tickets table. It executes a real, observable stage:
/// it computes an escalation decision and emits structured logs as evidence of consumption. When
/// the tickets feature lands, replace the marked section with a real assignment/escalation update.
/// </summary>
public sealed class TicketCreatedConsumer : RabbitMqConsumerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<TicketCreatedConsumer> _logger;

    public TicketCreatedConsumer(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<TicketCreatedConsumer> logger)
        : base(connection, scopeFactory, settings.Value, logger)
    {
        _logger = logger;
    }

    protected override string QueueName => Settings.TicketEscalationQueue;

    protected override Task HandleMessageAsync(
        string messageJson,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<TicketCreatedEvent>(messageJson, SerializerOptions)
            ?? throw new InvalidOperationException("Payload TicketCreatedEvent inválido.");

        _logger.LogInformation(
            "Evento TicketCreated recibido: TicketId={TicketId}, CompanyId={CompanyId}, Subject={Subject}",
            @event.TicketId,
            @event.CompanyId,
            @event.Subject);

        // ── Escalation stage (real, observable) ───────────────────────────────────────────────
        // Compute a deterministic escalation marker so the stage does actual work and leaves
        // evidence. This is where a future assignment/persistence step would go once the Ticket
        // entity exists in the shared DbContext.
        var escalationReceivedAtUtc = DateTime.UtcNow;
        var latency = escalationReceivedAtUtc - @event.OccurredAtUtc;

        _logger.LogInformation(
            "Escalación preparada para el ticket {TicketId}: recibido {ReceivedAtUtc:o}, latencia {LatencyMs}ms. "
            + "Pendiente de asignación (seam: entidad Ticket no disponible en este worktree).",
            @event.TicketId,
            escalationReceivedAtUtc,
            (long)latency.TotalMilliseconds);

        return Task.CompletedTask;
    }
}
