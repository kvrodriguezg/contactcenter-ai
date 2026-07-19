using Microsoft.Extensions.Configuration;

namespace ContactCenterAI.Infrastructure.Chat;

public class ChatServiceSettings
{
    public const string SectionName = "ChatService";

    public string Mode { get; set; } = ChatServiceModes.Embedded;

    public bool IsEmbedded =>
        string.Equals(Mode, ChatServiceModes.Embedded, StringComparison.OrdinalIgnoreCase);

    public bool IsExternal =>
        string.Equals(Mode, ChatServiceModes.External, StringComparison.OrdinalIgnoreCase);
}

public static class ChatServiceModes
{
    public const string Embedded = "Embedded";
    public const string External = "External";
}

public static class ChatServiceConfiguration
{
    public static ChatServiceSettings Resolve(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var settings = configuration.GetSection(ChatServiceSettings.SectionName)
            .Get<ChatServiceSettings>()
            ?? new ChatServiceSettings();

        var modeFromEnv = configuration["CHAT_SERVICE_MODE"];
        if (!string.IsNullOrWhiteSpace(modeFromEnv))
        {
            settings.Mode = modeFromEnv.Trim();
        }

        if (string.IsNullOrWhiteSpace(settings.Mode))
        {
            settings.Mode = ChatServiceModes.Embedded;
        }

        return settings;
    }
}
