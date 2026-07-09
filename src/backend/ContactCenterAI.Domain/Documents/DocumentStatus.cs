namespace ContactCenterAI.Domain.Documents;

public enum DocumentStatus
{
    Uploaded = 1,
    PendingProcessing = 2,
    Processing = 3,
    Processed = 4,
    Failed = 5
}
