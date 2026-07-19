namespace ContactCenterAI.Infrastructure.Storage;

public class DocumentStorageSettings
{
    public const string SectionName = "DocumentStorage";

    public string BasePath { get; set; } = "storage/documents";
}
