using ContactCenterAI.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Documents;

public class DocumentChunkingService : IDocumentChunkingService
{
    private readonly DocumentProcessingSettings _settings;

    public DocumentChunkingService(IOptions<DocumentProcessingSettings> settings)
    {
        _settings = settings.Value;
    }

    public IReadOnlyList<string> CreateChunks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalizedText = text.Trim();
        var chunkSize = Math.Max(1, _settings.ChunkSize);
        var overlap = Math.Max(0, _settings.ChunkOverlap);
        var step = Math.Max(1, chunkSize - overlap);
        var chunks = new List<string>();

        for (var start = 0; start < normalizedText.Length; start += step)
        {
            var length = Math.Min(chunkSize, normalizedText.Length - start);
            var chunk = normalizedText.Substring(start, length).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (start + length >= normalizedText.Length)
            {
                break;
            }
        }

        return chunks;
    }
}
