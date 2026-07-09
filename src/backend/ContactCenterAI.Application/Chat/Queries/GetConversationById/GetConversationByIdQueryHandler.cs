using System.Text.Json;
using ContactCenterAI.Application.Chat.DTOs;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContactCenterAI.Application.Chat.Queries.GetConversationById;

public class GetConversationByIdQueryHandler
    : IRequestHandler<GetConversationByIdQuery, ConversationDetailDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetConversationByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ConversationDetailDto> Handle(
        GetConversationByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("El usuario debe estar autenticado.");
        }

        var conversation = await _context.Conversations
            .AsNoTracking()
            .Include(c => c.Company)
            .Include(c => c.Messages.OrderBy(message => message.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("La conversación especificada no existe.");

        if (_currentUserService.Role != Role.SuperAdmin)
        {
            if (_currentUserService.CompanyId is null ||
                _currentUserService.UserId is null ||
                conversation.CompanyId != _currentUserService.CompanyId ||
                conversation.UserId != _currentUserService.UserId)
            {
                throw new UnauthorizedAccessException("No tiene permisos para acceder a esta conversación.");
            }
        }

        return new ConversationDetailDto
        {
            Id = conversation.Id,
            CompanyId = conversation.CompanyId,
            CompanyName = conversation.Company.Name,
            UserId = conversation.UserId,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = conversation.Messages
                .Select(message => new ConversationMessageDto
                {
                    Id = message.Id,
                    Role = message.Role.ToString(),
                    Content = message.Content,
                    Sources = DeserializeSources(message.SourcesJson),
                    CreatedAt = message.CreatedAt
                })
                .ToList()
        };
    }

    private static IReadOnlyList<ChatSourceDto> DeserializeSources(string? sourcesJson)
    {
        if (string.IsNullOrWhiteSpace(sourcesJson))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<ChatSourceDto>>(sourcesJson, JsonOptions) ?? [];
    }
}
