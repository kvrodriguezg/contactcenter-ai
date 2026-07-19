using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Documents.DTOs;
using ContactCenterAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;

namespace ContactCenterAI.Infrastructure.Documents;

public class SemanticSearchService : ISemanticSearchService
{
    private const int ContentPreviewLength = 200;

    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;

    public SemanticSearchService(
        ApplicationDbContext context,
        IEmbeddingService embeddingService)
    {
        _context = context;
        _embeddingService = embeddingService;
    }

    public async Task<IReadOnlyList<SemanticSearchResultDto>> SearchSimilarChunksAsync(
        Guid companyId,
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (!_embeddingService.IsConfigured)
        {
            throw new InvalidOperationException(
                "Proveedor de IA no configurado para generar embeddings.");
        }

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            query,
            "RETRIEVAL_QUERY",
            cancellationToken);

        var queryVector = new Vector(queryEmbedding);

        const string sql = """
            SELECT
                c."Id" AS "ChunkId",
                c."DocumentId" AS "DocumentId",
                d."OriginalFileName" AS "OriginalFileName",
                c."ChunkIndex" AS "ChunkIndex",
                c."Content" AS "Content",
                1 - (c."Embedding" <=> @queryEmbedding) AS "Score"
            FROM document_chunks c
            INNER JOIN documents d ON d."Id" = c."DocumentId"
            WHERE d."CompanyId" = @companyId
              AND c."Embedding" IS NOT NULL
            ORDER BY c."Embedding" <=> @queryEmbedding
            LIMIT @topK
            """;

        var results = new List<SemanticSearchResultDto>();

        var connection = _context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("queryEmbedding", queryVector));
        command.Parameters.Add(new NpgsqlParameter("companyId", companyId));
        command.Parameters.Add(new NpgsqlParameter("topK", topK));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var content = reader.GetString(reader.GetOrdinal("Content"));
            var preview = content.Length <= ContentPreviewLength
                ? content
                : content[..ContentPreviewLength];
            var fileName = reader.GetString(reader.GetOrdinal("OriginalFileName"));
            var score = reader.GetDouble(reader.GetOrdinal("Score"));

            results.Add(new SemanticSearchResultDto
            {
                ChunkId = reader.GetGuid(reader.GetOrdinal("ChunkId")),
                DocumentId = reader.GetGuid(reader.GetOrdinal("DocumentId")),
                DocumentName = fileName,
                OriginalFileName = fileName,
                ChunkIndex = reader.GetInt32(reader.GetOrdinal("ChunkIndex")),
                Content = content,
                ContentPreview = preview,
                Similarity = score,
                Score = score,
                PageNumber = null
            });
        }

        return results;
    }
}
