using System.Net;
using ContactCenterAI.Chat.Application;
using ContactCenterAI.Chat.Application.Chat.Commands.AskQuestion;
using ContactCenterAI.Chat.Application.Chat.Queries.GetConversationById;
using ContactCenterAI.Chat.Application.Chat.Queries.ListConversations;
using ContactCenterAI.Chat.Application.Common;
using ContactCenterAI.Chat.Infrastructure;
using ContactCenterAI.Chat.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        .WriteTo.File("logs/chat-api-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddChatApplication();
    builder.Services.AddChatInfrastructure(builder.Configuration);
    builder.Services.AddChatAuthentication(builder.Configuration, builder.Environment);

    builder.Services.AddControllers();
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ChatDbContext>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:5173"];

            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (ServiceUnavailableException ex)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (ChatAiException ex)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (CoreApiException ex)
        {
            context.Response.StatusCode = ex.StatusCode >= 400 && ex.StatusCode < 600
                ? ex.StatusCode
                : StatusCodes.Status502BadGateway;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Solicitud inválida.",
                errors = ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }
    });

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await db.Database.MigrateAsync();
    }

    app.UseSerilogRequestLogging();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "ContactCenterAI.Chat.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;

namespace ContactCenterAI.Chat.Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ChatController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask(
            [FromBody] AskQuestionRequest request,
            CancellationToken cancellationToken)
        {
            var token = ExtractBearerToken();
            var result = await _mediator.Send(
                new AskQuestionCommand(
                    request.Question,
                    request.ConversationId,
                    request.TopK <= 0 ? 5 : request.TopK,
                    token),
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
        {
            var token = ExtractBearerToken();
            var result = await _mediator.Send(
                new ListConversationsQuery(token),
                cancellationToken);
            return Ok(result);
        }

        [HttpGet("conversations/{id:guid}")]
        public async Task<IActionResult> GetConversationById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var token = ExtractBearerToken();
            var result = await _mediator.Send(
                new GetConversationByIdQuery(id, token),
                cancellationToken);
            return Ok(result);
        }

        private string ExtractBearerToken()
        {
            var header = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(header)
                || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Token de autenticación ausente.");
            }

            return header["Bearer ".Length..].Trim();
        }
    }

    public class AskQuestionRequest
    {
        public string Question { get; set; } = string.Empty;

        public Guid? ConversationId { get; set; }

        public int TopK { get; set; } = 5;
    }
}
