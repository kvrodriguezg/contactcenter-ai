using ContactCenterAI.Application.Common.Messaging;

namespace ContactCenterAI.Application.Tests.Common;

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<object> Published { get; } = [];

    public Exception? ThrowOnPublish { get; set; }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : class
    {
        if (ThrowOnPublish is not null)
        {
            throw ThrowOnPublish;
        }

        Published.Add(@event!);
        return Task.CompletedTask;
    }
}
