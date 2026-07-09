using ContactCenterAI.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Storage;

public class LocalDocumentStorageService : IDocumentStorageService
{
    private readonly DocumentStorageSettings _settings;

    public LocalDocumentStorageService(IOptions<DocumentStorageSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> SaveAsync(
        Guid companyId,
        Guid documentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var relativePath = Path.Combine(companyId.ToString(), documentId.ToString(), fileName);
        var fullPath = GetFullPath(relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;

        Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await content.CopyToAsync(fileStream, cancellationToken);

        return relativePath.Replace('\\', '/');
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null && Directory.Exists(directory))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }

        return Task.CompletedTask;
    }

    public string GetFullPath(string storagePath)
    {
        var normalizedPath = storagePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(_settings.BasePath, normalizedPath));
    }
}
