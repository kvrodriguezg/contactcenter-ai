namespace ContactCenterAI.Api.Models;

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;

    public Dictionary<string, string[]>? Errors { get; set; }
}
