using ContactCenterAI.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContactCenterAI.Infrastructure.Tests.Messaging;

public class MessageRetryExecutorTests
{
    [Fact]
    public async Task Succeeds_on_first_attempt()
    {
        var attempts = 0;

        var disposition = await MessageRetryExecutor.ExecuteAsync(
            _ =>
            {
                attempts++;
                return Task.CompletedTask;
            },
            maxRetryAttempts: 3,
            baseDelay: TimeSpan.Zero,
            logger: NullLogger.Instance,
            context: "test",
            cancellationToken: CancellationToken.None);

        Assert.Equal(MessageDisposition.Acknowledged, disposition);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task Retries_up_to_limit_then_rejects()
    {
        var attempts = 0;

        var disposition = await MessageRetryExecutor.ExecuteAsync(
            _ =>
            {
                attempts++;
                throw new InvalidOperationException("transient");
            },
            maxRetryAttempts: 2,
            baseDelay: TimeSpan.Zero,
            logger: NullLogger.Instance,
            context: "test",
            cancellationToken: CancellationToken.None);

        Assert.Equal(MessageDisposition.Rejected, disposition);
        // attempt 1 fail + 2 retries = 3 total when maxRetryAttempts=2 (attempt > max)
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Recovers_after_transient_failures()
    {
        var attempts = 0;

        var disposition = await MessageRetryExecutor.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException("transient");
                }

                return Task.CompletedTask;
            },
            maxRetryAttempts: 3,
            baseDelay: TimeSpan.Zero,
            logger: NullLogger.Instance,
            context: "test",
            cancellationToken: CancellationToken.None);

        Assert.Equal(MessageDisposition.Acknowledged, disposition);
        Assert.Equal(3, attempts);
    }
}
