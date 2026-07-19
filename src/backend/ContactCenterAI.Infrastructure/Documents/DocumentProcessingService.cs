using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Infrastructure.Ai;
using ContactCenterAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Documents;

public class DocumentProcessingService : IDocumentProcessingService
{
    private const int MaxErrorMessageLength = 2000;

    private readonly IApplicationDbContext _context;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IDocumentChunkingService _documentChunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly DocumentProcessingSettings _settings;
    private readonly GeminiSettings _geminiSettings;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        IApplicationDbContext context,
        IDocumentStorageService documentStorageService,
        IPdfTextExtractor pdfTextExtractor,
        IDocumentChunkingService documentChunkingService,
        IEmbeddingService embeddingService,
        IOptions<DocumentProcessingSettings> settings,
        IOptions<GeminiSettings> geminiSettings,
        ILogger<DocumentProcessingService> logger)
    {
        _context = context;
        _documentStorageService = documentStorageService;
        _pdfTextExtractor = pdfTextExtractor;
        _documentChunkingService = documentChunkingService;
        _embeddingService = embeddingService;
        _settings = settings.Value;
        _geminiSettings = geminiSettings.Value;
        _logger = logger;
    }

    public async Task<int> ProcessPendingDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var pendingDocuments = await _context.Documents
            .Where(d => d.Status == DocumentStatus.Uploaded || d.Status == DocumentStatus.PendingProcessing)
            .OrderBy(d => d.CreatedAt)
            .Take(_settings.BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingDocuments.Count == 0)
        {
            return 0;
        }

        var processedCount = 0;

        foreach (var document in pendingDocuments)
        {
            try
            {
                var outcome = await ProcessDocumentAsync(document, cancellationToken);
                if (outcome == DocumentProcessingOutcome.Processed)
                {
                    processedCount++;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(
                    exception,
                    "Error inesperado al procesar el documento {DocumentId}",
                    document.Id);
            }
        }

        return processedCount;
    }

    public async Task<DocumentProcessingOutcome> ProcessDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            _logger.LogWarning(
                "Documento {DocumentId} no encontrado; se omite el procesamiento por evento",
                documentId);
            return DocumentProcessingOutcome.NotFound;
        }

        // Idempotencia: no reprocesar documentos ya finalizados ni en curso.
        if (!DocumentProcessingRules.ShouldProcess(document.Status))
        {
            _logger.LogInformation(
                "Documento {DocumentId} en estado {Status}; se omite (idempotencia)",
                document.Id,
                document.Status);
            return DocumentProcessingRules.SkipOutcomeFor(document.Status);
        }

        return await ProcessDocumentAsync(document, cancellationToken);
    }

    private async Task<DocumentProcessingOutcome> ProcessDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Procesando documento {DocumentId} ({FileName})",
            document.Id,
            document.OriginalFileName);

        document.Status = DocumentStatus.Processing;
        document.ErrorMessage = null;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var filePath = _documentStorageService.GetFullPath(document.StoragePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"No se encontró el archivo en almacenamiento: {document.StoragePath}");
            }

            var extractedText = await _pdfTextExtractor.ExtractTextAsync(filePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new InvalidOperationException("No se pudo extraer texto del PDF.");
            }

            var chunkTexts = _documentChunkingService.CreateChunks(extractedText);

            if (chunkTexts.Count == 0)
            {
                throw new InvalidOperationException("No se generaron fragmentos de texto a partir del PDF.");
            }

            if (!_embeddingService.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Proveedor de IA no configurado para generar embeddings.");
            }

            var existingChunks = await _context.DocumentChunks
                .Where(c => c.DocumentId == document.Id)
                .ToListAsync(cancellationToken);

            if (existingChunks.Count > 0)
            {
                _context.DocumentChunks.RemoveRange(existingChunks);
            }

            var createdAt = DateTime.UtcNow;
            var embeddedAt = DateTime.UtcNow;
            var chunks = new List<DocumentChunk>(chunkTexts.Count);

            for (var index = 0; index < chunkTexts.Count; index++)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(
                    chunkTexts[index],
                    cancellationToken: cancellationToken);

                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    ChunkIndex = index,
                    Content = chunkTexts[index],
                    Embedding = embedding,
                    EmbeddingModel = _geminiSettings.EmbeddingsModel,
                    EmbeddedAt = embeddedAt,
                    CreatedAt = createdAt
                });
            }

            _context.DocumentChunks.AddRange(chunks);

            document.Status = DocumentStatus.Processed;
            document.ProcessedAt = DateTime.UtcNow;
            document.ErrorMessage = null;
            document.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Documento {DocumentId} procesado correctamente con {ChunkCount} fragmentos y embeddings",
                document.Id,
                chunkTexts.Count);

            return DocumentProcessingOutcome.Processed;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await MarkAsFailedAsync(document, exception, cancellationToken);
            return DocumentProcessingOutcome.Failed;
        }
    }

    private async Task MarkAsFailedAsync(
        Document document,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception.Message;

        if (errorMessage.Length > MaxErrorMessageLength)
        {
            errorMessage = errorMessage[..MaxErrorMessageLength];
        }

        document.Status = DocumentStatus.Failed;
        document.ProcessedAt = DateTime.UtcNow;
        document.ErrorMessage = errorMessage;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            exception,
            "El documento {DocumentId} falló durante el procesamiento",
            document.Id);
    }
}
