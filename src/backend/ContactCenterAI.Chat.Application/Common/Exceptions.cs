namespace ContactCenterAI.Chat.Application.Common;

public sealed class CoreApiException : Exception
{
    public CoreApiException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

public sealed class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class ChatAiException : Exception
{
    public ChatAiException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
