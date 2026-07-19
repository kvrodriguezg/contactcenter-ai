using Microsoft.Extensions.Logging;

namespace ContactCenterAI.Infrastructure.Messaging;

/// <summary>Terminal disposition of a message after the retry executor ran.</summary>
public enum MessageDisposition
{
    /// <summary>Handler succeeded; the message should be acked.</summary>
    Acknowledged = 0,

    /// <summary>Handler failed after all retries; the message should be nacked without requeue.</summary>
    Rejected = 1
}

/// <summary>
/// Runs a message handler with a bounded number of in-process retries and linear backoff.
/// Extracted from the consumer so the "limited retries + one bad message never crashes the
/// worker" behavior is deterministically unit-testable without a broker.
/// </summary>
public static class MessageRetryExecutor
{
    public static async Task<MessageDisposition> ExecuteAsync(
        Func<CancellationToken, Task> handler,
        int maxRetryAttempts,
        TimeSpan baseDelay,
        ILogger logger,
        string context,
        CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                await handler(cancellationToken);
                return MessageDisposition.Acknowledged;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (attempt > maxRetryAttempts)
                {
                    logger.LogError(
                        exception,
                        "Mensaje en {Context} falló tras {Attempts} intentos; se descarta (sin requeue)",
                        context,
                        attempt);
                    return MessageDisposition.Rejected;
                }

                logger.LogWarning(
                    exception,
                    "Mensaje en {Context} falló (intento {Attempt}/{MaxAttempts}); reintentando",
                    context,
                    attempt,
                    maxRetryAttempts + 1);

                if (baseDelay > TimeSpan.Zero)
                {
                    await Task.Delay(baseDelay * attempt, cancellationToken);
                }
            }
        }
    }
}
