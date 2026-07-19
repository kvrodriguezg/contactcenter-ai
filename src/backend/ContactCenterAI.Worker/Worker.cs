using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Application.Common.Messaging;
using ContactCenterAI.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DocumentProcessingSettings> _settings;
    private readonly IOptions<MessagingSettings> _messagingSettings;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IOptions<DocumentProcessingSettings> settings,
        IOptions<MessagingSettings> messagingSettings,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _messagingSettings = messagingSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var messagingEnabled = _messagingSettings.Value.Enabled;
        var pollingEnabled = _settings.Value.PollingEnabled;

        if (!pollingEnabled)
        {
            if (messagingEnabled)
            {
                _logger.LogInformation(
                    "Polling de documentos deshabilitado (DocumentProcessing:PollingEnabled=false). "
                    + "El Worker depende de los consumidores RabbitMQ; sin polling constante.");
            }
            else
            {
                _logger.LogWarning(
                    "Polling y mensajería deshabilitados: ningún mecanismo procesará documentos pendientes.");
            }

            return;
        }

        if (messagingEnabled)
        {
            _logger.LogInformation(
                "ContactCenterAI Worker en modo reconciliación (Messaging:Enabled=true). "
                + "Polling cada {IntervalSeconds}s como fallback seguro ante fallos del broker",
                _settings.Value.IntervalSeconds);
        }
        else
        {
            _logger.LogInformation(
                "ContactCenterAI Worker iniciado en modo polling (Messaging:Enabled=false). "
                + "Intervalo {IntervalSeconds}s",
                _settings.Value.IntervalSeconds);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processingService = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();
                var processedCount = await processingService.ProcessPendingDocumentsAsync(stoppingToken);

                if (processedCount > 0)
                {
                    _logger.LogInformation(
                        "Ciclo de procesamiento completado. Documentos procesados: {ProcessedCount}",
                        processedCount);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Error en el ciclo de procesamiento de documentos");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.Value.IntervalSeconds), stoppingToken);
        }
    }
}
