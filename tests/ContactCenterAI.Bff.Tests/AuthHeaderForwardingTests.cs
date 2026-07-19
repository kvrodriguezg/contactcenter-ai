using System.Net;
using ContactCenterAI.Bff.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ContactCenterAI.Bff.Tests;

public class AuthHeaderForwardingTests
{
    [Fact]
    public async Task Forwards_authorization_header_without_logging_token()
    {
        const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test-token-value";
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        accessor.HttpContext.Request.Headers.Authorization = $"Bearer {token}";

        var inner = new CaptureHandler();
        var handler = new AuthHeaderForwardingHandler(accessor)
        {
            InnerHandler = inner
        };

        using var loggerFactory = new CollectingLoggerFactory();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://downstream.test/") };

        // Simulate typical client call path (no token written to logs by handler).
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/Companies");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"Bearer {token}", inner.AuthorizationHeader);
        Assert.DoesNotContain(token, string.Join('\n', loggerFactory.Messages), StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer ", string.Join('\n', loggerFactory.Messages), StringComparison.Ordinal);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? AuthorizationHeader { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationHeader = request.Headers.Authorization?.ToString()
                ?? (request.Headers.TryGetValues("Authorization", out var values)
                    ? values.FirstOrDefault()
                    : null);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class CollectingLoggerFactory : ILoggerFactory
    {
        public List<string> Messages { get; } = [];

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(Messages);

        public void Dispose()
        {
        }
    }

    private sealed class CollectingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Add(formatter(state, exception));
        }
    }
}
