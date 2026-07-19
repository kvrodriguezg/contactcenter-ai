namespace ContactCenterAI.Application.Common.Messaging;

/// <summary>
/// Transport-agnostic publisher for integration events. The concrete implementation
/// (RabbitMQ vs. no-op) is selected at composition time based on <see cref="MessagingSettings.Enabled"/>.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
}
