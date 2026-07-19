using System.Text.Json;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="DocumentUploadedEvent"/> and triggers the EXISTING document processing
/// pipeline (extraction → chunking → embeddings) via <see cref="IDocumentProcessingService"/>.
/// Idempotent: already-Processed/Processing documents are skipped, so duplicate messages and the
/// polling fallback never produce duplicate chunks.
/// </summary>
public sealed class DocumentUploadedConsumer : RabbitMqConsumerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<DocumentUploadedConsumer> _logger;

    public DocumentUploadedConsumer(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<DocumentUploadedConsumer> logger)
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
            "Evento DocumentUploaded recibido: DocumentId={DocumentId}, CompanyId={CompanyId}",
            @event.DocumentId,
            @event.CompanyId);

        var processingService = services.GetRequiredService<IDocumentProcessingService>();
        var outcome = await processingService.ProcessDocumentAsync(@event.DocumentId, cancellationToken);

        _logger.LogInformation(
            "Procesamiento por evento del documento {DocumentId} finalizado con resultado {Outcome}",
            @event.DocumentId,
            outcome);

        // A genuine failure throws inside the pipeline only for infra errors; ProcessDocumentAsync
        // itself marks the document Failed and returns an outcome. We surface Failed as an
        // exception so the retry executor can retry transient issues before giving up.
        if (outcome == DocumentProcessingOutcome.Failed)
        {
            throw new InvalidOperationException(
                $"El procesamiento del documento {@event.DocumentId} falló; se reintentará.");
        }
    }
}
