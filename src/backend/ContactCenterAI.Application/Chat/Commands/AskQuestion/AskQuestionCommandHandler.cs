using System.Text.Json;
using ContactCenterAI.Application.Chat.DTOs;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Documents.DTOs;
using ContactCenterAI.Domain.Chat;
using ContactCenterAI.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Chat.Commands.AskQuestion;

public class AskQuestionCommandHandler : IRequestHandler<AskQuestionCommand, AskQuestionResponse>
{
    private const int TitleMaxLength = 200;
    private const string NoContextAnswer =
        "No encontré información suficiente en los documentos cargados para responder tu pregunta.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IEmbeddingService _embeddingService;

    public AskQuestionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISemanticSearchService semanticSearchService,
        IChatCompletionService chatCompletionService,
        IEmbeddingService embeddingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _semanticSearchService = semanticSearchService;
        _chatCompletionService = chatCompletionService;
        _embeddingService = embeddingService;
    }

    public async Task<AskQuestionResponse> Handle(
        AskQuestionCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("El usuario debe estar autenticado.");
        }

        if (!_chatCompletionService.IsConfigured || !_embeddingService.IsConfigured)
        {
            throw new InvalidOperationException(
                "Proveedor de IA no configurado para generar respuestas de chat.");
        }

        var companyId = await ResolveCompanyIdAsync(request.CompanyId, cancellationToken);
        var userId = _currentUserService.UserId.Value;
        var now = DateTime.UtcNow;

        var conversation = await ResolveConversationAsync(
            request.ConversationId,
            companyId,
            userId,
            request.Question,
            now,
            cancellationToken);

        var searchResults = await _semanticSearchService.SearchSimilarChunksAsync(
            companyId,
            request.Question,
            request.TopK,
            cancellationToken);

        var sources = searchResults
            .Select(result => new ChatSourceDto
            {
                DocumentId = result.DocumentId,
                OriginalFileName = result.OriginalFileName,
                ChunkIndex = result.ChunkIndex,
                ContentPreview = result.ContentPreview,
                Score = result.Score
            })
            .ToList();

        var contextChunks = await LoadFullChunkContentsAsync(searchResults, cancellationToken);

        string answer;

        if (contextChunks.Count == 0)
        {
            answer = NoContextAnswer;
        }
        else
        {
            answer = await _chatCompletionService.GenerateAnswerAsync(
                request.Question,
                contextChunks,
                cancellationToken);
        }

        var userMessage = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Question,
            CreatedAt = now
        };

        var assistantMessage = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = answer,
            SourcesJson = sources.Count > 0
                ? JsonSerializer.Serialize(sources, JsonOptions)
                : null,
            CreatedAt = now
        };

        _context.ConversationMessages.Add(userMessage);
        _context.ConversationMessages.Add(assistantMessage);

        conversation.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return new AskQuestionResponse
        {
            Answer = answer,
            ConversationId = conversation.Id,
            Sources = sources,
            CreatedAt = now
        };
    }

    private async Task<Conversation> ResolveConversationAsync(
        Guid? conversationId,
        Guid companyId,
        Guid userId,
        string question,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (conversationId.HasValue)
        {
            var existingConversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("La conversación especificada no existe.");

            EnsureConversationAccess(existingConversation, companyId, userId);
            return existingConversation;
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = userId,
            Title = BuildTitle(question),
            CreatedAt = now
        };

        _context.Conversations.Add(conversation);
        return conversation;
    }

    private void EnsureConversationAccess(Conversation conversation, Guid companyId, Guid userId)
    {
        if (_currentUserService.Role == Role.SuperAdmin)
        {
            return;
        }

        if (conversation.CompanyId != companyId || conversation.UserId != userId)
        {
            throw new UnauthorizedAccessException("No tiene permisos para acceder a esta conversación.");
        }
    }

    private async Task<List<string>> LoadFullChunkContentsAsync(
        IReadOnlyList<SemanticSearchResultDto> searchResults,
        CancellationToken cancellationToken)
    {
        if (searchResults.Count == 0)
        {
            return [];
        }

        var documentIds = searchResults.Select(result => result.DocumentId).Distinct().ToList();
        var chunkIndexes = searchResults.Select(result => result.ChunkIndex).Distinct().ToList();

        var chunks = await _context.DocumentChunks
            .AsNoTracking()
            .Where(chunk => documentIds.Contains(chunk.DocumentId) && chunkIndexes.Contains(chunk.ChunkIndex))
            .Select(chunk => new
            {
                chunk.DocumentId,
                chunk.ChunkIndex,
                chunk.Content
            })
            .ToListAsync(cancellationToken);

        var contents = new List<string>(searchResults.Count);

        foreach (var result in searchResults)
        {
            var chunk = chunks.FirstOrDefault(item =>
                item.DocumentId == result.DocumentId && item.ChunkIndex == result.ChunkIndex);

            contents.Add(chunk?.Content ?? result.ContentPreview);
        }

        return contents;
    }

    private static string BuildTitle(string question)
    {
        var normalized = question.Trim();

        if (normalized.Length <= TitleMaxLength)
        {
            return normalized;
        }

        return normalized[..TitleMaxLength];
    }

    private async Task<Guid> ResolveCompanyIdAsync(Guid? requestedCompanyId, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role == Role.SuperAdmin)
        {
            if (requestedCompanyId.HasValue)
            {
                return requestedCompanyId.Value;
            }

            var defaultCompanyId = await _context.Companies
                .AsNoTracking()
                .OrderBy(company => company.CreatedAt)
                .Select(company => company.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultCompanyId == Guid.Empty)
            {
                throw new ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(
                        nameof(AskQuestionCommand.CompanyId),
                        "Debe especificar una empresa o existir al menos una empresa en el sistema.")
                ]);
            }

            return defaultCompanyId;
        }

        if (_currentUserService.CompanyId is null)
        {
            throw new UnauthorizedAccessException("El usuario debe pertenecer a una empresa.");
        }

        if (requestedCompanyId.HasValue && requestedCompanyId != _currentUserService.CompanyId)
        {
            throw new UnauthorizedAccessException("No tiene permisos para operar en otra empresa.");
        }

        return _currentUserService.CompanyId.Value;
    }
}
