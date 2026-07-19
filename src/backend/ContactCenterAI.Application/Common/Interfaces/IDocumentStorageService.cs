namespace ContactCenterAI.Application.Common.Interfaces;

public interface IDocumentStorageService
{
    Task<string> SaveAsync(
        Guid companyId,
        Guid documentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    string GetFullPath(string storagePath);
}
