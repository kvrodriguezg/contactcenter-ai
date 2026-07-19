using ContactCenterAI.Bff.Authentication;
using ContactCenterAI.Bff.Clients;
using ContactCenterAI.Bff.GraphQL;
using ContactCenterAI.Bff.GraphQL.Types;
using ContactCenterAI.Bff.Security;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, _, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/bff-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddBffAuthentication(builder.Configuration, builder.Environment);

    var coreBaseUrl = builder.Configuration["CoreApi:BaseUrl"]
        ?? throw new InvalidOperationException("CoreApi:BaseUrl (CoreApi__BaseUrl) es obligatorio.");
    var chatBaseUrl = builder.Configuration["ChatApi:BaseUrl"]
        ?? throw new InvalidOperationException("ChatApi:BaseUrl (ChatApi__BaseUrl) es obligatorio.");

    builder.Services.AddTransient<AuthHeaderForwardingHandler>();

    builder.Services.AddHttpClient<ICoreApiClient, CoreApiClient>(client =>
        {
            client.BaseAddress = new Uri(EnsureTrailingSlash(coreBaseUrl));
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderForwardingHandler>();

    builder.Services.AddHttpClient<IChatApiClient, ChatApiClient>(client =>
        {
            client.BaseAddress = new Uri(EnsureTrailingSlash(chatBaseUrl));
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthHeaderForwardingHandler>();

    builder.Services.AddScoped<BffCallerContext>();

    builder.Services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .AddTypeExtension<CompanyTypeExtensions>()
        .AddTypeExtension<UserTypeExtensions>()
        .AddTypeExtension<DocumentTypeExtensions>()
        .AddTypeExtension<TicketTypeExtensions>()
        .AddTypeExtension<ConversationTypeExtensions>()
        .AddAuthorization()
        .AddMaxExecutionDepthRule(7, skipIntrospectionFields: true)
        .ModifyRequestOptions(options =>
        {
            options.IncludeExceptionDetails = false;
            options.ExecutionTimeout = TimeSpan.FromSeconds(30);
        })
        .ModifyCostOptions(options =>
        {
            options.EnforceCostLimits = true;
            options.MaxFieldCost = 1000;
            options.MaxTypeCost = 2000;
        })
        .AddErrorFilter(DownstreamErrorHandling.Map);

    builder.Services.AddHealthChecks();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            var origins = ResolveCorsOrigins(builder.Configuration);

            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        };
    });

    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapGraphQL("/graphql");

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "ContactCenterAI.Bff terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static string EnsureTrailingSlash(string url) =>
    url.EndsWith('/') ? url : url + "/";

static string[] ResolveCorsOrigins(IConfiguration configuration)
{
    var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    void AddOrigin(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var origin = part.TrimEnd('/');
            if (origin.Length > 0)
            {
                origins.Add(origin);
            }
        }
    }

    var fromSection = configuration.GetSection("Cors:Origins").Get<string[]>();
    if (fromSection is not null)
    {
        foreach (var item in fromSection)
        {
            AddOrigin(item);
        }
    }

    AddOrigin(configuration["WEB_ORIGIN"]);

    if (origins.Count == 0)
    {
        origins.Add("http://localhost:5173");
    }

    return [.. origins];
}

public partial class Program;
