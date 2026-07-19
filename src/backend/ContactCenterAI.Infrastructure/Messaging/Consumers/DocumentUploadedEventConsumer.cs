using System.Text.Json;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="DocumentUploadedEvent"/> and triggers the existing document processing
/// pipeline (extraction → chunking → embeddings) via <see cref="IDocumentProcessingService"/>.
/// Idempotent: already-Processed/Processing documents are skipped, so duplicate messages and the
/// polling fallback never produce duplicate chunks.
/// </summary>
public class DocumentUploadedEventConsumer : RabbitMqConsumerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<DocumentUploadedEventConsumer> _logger;

    public DocumentUploadedEventConsumer(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<DocumentUploadedEventConsumer> logger)
        : base(connection, scopeFactory, settings.Value, logger)
    {
        _logger = logger;
    }

    protected override string QueueName => Settings.DocumentProcessingQueue;

    protected override async Task HandleMessageAsync(
        string messageJson,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<DocumentUploadedEvent>(messageJson, SerializerOptions)
            ?? throw new InvalidOperationException("Payload DocumentUploadedEvent inválido.");

        _logger.LogInformation(
            "Evento DocumentUploaded recibido: DocumentId={DocumentId}, CompanyId={CompanyId}, CorrelationId={CorrelationId}",
            @event.DocumentId,
            @event.CompanyId,
            @event.CorrelationId);

        var processingService = services.GetRequiredService<IDocumentProcessingService>();
        var outcome = await processingService.ProcessDocumentAsync(@event.DocumentId, cancellationToken);

        _logger.LogInformation(
            "Procesamiento por evento del documento {DocumentId} finalizado con resultado {Outcome} (CorrelationId={CorrelationId})",
            @event.DocumentId,
            outcome,
            @event.CorrelationId);

        // Surface Failed so the retry executor can retry transient issues before giving up.
        // SkippedAlreadyProcessed / SkippedInProgress / NotFound are successful dispositions.
        if (outcome == DocumentProcessingOutcome.Failed)
        {
            throw new InvalidOperationException(
                $"El procesamiento del documento {@event.DocumentId} falló; se reintentará.");
        }
    }
}
