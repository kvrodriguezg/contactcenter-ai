using System.Net;
using System.Text.Json;
using ContactCenterAI.Bff.GraphQL.Models;

namespace ContactCenterAI.Bff.Tests;

public class BffGraphQlTests : IClassFixture<BffWebApplicationFactory>
{
    private readonly BffWebApplicationFactory _factory;

    private static readonly Guid CompanyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CompanyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SuperAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CompanyAdminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AgentId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public BffGraphQlTests(BffWebApplicationFactory factory)
    {
        _factory = factory;
        SeedHappyPath();
    }

    private void SeedHappyPath()
    {
        _factory.Core.ThrowUnavailable = false;
        _factory.Chat.ThrowUnavailable = false;

        _factory.Core.Companies =
        [
            new Company { Id = CompanyA, Name = "Acme", Status = "Active", CreatedAt = DateTime.UtcNow },
            new Company { Id = CompanyB, Name = "Beta", Status = "Active", CreatedAt = DateTime.UtcNow }
        ];

        _factory.Core.Users =
        [
            new User
            {
                Id = SuperAdminId, Email = "super@test.com", Role = "SuperAdmin",
                IsActive = true, CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = CompanyAdminId, Email = "admin@acme.com", Role = "CompanyAdmin",
                IsActive = true, CompanyId = CompanyA, CompanyName = "Acme", CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = AgentId, Email = "agent@acme.com", Role = "Agent",
                IsActive = true, CompanyId = CompanyA, CompanyName = "Acme", CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Email = "agent@beta.com", Role = "Agent",
                IsActive = true, CompanyId = CompanyB, CompanyName = "Beta", CreatedAt = DateTime.UtcNow
            }
        ];

        _factory.Core.Documents =
        [
            new Document
            {
                Id = Guid.Parse("d1111111-1111-1111-1111-111111111111"),
                OriginalFileName = "a.pdf", Status = "Ready", CompanyId = CompanyA,
                CompanyName = "Acme", UploadedByUserId = CompanyAdminId, SizeBytes = 10,
                CreatedAt = DateTime.UtcNow
            },
            new Document
            {
                Id = Guid.Parse("d2222222-2222-2222-2222-222222222222"),
                OriginalFileName = "b.pdf", Status = "Ready", CompanyId = CompanyB,
                CompanyName = "Beta", UploadedByUserId = AgentId, SizeBytes = 20,
                CreatedAt = DateTime.UtcNow
            }
        ];

        _factory.Core.Tickets =
        [
            new Ticket
            {
                Id = Guid.Parse("e1111111-1111-1111-1111-111111111111"),
                CompanyId = CompanyA, Subject = "Ticket A", Description = "d",
                Status = "Open", Priority = "High", CreatedByUserId = AgentId,
                CreatedAt = DateTime.UtcNow
            },
            new Ticket
            {
                Id = Guid.Parse("e2222222-2222-2222-2222-222222222222"),
                CompanyId = CompanyB, Subject = "Ticket B", Description = "d",
                Status = "Open", Priority = "Low", CreatedByUserId = AgentId,
                CreatedAt = DateTime.UtcNow
            }
        ];

        _factory.Chat.Conversations =
        [
            new Conversation
            {
                Id = Guid.Parse("f1111111-1111-1111-1111-111111111111"),
                CompanyId = CompanyA, ExternalUserId = AgentId, UserEmail = "agent@acme.com",
                Title = "Conv A", CreatedAt = DateTime.UtcNow,
                Messages =
                [
                    new ConversationMessage
                    {
                        Id = Guid.NewGuid(), Role = "User", Content = "Hola",
                        CreatedAt = DateTime.UtcNow
                    }
                ]
            },
            new Conversation
            {
                Id = Guid.Parse("f2222222-2222-2222-2222-222222222222"),
                CompanyId = CompanyB, ExternalUserId = AgentId, UserEmail = "agent@beta.com",
                Title = "Conv B", CreatedAt = DateTime.UtcNow,
                Messages = []
            }
        ];
    }

    private void SetCaller(string role, Guid userId, Guid? companyId, string email)
    {
        _factory.Core.Me = new CurrentUser
        {
            UserId = userId,
            Email = email,
            Role = role,
            CompanyId = companyId,
            CompanyName = companyId == CompanyA ? "Acme" : companyId == CompanyB ? "Beta" : null,
            IsActive = true
        };
    }

    [Fact]
    public async Task Health_responds_ok()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Graphql_without_token_is_rejected()
    {
        var client = _factory.CreateClient();
        var (status, body) = await GraphQlClient.PostAsync(client, "{ me { email } }");

        Assert.True(
            status == HttpStatusCode.Unauthorized
            || HasAuthError(body),
            $"Expected auth rejection. Status={status}, Body={body.RootElement}");
    }

    [Fact]
    public async Task Graphql_with_valid_token_works()
    {
        SetCaller("SuperAdmin", SuperAdminId, null, "super@test.com");
        var token = TestTokens.Create("SuperAdmin", SuperAdminId, email: "super@test.com");
        var client = _factory.CreateClient();

        var (status, body) = await GraphQlClient.PostAsync(client, "{ me { email role } }", token);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.False(HasErrors(body));
        Assert.Equal("super@test.com", body.RootElement.GetProperty("data").GetProperty("me").GetProperty("email").GetString());
    }

    [Fact]
    public async Task SuperAdmin_lists_companies()
    {
        SetCaller("SuperAdmin", SuperAdminId, null, "super@test.com");
        var token = TestTokens.Create("SuperAdmin", SuperAdminId);
        var client = _factory.CreateClient();

        var (_, body) = await GraphQlClient.PostAsync(client, "{ companies { id name } }", token);

        Assert.False(HasErrors(body));
        var companies = body.RootElement.GetProperty("data").GetProperty("companies");
        Assert.Equal(2, companies.GetArrayLength());
    }

    [Fact]
    public async Task CompanyAdmin_only_queries_own_company()
    {
        SetCaller("CompanyAdmin", CompanyAdminId, CompanyA, "admin@acme.com");
        var token = TestTokens.Create("CompanyAdmin", CompanyAdminId, CompanyA);
        var client = _factory.CreateClient();

        var (_, listBody) = await GraphQlClient.PostAsync(client, "{ companies { id } }", token);
        Assert.True(HasErrors(listBody));
        Assert.Contains("SuperAdmin", listBody.RootElement.ToString(), StringComparison.OrdinalIgnoreCase);

        var (_, ownBody) = await GraphQlClient.PostAsync(
            client,
            "query($id: UUID!) { companyById(id: $id) { id name } }",
            token,
            new { id = CompanyA });
        Assert.False(HasErrors(ownBody));
        Assert.Equal(
            "Acme",
            ownBody.RootElement.GetProperty("data").GetProperty("companyById").GetProperty("name").GetString());

        var (_, otherBody) = await GraphQlClient.PostAsync(
            client,
            "query($id: UUID!) { companyById(id: $id) { id name } }",
            token,
            new { id = CompanyB });
        Assert.True(HasErrors(otherBody));
    }

    [Fact]
    public async Task Agent_cannot_list_global_users()
    {
        SetCaller("Agent", AgentId, CompanyA, "agent@acme.com");
        var token = TestTokens.Create("Agent", AgentId, CompanyA);
        var client = _factory.CreateClient();

        var (_, body) = await GraphQlClient.PostAsync(client, "{ users { id email } }", token);

        Assert.True(HasErrors(body));
        Assert.Contains("Agent", body.RootElement.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Documents_respect_company_isolation()
    {
        SetCaller("CompanyAdmin", CompanyAdminId, CompanyA, "admin@acme.com");
        var token = TestTokens.Create("CompanyAdmin", CompanyAdminId, CompanyA);
        var client = _factory.CreateClient();

        var (_, body) = await GraphQlClient.PostAsync(
            client,
            "{ documents { id originalFileName companyId } }",
            token);

        Assert.False(HasErrors(body));
        var docs = body.RootElement.GetProperty("data").GetProperty("documents");
        Assert.Equal(1, docs.GetArrayLength());
        Assert.Equal(CompanyA.ToString(), docs[0].GetProperty("companyId").GetString());
    }

    [Fact]
    public async Task Conversations_respect_company_isolation()
    {
        SetCaller("Agent", AgentId, CompanyA, "agent@acme.com");
        var token = TestTokens.Create("Agent", AgentId, CompanyA);
        var client = _factory.CreateClient();

        var (_, body) = await GraphQlClient.PostAsync(
            client,
            "{ conversations { id title companyId } }",
            token);

        Assert.False(HasErrors(body));
        var items = body.RootElement.GetProperty("data").GetProperty("conversations");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(CompanyA.ToString(), items[0].GetProperty("companyId").GetString());
    }

    [Fact]
    public async Task Tickets_respect_company_isolation()
    {
        SetCaller("CompanyAdmin", CompanyAdminId, CompanyA, "admin@acme.com");
        var token = TestTokens.Create("CompanyAdmin", CompanyAdminId, CompanyA);
        var client = _factory.CreateClient();

        var (_, body) = await GraphQlClient.PostAsync(
            client,
            "{ tickets { id subject companyId } }",
            token);

        Assert.False(HasErrors(body));
        var items = body.RootElement.GetProperty("data").GetProperty("tickets");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("Ticket A", items[0].GetProperty("subject").GetString());
    }

    [Fact]
    public async Task Core_api_down_returns_controlled_error()
    {
        SetCaller("SuperAdmin", SuperAdminId, null, "super@test.com");
        _factory.Core.ThrowUnavailable = true;
        var token = TestTokens.Create("SuperAdmin", SuperAdminId);
        var client = _factory.CreateClient();

        var (status, body) = await GraphQlClient.PostAsync(client, "{ me { email } }", token);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.True(HasErrors(body));
        var text = body.RootElement.ToString();
        Assert.Contains("Core API", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at ContactCenterAI", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chat_api_down_returns_controlled_error()
    {
        SetCaller("Agent", AgentId, CompanyA, "agent@acme.com");
        _factory.Chat.ThrowUnavailable = true;
        var token = TestTokens.Create("Agent", AgentId, CompanyA);
        var client = _factory.CreateClient();

        var (status, body) = await GraphQlClient.PostAsync(
            client,
            "{ conversations { id } }",
            token);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.True(HasErrors(body));
        var text = body.RootElement.ToString();
        Assert.Contains("Chat API", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", text, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasErrors(JsonDocument body) =>
        body.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0;

    private static bool HasAuthError(JsonDocument body)
    {
        if (!HasErrors(body))
        {
            return false;
        }

        var text = body.RootElement.ToString();
        return text.Contains("auth", StringComparison.OrdinalIgnoreCase)
            || text.Contains("AUTH_", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase);
    }
}
