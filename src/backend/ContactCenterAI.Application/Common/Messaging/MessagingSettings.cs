namespace ContactCenterAI.Application.Common.Messaging;

/// <summary>
/// Transport-agnostic messaging feature flag. Bound from the "Messaging" configuration section
/// (env var <c>Messaging__Enabled</c>). Kept in the Application layer so use-case handlers can
/// decide behavior (e.g. mark documents as enqueued) without referencing the Infrastructure layer.
/// </summary>
public class MessagingSettings
{
    public const string SectionName = "Messaging";

    /// <summary>
    /// When true, an event-oriented publisher (RabbitMQ) is used and the Worker runs consumers.
    /// When false, a no-op publisher is used and the system relies purely on DB polling.
    /// </summary>
    public bool Enabled { get; set; }
}
