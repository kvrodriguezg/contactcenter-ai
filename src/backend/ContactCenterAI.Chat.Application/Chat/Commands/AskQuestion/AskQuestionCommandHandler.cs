using System.Text.Json;
using ContactCenterAI.Chat.Application.Chat.DTOs;
using ContactCenterAI.Chat.Application.Common;
using ContactCenterAI.Chat.Application.Common.Interfaces;
using ContactCenterAI.Chat.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Chat.Application.Chat.Commands.AskQuestion;

public class AskQuestionCommandHandler : IRequestHandler<AskQuestionCommand, AskQuestionResponse>
{
    private const int TitleMaxLength = 200;
    private const string NoContextAnswer =
        "No encontré información suficiente en los documentos cargados para responder tu pregunta.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IChatDbContext _dbContext;
    private readonly IUserProfileClient _userProfileClient;
    private readonly IDocumentSearchClient _documentSearchClient;
    private readonly IChatCompletionService _chatCompletionService;

    public AskQuestionCommandHandler(
        IChatDbContext dbContext,
        IUserProfileClient userProfileClient,
        IDocumentSearchClient documentSearchClient,
        IChatCompletionService chatCompletionService)
    {
        _dbContext = dbContext;
        _userProfileClient = userProfileClient;
        _documentSearchClient = documentSearchClient;
        _chatCompletionService = chatCompletionService;
    }

    public async Task<AskQuestionResponse> Handle(
        AskQuestionCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BearerToken))
        {
            throw new UnauthorizedAccessException("Token de autenticación ausente.");
        }

        var profile = await _userProfileClient.GetCurrentUserAsync(
            request.BearerToken,
            cancellationToken);

        UserProfileValidator.EnsureValidForChat(profile);

        if (!_chatCompletionService.IsConfigured)
        {
            throw new ChatAiException(
                "Proveedor de IA no configurado para generar respuestas de chat.");
        }

        var companyId = profile.CompanyId!.Value;
        var now = DateTime.UtcNow;

        var conversation = await ResolveConversationAsync(
            request.ConversationId,
            profile,
            companyId,
            request.Question,
            now,
            cancellationToken);

        IReadOnlyList<DocumentSearchHitDto> searchHits;

        try
        {
            searchHits = await _documentSearchClient.SearchAsync(
                request.BearerToken,
                request.Question,
                request.TopK,
                cancellationToken);
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (CoreApiException)
        {
            throw;
        }

        var sources = searchHits
            .Select(hit => new ChatSourceDto
            {
                DocumentId = hit.DocumentId,
                DocumentName = hit.DocumentName,
                ChunkId = hit.ChunkId,
                ChunkIndex = hit.ChunkIndex,
                ContentPreview = hit.Content.Length <= 200
                    ? hit.Content
                    : hit.Content[..200],
                Similarity = hit.Similarity,
                PageNumber = hit.PageNumber
            })
            .ToList();

        var contextChunks = searchHits
            .Select(hit => hit.Content)
            .Where(content => !string.IsNullOrWhiteSpace(content))
            .ToList();

        string answer;

        try
        {
            answer = contextChunks.Count == 0
                ? NoContextAnswer
                : await _chatCompletionService.GenerateAnswerAsync(
                    request.Question,
                    contextChunks,
                    cancellationToken);
        }
        catch (ChatAiException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ChatAiException(
                "No fue posible generar la respuesta con el proveedor de IA.",
                exception);
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

        _dbContext.ConversationMessages.Add(userMessage);
        _dbContext.ConversationMessages.Add(assistantMessage);
        conversation.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

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
        UserProfileDto profile,
        Guid companyId,
        string question,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (conversationId.HasValue)
        {
            var existing = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("La conversación especificada no existe.");

            if (existing.CompanyId != companyId || existing.ExternalUserId != profile.UserId)
            {
                throw new UnauthorizedAccessException(
                    "No tiene permisos para acceder a esta conversación.");
            }

            return existing;
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            ExternalUserId = profile.UserId,
            UserEmail = profile.Email,
            CompanyId = companyId,
            Title = BuildTitle(question),
            CreatedAt = now
        };

        _dbContext.Conversations.Add(conversation);
        return conversation;
    }

    private static string BuildTitle(string question)
    {
        var normalized = question.Trim();
        return normalized.Length <= TitleMaxLength
            ? normalized
            : normalized[..TitleMaxLength];
    }
}

public class AskQuestionCommandValidator : AbstractValidator<AskQuestionCommand>
{
    public AskQuestionCommandValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.TopK).InclusiveBetween(1, 20);
        RuleFor(x => x.BearerToken).NotEmpty();
    }
}
