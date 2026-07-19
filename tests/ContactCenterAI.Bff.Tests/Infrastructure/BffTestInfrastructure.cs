using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ContactCenterAI.Bff.Clients;
using ContactCenterAI.Bff.GraphQL.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContactCenterAI.Bff.Tests;

public class BffWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string JwtSecret = "DEV_ONLY_SECRET_KEY_CHANGE_IN_PRODUCTION_123456";
    public const string Issuer = "ContactCenterAI";
    public const string Audience = "ContactCenterAI.Client";

    public FakeCoreApiClient Core { get; } = new();
    public FakeChatApiClient Chat { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("CoreApi:BaseUrl", "http://core.test/");
        builder.UseSetting("ChatApi:BaseUrl", "http://chat.test/");
        builder.UseSetting("AUTH_PROVIDER", "Local");
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Jwt:SecretKey", JwtSecret);
        builder.UseSetting("Cors:Origins:0", "http://localhost:5173");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICoreApiClient>();
            services.RemoveAll<IChatApiClient>();
            services.AddSingleton<ICoreApiClient>(Core);
            services.AddSingleton<IChatApiClient>(Chat);
        });
    }
}

public sealed class FakeCoreApiClient : ICoreApiClient
{
    public bool ThrowUnavailable { get; set; }
    public CurrentUser? Me { get; set; }
    public List<Company> Companies { get; set; } = [];
    public List<User> Users { get; set; } = [];
    public List<Document> Documents { get; set; } = [];
    public List<Ticket> Tickets { get; set; } = [];

    private void EnsureAvailable()
    {
        if (ThrowUnavailable)
        {
            throw new DownstreamApiException("CoreApi", "Core API no está disponible.");
        }
    }

    public Task<CurrentUser?> GetCurrentUserAsync(CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult(Me);
    }

    public Task<IReadOnlyList<Company>> GetCompaniesAsync(CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult<IReadOnlyList<Company>>(Companies);
    }

    public Task<Company?> GetCompanyByIdAsync(Guid id, CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult(Companies.FirstOrDefault(c => c.Id == id));
    }

    public Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult<IReadOnlyList<User>>(Users);
    }

    public Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    }

    public Task<IReadOnlyList<Document>> GetDocumentsAsync(CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult<IReadOnlyList<Document>>(Documents);
    }

    public Task<Document?> GetDocumentByIdAsync(Guid id, CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult(Documents.FirstOrDefault(d => d.Id == id));
    }

    public Task<IReadOnlyList<Ticket>> GetTicketsAsync(CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult<IReadOnlyList<Ticket>>(Tickets);
    }

    public Task<Ticket?> GetTicketByIdAsync(Guid id, CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult(Tickets.FirstOrDefault(t => t.Id == id));
    }
}

public sealed class FakeChatApiClient : IChatApiClient
{
    public bool ThrowUnavailable { get; set; }
    public List<Conversation> Conversations { get; set; } = [];

    private void EnsureAvailable()
    {
        if (ThrowUnavailable)
        {
            throw new DownstreamApiException("ChatApi", "Chat API no está disponible.");
        }
    }

    public Task<IReadOnlyList<Conversation>> GetConversationsAsync(CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult<IReadOnlyList<Conversation>>(
            Conversations.Select(c => new Conversation
            {
                Id = c.Id,
                CompanyId = c.CompanyId,
                ExternalUserId = c.ExternalUserId,
                UserEmail = c.UserEmail,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Messages = null
            }).ToList());
    }

    public Task<Conversation?> GetConversationByIdAsync(Guid id, CancellationToken ct)
    {
        EnsureAvailable();
        return Task.FromResult(Conversations.FirstOrDefault(c => c.Id == id));
    }
}

public static class TestTokens
{
    public static string Create(
        string role,
        Guid? userId = null,
        Guid? companyId = null,
        string email = "user@test.com")
    {
        var id = userId ?? Guid.NewGuid();
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, id.ToString()),
            new(System.Security.Claims.ClaimTypes.NameIdentifier, id.ToString()),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, email),
            new(System.Security.Claims.ClaimTypes.Email, email),
            new(System.Security.Claims.ClaimTypes.Role, role)
        };

        if (companyId is not null)
        {
            claims.Add(new System.Security.Claims.Claim("companyId", companyId.Value.ToString()));
        }

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(BffWebApplicationFactory.JwtSecret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: BffWebApplicationFactory.Issuer,
            audience: BffWebApplicationFactory.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class GraphQlClient
{
    public static async Task<(HttpStatusCode Status, JsonDocument Body)> PostAsync(
        HttpClient client,
        string query,
        string? token = null,
        object? variables = null)
    {
        client.DefaultRequestHeaders.Authorization = token is null
            ? null
            : new AuthenticationHeaderValue("Bearer", token);

        var payload = new Dictionary<string, object?> { ["query"] = query };
        if (variables is not null)
        {
            payload["variables"] = variables;
        }

        using var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/graphql", content);
        var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        return (response.StatusCode, doc);
    }
}
