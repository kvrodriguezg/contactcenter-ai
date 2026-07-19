namespace ContactCenterAI.Chat.Application.Chat.DTOs;

public class ChatSourceDto
{
    public Guid DocumentId { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public Guid ChunkId { get; set; }

    public int ChunkIndex { get; set; }

    public string ContentPreview { get; set; } = string.Empty;

    public double Similarity { get; set; }

    public int? PageNumber { get; set; }
}

public class AskQuestionResponse
{
    public string Answer { get; set; } = string.Empty;

    public Guid ConversationId { get; set; }

    public IReadOnlyList<ChatSourceDto> Sources { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}

public class ConversationDto
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid ExternalUserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class ConversationMessageDto
{
    public Guid Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public IReadOnlyList<ChatSourceDto> Sources { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}

public class ConversationDetailDto
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid ExternalUserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<ConversationMessageDto> Messages { get; set; } = [];
}
