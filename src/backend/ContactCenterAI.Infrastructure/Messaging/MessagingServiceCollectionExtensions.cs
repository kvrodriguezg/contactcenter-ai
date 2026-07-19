using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Infrastructure.Messaging.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContactCenterAI.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the event publisher. When <c>Messaging:Enabled=true</c> a RabbitMQ publisher and a
    /// shared auto-recovering connection are wired; otherwise a no-op publisher preserves the
    /// pure-polling behavior with zero broker dependency. Called from <c>AddInfrastructure</c> so the
    /// API (publisher) and the Worker (publisher + consumers) share the same selection logic.
    /// </summary>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MessagingSettings>(
            configuration.GetSection(MessagingSettings.SectionName));

        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName));

        if (IsMessagingEnabled(configuration))
        {
            services.AddSingleton<RabbitMqConnection>();
            services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        }
        else
        {
            services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
        }

        return services;
    }

    /// <summary>
    /// Registers the queue consumers as hosted services. Only invoked by the Worker, and only when
    /// messaging is enabled. When disabled, the Worker relies solely on its polling loop.
    /// </summary>
    public static IServiceCollection AddMessagingConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (!IsMessagingEnabled(configuration))
        {
            return services;
        }

        services.AddHostedService<DocumentUploadedEventConsumer>();
        services.AddHostedService<TicketCreatedEventConsumer>();

        return services;
    }

    private static bool IsMessagingEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>($"{MessagingSettings.SectionName}:{nameof(MessagingSettings.Enabled)}");
}
