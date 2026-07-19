namespace ContactCenterAI.Chat.Application.Common.Interfaces;

public interface IChatCompletionService
{
    bool IsConfigured { get; }

    Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyList<string> contextChunks,
        CancellationToken cancellationToken = default);
}
