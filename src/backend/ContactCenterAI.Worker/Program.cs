using ContactCenterAI.Application;
using ContactCenterAI.Infrastructure;
using ContactCenterAI.Infrastructure.Messaging;
using ContactCenterAI.Worker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((_, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddMessagingConsumers(builder.Configuration);

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "ContactCenterAI.Worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
