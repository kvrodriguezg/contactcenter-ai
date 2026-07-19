using ContactCenterAI.Domain.Identity;
using ContactCenterAI.Domain.Tenancy;
using ContactCenterAI.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace ContactCenterAI.Infrastructure.Tests.Identity;

public class AuthenticationSettingsTests
{
    [Theory]
    [InlineData("Local", true, false)]
    [InlineData("local", true, false)]
    [InlineData("Auth0", false, true)]
    [InlineData("auth0", false, true)]
    public void Feature_flag_resolves_provider(string provider, bool isLocal, bool isAuth0)
    {
        var settings = new AuthenticationSettings { Provider = provider };

        Assert.Equal(isLocal, settings.IsLocal);
        Assert.Equal(isAuth0, settings.IsAuth0);
    }
    [Fact]
    public void Local_provider_keeps_password_login_enabled()
    {
        var settings = new AuthenticationSettings { Provider = AuthenticationProviders.Local };

        Assert.True(settings.IsLocal);
        Assert.False(settings.IsAuth0);
    }

    [Fact]
    public void Auth0_provider_signals_local_login_disabled()
    {
        var settings = new AuthenticationSettings { Provider = AuthenticationProviders.Auth0 };

        Assert.True(settings.IsAuth0);
        Assert.False(settings.IsLocal);
    }
}

public class Auth0TokenValidationTests
{
    [Fact]
    public void Creates_parameters_with_exact_issuer_and_audience()
    {
        var settings = new Auth0Settings
        {
            Domain = "tenant.auth0.com",
            Audience = "https://contactcenterai-api"
        };

        var parameters = Auth0TokenValidation.Create(settings);

        Assert.True(parameters.ValidateIssuer);
        Assert.True(parameters.ValidateAudience);
        Assert.True(parameters.ValidateLifetime);
        Assert.Equal("https://tenant.auth0.com/", parameters.ValidIssuer);
        Assert.Equal("https://contactcenterai-api", parameters.ValidAudience);
    }

    [Fact]
    public void Rejects_incomplete_auth0_configuration()
    {
        var settings = new Auth0Settings
        {
            Domain = "",
            Audience = "https://contactcenterai-api"
        };

        Assert.Throws<InvalidOperationException>(() => Auth0TokenValidation.Create(settings));
    }

    [Fact]
    public void Authority_normalizes_domain_without_scheme()
    {
        var settings = new Auth0Settings { Domain = "tenant.auth0.com/" };

        Assert.Equal("https://tenant.auth0.com/", settings.Authority);
        Assert.Equal(settings.Authority, settings.Issuer);
    }
}

public class AuthenticationConfigurationTests
{
    [Fact]
    public void AUTH_PROVIDER_env_overrides_section()
    {
        var configuration = new TestConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Provider"] = "Local",
            ["AUTH_PROVIDER"] = "Auth0"
        });

        var settings = AuthenticationConfiguration.ResolveAuthenticationSettings(configuration);

        Assert.True(settings.IsAuth0);
    }

    [Fact]
    public void AUTH0_flat_env_overrides_section()
    {
        var configuration = new TestConfiguration(new Dictionary<string, string?>
        {
            ["Auth0:Domain"] = "from-section.auth0.com",
            ["Auth0:Audience"] = "https://old-audience",
            ["AUTH0_DOMAIN"] = "from-env.auth0.com",
            ["AUTH0_AUDIENCE"] = "https://contactcenterai-api"
        });

        var settings = AuthenticationConfiguration.ResolveAuth0Settings(configuration);

        Assert.Equal("from-env.auth0.com", settings.Domain);
        Assert.Equal("https://contactcenterai-api", settings.Audience);
    }

    private sealed class TestConfiguration : IConfiguration
    {
        private readonly Dictionary<string, string?> _values;

        public TestConfiguration(Dictionary<string, string?> values)
        {
            _values = values;
        }

        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var value) ? value : null;
            set => _values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() =>
            Array.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken() => new TestChangeToken();

        public IConfigurationSection GetSection(string key) =>
            new TestConfigurationSection(key, this);
    }

    private sealed class TestConfigurationSection : IConfigurationSection
    {
        private readonly TestConfiguration _root;

        public TestConfigurationSection(string path, TestConfiguration root)
        {
            Path = path;
            Key = path.Contains(':') ? path[(path.LastIndexOf(':') + 1)..] : path;
            _root = root;
        }

        public string? this[string key]
        {
            get => _root[$"{Path}:{key}"];
            set => _root[$"{Path}:{key}"] = value;
        }

        public string Key { get; }

        public string Path { get; }

        public string? Value
        {
            get => _root[Path];
            set => _root[Path] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            var prefix = Path + ":";
            return _root.GetChildren()
                .Select(_ => _)
                .Where(_ => false);
        }

        public IChangeToken GetReloadToken() => new TestChangeToken();

        public IConfigurationSection GetSection(string key) =>
            new TestConfigurationSection($"{Path}:{key}", _root);
    }

    private sealed class TestChangeToken : IChangeToken
    {
        public bool HasChanged => false;

        public bool ActiveChangeCallbacks => false;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) =>
            EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}

public class LocalUserResolverTests : IAsyncLifetime
{
    private readonly AuthTestDbContext _dbContext;

    public LocalUserResolverTests()
    {
        var options = new DbContextOptionsBuilder<AuthTestDbContext>()
            .UseInMemoryDatabase($"auth-tests-{Guid.NewGuid()}")
            .Options;

        _dbContext = new AuthTestDbContext(options);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Access_without_token_fails()
    {
        var resolver = CreateResolver(AuthenticationProviders.Local);
        var result = await resolver.ResolveAsync(new ClaimsPrincipal());

        Assert.False(result.Succeeded);
        Assert.Equal(LocalUserResolutionErrors.NotAuthenticated, result.ErrorCode);
    }

    [Fact]
    public async Task Local_login_resolves_by_user_id_claim()
    {
        var user = await SeedUserAsync(email: "local@test.com", isActive: true);
        var resolver = CreateResolver(AuthenticationProviders.Local);
        var principal = CreatePrincipal(user.Id.ToString(), user.Email);

        var result = await resolver.ResolveAsync(principal);

        Assert.True(result.Succeeded);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.CompanyId, result.CompanyId);
        Assert.Equal(Role.Agent, result.Role);
    }

    [Fact]
    public async Task Auth0_associates_by_external_subject()
    {
        var user = await SeedUserAsync(
            email: "linked@test.com",
            isActive: true,
            externalSubject: "auth0|abc123");

        var resolver = CreateResolver(AuthenticationProviders.Auth0);
        var principal = CreatePrincipal("auth0|abc123", user.Email);

        var result = await resolver.ResolveAsync(principal);

        Assert.True(result.Succeeded);
        Assert.Equal(user.Id, result.UserId);
    }

    [Fact]
    public async Task Auth0_resolves_authenticated_user_by_sub_claim()
    {
        var user = await SeedUserAsync(
            email: "prelinked@test.com",
            isActive: true,
            externalSubject: "auth0|687d1234567890abcdef");

        var resolver = CreateResolver(AuthenticationProviders.Auth0);
        // Different email in the token must not matter when ExternalSubject matches `sub`.
        var principal = CreatePrincipal("auth0|687d1234567890abcdef", "other-email@test.com");

        var result = await resolver.ResolveAsync(principal);

        Assert.True(result.Succeeded);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task Auth0_fallback_by_email_links_external_subject()
    {
        var user = await SeedUserAsync(email: "fallback@test.com", isActive: true);
        var resolver = CreateResolver(AuthenticationProviders.Auth0);
        var principal = CreatePrincipal("auth0|new-sub", user.Email);

        var result = await resolver.ResolveAsync(principal);

        Assert.True(result.Succeeded);
        Assert.Equal(user.Id, result.UserId);

        await _dbContext.Entry(user).ReloadAsync();
        Assert.Equal("auth0|new-sub", user.ExternalSubject);
        Assert.Equal(AuthenticationProvider.Auth0, user.AuthenticationProvider);
    }

    [Fact]
    public async Task Inactive_user_is_rejected()
    {
        var user = await SeedUserAsync(email: "inactive@test.com", isActive: false);
        var resolver = CreateResolver(AuthenticationProviders.Local);
        var principal = CreatePrincipal(user.Id.ToString(), user.Email);

        var result = await resolver.ResolveAsync(principal);

        Assert.False(result.Succeeded);
        Assert.Equal(LocalUserResolutionErrors.UserInactive, result.ErrorCode);
    }

    [Fact]
    public async Task CompanyId_comes_from_local_profile_not_claims()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var user = await SeedUserAsync(
            email: "tenant@test.com",
            isActive: true,
            companyId: companyA);

        var resolver = CreateResolver(AuthenticationProviders.Local);
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("companyId", companyB.ToString())
        ], "TestAuth");

        var result = await resolver.ResolveAsync(new ClaimsPrincipal(identity));

        Assert.True(result.Succeeded);
        Assert.Equal(companyA, result.CompanyId);
        Assert.NotEqual(companyB, result.CompanyId);
    }

    [Fact]
    public async Task Auth0_unknown_user_is_not_registered()
    {
        var resolver = CreateResolver(AuthenticationProviders.Auth0);
        var principal = CreatePrincipal("auth0|unknown", "unknown@test.com");

        var result = await resolver.ResolveAsync(principal);

        Assert.False(result.Succeeded);
        Assert.Equal(LocalUserResolutionErrors.UserNotRegistered, result.ErrorCode);
    }

    private LocalUserResolver CreateResolver(string provider)
    {
        var settings = Options.Create(new AuthenticationSettings { Provider = provider });
        return new LocalUserResolver(_dbContext, settings);
    }

    private static ClaimsPrincipal CreatePrincipal(string subject, string email)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim("sub", subject),
            new Claim(ClaimTypes.Email, email),
            new Claim("email", email)
        ], "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private async Task<User> SeedUserAsync(
        string email,
        bool isActive,
        string? externalSubject = null,
        Guid? companyId = null)
    {
        var company = new Company
        {
            Id = companyId ?? Guid.NewGuid(),
            Name = "Test Co",
            Status = CompanyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "hash",
            Role = Role.Agent,
            IsActive = isActive,
            CompanyId = company.Id,
            ExternalSubject = externalSubject,
            AuthenticationProvider = externalSubject is null
                ? AuthenticationProvider.Local
                : AuthenticationProvider.Auth0,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Companies.Add(company);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }
}
