using System.Text.Json;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Infrastructure.Messaging;
using ContactCenterAI.Infrastructure.Messaging.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

/// <summary>
/// Exercises document-consumer handling logic without a live broker by driving
/// <see cref="DocumentUploadedEventConsumer"/> through a test subclass.
/// </summary>
public class DocumentUploadedEventConsumerTests
{
    private sealed class FakeProcessingService : IDocumentProcessingService
    {
        public DocumentProcessingOutcome Outcome { get; set; } = DocumentProcessingOutcome.Processed;

        public List<Guid> ProcessedIds { get; } = [];

        public Task<int> ProcessPendingDocumentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<DocumentProcessingOutcome> ProcessDocumentAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            ProcessedIds.Add(documentId);
            return Task.FromResult(Outcome);
        }
    }

    private sealed class TestableDocumentConsumer : DocumentUploadedEventConsumer
    {
        public TestableDocumentConsumer(
            IServiceScopeFactory scopeFactory,
            RabbitMqSettings settings)
            : base(
                connection: null!,
                scopeFactory,
                Options.Create(settings),
                NullLogger<DocumentUploadedEventConsumer>.Instance)
        {
        }

        public Task InvokeAsync(string json, IServiceProvider services, CancellationToken ct) =>
            HandleMessageAsync(json, services, ct);
    }

    private static (TestableDocumentConsumer consumer, FakeProcessingService processing, IServiceProvider services)
        CreateSut()
    {
        var processing = new FakeProcessingService();
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentProcessingService>(processing);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var consumer = new TestableDocumentConsumer(scopeFactory, new RabbitMqSettings());
        return (consumer, processing, provider);
    }

    [Fact]
    public async Task Consumes_event_and_invokes_processing()
    {
        var (consumer, processing, services) = CreateSut();
        var documentId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(
            new DocumentUploadedEvent(
                documentId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid()),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await consumer.InvokeAsync(json, services, CancellationToken.None);

        Assert.Equal(documentId, Assert.Single(processing.ProcessedIds));
    }

    [Fact]
    public async Task Already_processed_outcome_does_not_throw()
    {
        var (consumer, processing, services) = CreateSut();
        processing.Outcome = DocumentProcessingOutcome.SkippedAlreadyProcessed;

        var json = JsonSerializer.Serialize(
            new DocumentUploadedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid()),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await consumer.InvokeAsync(json, services, CancellationToken.None);
    }

    [Fact]
    public async Task Failed_outcome_throws_for_limited_retry()
    {
        var (consumer, processing, services) = CreateSut();
        processing.Outcome = DocumentProcessingOutcome.Failed;

        var json = JsonSerializer.Serialize(
            new DocumentUploadedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid()),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            consumer.InvokeAsync(json, services, CancellationToken.None));
    }
}
