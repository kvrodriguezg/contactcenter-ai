namespace ContactCenterAI.Application.Common.Interfaces;

public interface IDocumentChunkingService
{
    IReadOnlyList<string> CreateChunks(string text);
}
