using System.Text.Json;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="TicketCreatedEvent"/> and runs the async escalation stage via
/// <see cref="ITicketEscalationService"/>. Idempotent: tickets with EscalationProcessedAt set are skipped.
/// </summary>
public class TicketCreatedEventConsumer : RabbitMqConsumerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<TicketCreatedEventConsumer> _logger;

    public TicketCreatedEventConsumer(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<TicketCreatedEventConsumer> logger)
        : base(connection, scopeFactory, settings.Value, logger)
    {
        _logger = logger;
    }

    protected override string QueueName => Settings.TicketEscalationQueue;

    protected override async Task HandleMessageAsync(
        string messageJson,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<TicketCreatedEvent>(messageJson, SerializerOptions)
            ?? throw new InvalidOperationException("Payload TicketCreatedEvent inválido.");

        _logger.LogInformation(
            "Evento TicketCreated recibido: TicketId={TicketId}, CompanyId={CompanyId}, Priority={Priority}, CorrelationId={CorrelationId}",
            @event.TicketId,
            @event.CompanyId,
            @event.Priority,
            @event.CorrelationId);

        var escalationService = services.GetRequiredService<ITicketEscalationService>();
        var outcome = await escalationService.ProcessEscalationAsync(
            @event.TicketId,
            @event.Priority,
            @event.CorrelationId,
            cancellationToken);

        _logger.LogInformation(
            "Escalación por evento del ticket {TicketId} finalizada con resultado {Outcome} (CorrelationId={CorrelationId})",
            @event.TicketId,
            outcome,
            @event.CorrelationId);

        if (outcome == TicketEscalationOutcome.NotFound)
        {
            // Permanent: do not retry forever for a missing ticket.
            _logger.LogWarning(
                "Ticket {TicketId} no encontrado tras TicketCreatedEvent; se confirma sin reintento",
                @event.TicketId);
        }
    }
}
