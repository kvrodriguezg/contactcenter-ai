using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContactCenterAI.Bff.Clients;

/// <summary>
/// Shared JSON options for downstream deserialization. Case-insensitive to match
/// ASP.NET camelCase output, plus a string-enum converter so ticket
/// Status/Priority decode whether Core serialises them as names or numbers.
/// </summary>
public static class BffJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: true));
        return options;
    }
}
