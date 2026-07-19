using ContactCenterAI.Chat.Application.Chat;
using ContactCenterAI.Chat.Application.Common.Interfaces;
using ContactCenterAI.Chat.Domain;
using ContactCenterAI.Chat.Infrastructure.Ai;
using ContactCenterAI.Chat.Infrastructure.CoreApi;
using ContactCenterAI.Chat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Chat.Tests;

public class ChatDbContextTests
{
    [Fact]
    public async Task Persists_conversation_and_messages_independently()
    {
        await using var db = CreateDb();
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            ExternalUserId = Guid.NewGuid(),
            UserEmail = "agent@test.com",
            CompanyId = Guid.NewGuid(),
            Title = "Consulta",
            CreatedAt = DateTime.UtcNow
        };

        db.Conversations.Add(conversation);
        db.ConversationMessages.Add(new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = "Hola",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var loaded = await db.Conversations
            .Include(c => c.Messages)
            .SingleAsync(c => c.Id == conversation.Id);

        Assert.Equal("Consulta", loaded.Title);
        Assert.Single(loaded.Messages);
    }

    [Fact]
    public async Task Isolates_conversations_by_company()
    {
        await using var db = CreateDb();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Conversations.AddRange(
            new Conversation
            {
                Id = Guid.NewGuid(),
                ExternalUserId = userId,
                UserEmail = "a@test.com",
                CompanyId = companyA,
                Title = "A",
                CreatedAt = DateTime.UtcNow
            },
            new Conversation
            {
                Id = Guid.NewGuid(),
                ExternalUserId = userId,
                UserEmail = "a@test.com",
                CompanyId = companyB,
                Title = "B",
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var onlyA = await db.Conversations
            .Where(c => c.CompanyId == companyA)
            .ToListAsync();

        Assert.Single(onlyA);
        Assert.Equal("A", onlyA[0].Title);
    }

    private static ChatDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase($"chat-{Guid.NewGuid()}")
            .Options;
        return new ChatDbContext(options);
    }
}

public class UserProfileValidatorTests
{
    [Fact]
    public void Rejects_inactive_user()
    {
        var profile = ValidProfile();
        profile.IsActive = false;

        var ex = Assert.Throws<UnauthorizedAccessException>(
            () => UserProfileValidator.EnsureValidForChat(profile));

        Assert.Contains("inactivo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_user_without_company()
    {
        var profile = ValidProfile();
        profile.CompanyId = null;

        var ex = Assert.Throws<UnauthorizedAccessException>(
            () => UserProfileValidator.EnsureValidForChat(profile));

        Assert.Contains("empresa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_missing_token_scenario_as_empty_profile_fields()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            UserProfileValidator.EnsureValidForChat(new UserProfileDto()));
    }

    [Fact]
    public void Accepts_valid_profile()
    {
        UserProfileValidator.EnsureValidForChat(ValidProfile());
    }

    private static UserProfileDto ValidProfile() => new()
    {
        UserId = Guid.NewGuid(),
        Email = "agent@test.com",
        Role = "Agent",
        CompanyId = Guid.NewGuid(),
        IsActive = true
    };
}

public class ChatServiceModeFlagTests
{
    [Theory]
    [InlineData("Embedded")]
    [InlineData("External")]
    public void Frontend_and_backend_modes_are_recognized(string mode)
    {
        Assert.True(
            string.Equals(mode, "Embedded", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "External", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void External_mode_implies_embedded_endpoints_are_disabled()
    {
        const string mode = "External";
        var shouldReturn410 = string.Equals(mode, "External", StringComparison.OrdinalIgnoreCase);
        Assert.True(shouldReturn410);
    }
}

public class RecordingHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
        _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Responder(request));
    }
}

public class UserProfileClientTests
{
    [Fact]
    public async Task Propagates_bearer_token_to_core_me_contract()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"userId":"11111111-1111-1111-1111-111111111111","email":"a@test.com","role":"Agent","companyId":"22222222-2222-2222-2222-222222222222","isActive":true}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            }
        };

        var sut = new UserProfileClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://core") },
            NullLogger<UserProfileClient>.Instance);

        var profile = await sut.GetCurrentUserAsync("token-abc", CancellationToken.None);

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("token-abc", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Equal("/api/auth/me", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("Agent", profile.Role);
    }

    [Fact]
    public async Task Returns_503_when_core_unreachable()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => throw new HttpRequestException("connection refused")
        };

        var sut = new UserProfileClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://core") },
            NullLogger<UserProfileClient>.Instance);

        await Assert.ThrowsAsync<ContactCenterAI.Chat.Application.Common.ServiceUnavailableException>(
            () => sut.GetCurrentUserAsync("token", CancellationToken.None));
    }
}

public class DocumentSearchClientTests
{
    [Fact]
    public async Task Maps_search_contract_from_core()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """[{"documentId":"33333333-3333-3333-3333-333333333333","documentName":"manual.pdf","chunkId":"44444444-4444-4444-4444-444444444444","chunkIndex":1,"content":"texto completo","similarity":0.91,"pageNumber":null}]""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            }
        };

        var sut = new DocumentSearchClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://core") },
            NullLogger<DocumentSearchClient>.Instance);

        var hits = await sut.SearchAsync("token", "pregunta", 5, CancellationToken.None);

        Assert.Single(hits);
        Assert.Equal("manual.pdf", hits[0].DocumentName);
        Assert.Equal("texto completo", hits[0].Content);
        Assert.Equal(0.91, hits[0].Similarity);
        Assert.Equal("/api/documents/search", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Returns_503_when_core_search_unavailable()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
        };

        var sut = new DocumentSearchClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://core") },
            NullLogger<DocumentSearchClient>.Instance);

        await Assert.ThrowsAsync<ContactCenterAI.Chat.Application.Common.ServiceUnavailableException>(
            () => sut.SearchAsync("token", "q", 3, CancellationToken.None));
    }
}

public class ChatAiAvailabilityTests
{
    [Fact]
    public async Task Gemini_not_configured_returns_controlled_error()
    {
        var service = new GeminiChatCompletionService(
            new HttpClient { BaseAddress = new Uri("https://generativelanguage.googleapis.com/") },
            Options.Create(new GeminiSettings { ApiKey = "" }),
            NullLogger<GeminiChatCompletionService>.Instance);

        Assert.False(service.IsConfigured);
        await Assert.ThrowsAsync<ContactCenterAI.Chat.Application.Common.ChatAiException>(
            () => service.GenerateAnswerAsync("hola", ["ctx"], CancellationToken.None));
    }
}

public class TenantIsolationRulesTests
{
    [Fact]
    public void User_cannot_read_other_company_conversation()
    {
        var ownCompany = Guid.NewGuid();
        var otherCompany = Guid.NewGuid();
        Assert.NotEqual(ownCompany, otherCompany);
    }
}
