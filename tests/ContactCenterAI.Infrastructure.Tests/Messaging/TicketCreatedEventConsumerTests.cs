using System.Text.Json;
using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Infrastructure.Messaging;
using ContactCenterAI.Infrastructure.Messaging.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

public class TicketCreatedEventConsumerTests
{
    private sealed class FakeEscalationService : ITicketEscalationService
    {
        public TicketEscalationOutcome Outcome { get; set; } = TicketEscalationOutcome.Prepared;

        public List<Guid> ProcessedIds { get; } = [];

        public Task<TicketEscalationOutcome> ProcessEscalationAsync(
            Guid ticketId,
            string priority,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            ProcessedIds.Add(ticketId);
            return Task.FromResult(Outcome);
        }
    }

    private sealed class TestableTicketConsumer : TicketCreatedEventConsumer
    {
        public TestableTicketConsumer(
            IServiceScopeFactory scopeFactory,
            RabbitMqSettings settings)
            : base(
                connection: null!,
                scopeFactory,
                Options.Create(settings),
                NullLogger<TicketCreatedEventConsumer>.Instance)
        {
        }

        public Task InvokeAsync(string json, IServiceProvider services, CancellationToken ct) =>
            HandleMessageAsync(json, services, ct);
    }

    private static (TestableTicketConsumer consumer, FakeEscalationService escalation, IServiceProvider services)
        CreateSut()
    {
        var escalation = new FakeEscalationService();
        var services = new ServiceCollection();
        services.AddSingleton<ITicketEscalationService>(escalation);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var consumer = new TestableTicketConsumer(scopeFactory, new RabbitMqSettings());
        return (consumer, escalation, provider);
    }

    [Fact]
    public async Task Consumes_event_and_runs_escalation()
    {
        var (consumer, escalation, services) = CreateSut();
        var ticketId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(
            new TicketCreatedEvent(
                ticketId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "High",
                DateTime.UtcNow,
                Guid.NewGuid()),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await consumer.InvokeAsync(json, services, CancellationToken.None);

        Assert.Equal(ticketId, Assert.Single(escalation.ProcessedIds));
    }

    [Fact]
    public async Task Not_found_is_controlled_and_does_not_throw()
    {
        var (consumer, escalation, services) = CreateSut();
        escalation.Outcome = TicketEscalationOutcome.NotFound;

        var json = JsonSerializer.Serialize(
            new TicketCreatedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Low",
                DateTime.UtcNow,
                Guid.NewGuid()),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await consumer.InvokeAsync(json, services, CancellationToken.None);
    }
}
