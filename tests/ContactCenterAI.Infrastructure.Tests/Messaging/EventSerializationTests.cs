using System.Text.Json;
using ContactCenterAI.Application.Common.Messaging;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

public class EventSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DocumentUploadedEvent_roundtrips_json()
    {
        var original = new DocumentUploadedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid());

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DocumentUploadedEvent>(json, Options);

        Assert.NotNull(restored);
        Assert.Equal(original.DocumentId, restored!.DocumentId);
        Assert.Equal(original.CompanyId, restored.CompanyId);
        Assert.Equal(original.UploadedByUserId, restored.UploadedByUserId);
        Assert.Equal(original.CorrelationId, restored.CorrelationId);
        Assert.Equal(original.OccurredAt, restored.OccurredAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void TicketCreatedEvent_roundtrips_json()
    {
        var original = new TicketCreatedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "High",
            DateTime.UtcNow,
            Guid.NewGuid());

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<TicketCreatedEvent>(json, Options);

        Assert.NotNull(restored);
        Assert.Equal(original.TicketId, restored!.TicketId);
        Assert.Equal(original.CompanyId, restored.CompanyId);
        Assert.Equal(original.CreatedByUserId, restored.CreatedByUserId);
        Assert.Equal(original.Priority, restored.Priority);
        Assert.Equal(original.CorrelationId, restored.CorrelationId);
        Assert.Equal(original.OccurredAt, restored.OccurredAt, TimeSpan.FromMilliseconds(1));
    }
}
