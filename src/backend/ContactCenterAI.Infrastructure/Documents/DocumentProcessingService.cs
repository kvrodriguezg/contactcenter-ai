using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Documents;
using ContactCenterAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Documents;

public class DocumentProcessingService : IDocumentProcessingService
{
    private const int MaxErrorMessageLength = 2000;

    private readonly ApplicationDbContext _context;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IDocumentChunkingService _documentChunkingService;
    private readonly DocumentProcessingSettings _settings;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        ApplicationDbContext context,
        IDocumentStorageService documentStorageService,
        IPdfTextExtractor pdfTextExtractor,
        IDocumentChunkingService documentChunkingService,
        IOptions<DocumentProcessingSettings> settings,
        ILogger<DocumentProcessingService> logger)
    {
        _context = context;
        _documentStorageService = documentStorageService;
        _pdfTextExtractor = pdfTextExtractor;
        _documentChunkingService = documentChunkingService;
        _settings = settings.Value;
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
                await ProcessDocumentAsync(document, cancellationToken);
                processedCount++;
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

    private async Task ProcessDocumentAsync(Document document, CancellationToken cancellationToken)
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

            var existingChunks = await _context.DocumentChunks
                .Where(c => c.DocumentId == document.Id)
                .ToListAsync(cancellationToken);

            if (existingChunks.Count > 0)
            {
                _context.DocumentChunks.RemoveRange(existingChunks);
            }

            var createdAt = DateTime.UtcNow;

            for (var index = 0; index < chunkTexts.Count; index++)
            {
                _context.DocumentChunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    ChunkIndex = index,
                    Content = chunkTexts[index],
                    CreatedAt = createdAt
                });
            }

            document.Status = DocumentStatus.Processed;
            document.ProcessedAt = DateTime.UtcNow;
            document.ErrorMessage = null;
            document.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Documento {DocumentId} procesado correctamente con {ChunkCount} fragmentos",
                document.Id,
                chunkTexts.Count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await MarkAsFailedAsync(document, exception, cancellationToken);
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
