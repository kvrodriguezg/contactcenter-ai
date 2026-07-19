using ContactCenterAI.Infrastructure.Chat;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Api.Tests;

public class EmbeddedChatGateTests
{
    [Fact]
    public void External_mode_disables_embedded_chat()
    {
        var settings = new ChatServiceSettings { Mode = ChatServiceModes.External };
        Assert.True(settings.IsExternal);
        Assert.False(settings.IsEmbedded);
    }

    [Fact]
    public void Embedded_mode_keeps_local_chat()
    {
        var settings = Options.Create(new ChatServiceSettings { Mode = ChatServiceModes.Embedded }).Value;
        Assert.True(settings.IsEmbedded);
    }
}
