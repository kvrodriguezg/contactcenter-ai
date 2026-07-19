namespace ContactCenterAI.Bff.Clients;

/// <summary>
/// Thrown when a downstream REST service (Core API / Chat API) is unreachable or
/// returns a non-success status. Caught by the GraphQL error filter and converted
/// into a controlled GraphQL error so the BFF never emits an unhandled 500.
/// </summary>
public sealed class DownstreamApiException : Exception
{
    public DownstreamApiException(string service, string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        Service = service;
        StatusCode = statusCode;
    }

    /// <summary>Logical downstream name ("CoreApi" | "ChatApi").</summary>
    public string Service { get; }

    /// <summary>Downstream HTTP status when available; null for transport failures.</summary>
    public int? StatusCode { get; }
}
