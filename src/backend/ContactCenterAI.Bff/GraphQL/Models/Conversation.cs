namespace ContactCenterAI.Bff.GraphQL.Models;

/// <summary>Maps the Chat API <c>ConversationDto</c> (external chat microservice).</summary>
public class Conversation
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid ExternalUserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Populated only for the conversationById resolver (detail view). Null on list.
    public IReadOnlyList<ConversationMessage>? Messages { get; set; }
}

/// <summary>Maps Chat API <c>ConversationMessageDto</c>.</summary>
public class ConversationMessage
{
    public Guid Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public IReadOnlyList<ChatSource> Sources { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}

/// <summary>Maps Chat API <c>ChatSourceDto</c> (RAG citation, no embeddings exposed).</summary>
public class ChatSource
{
    public Guid DocumentId { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public Guid ChunkId { get; set; }

    public int ChunkIndex { get; set; }

    public string ContentPreview { get; set; } = string.Empty;

    public double Similarity { get; set; }

    public int? PageNumber { get; set; }
}
