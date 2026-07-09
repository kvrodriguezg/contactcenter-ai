using ContactCenterAI.Application.Common.Interfaces;
using ContactCenterAI.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace ContactCenterAI.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DocumentProcessingSettings> _settings;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IOptions<DocumentProcessingSettings> settings,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ContactCenterAI Worker iniciado. Intervalo de procesamiento: {IntervalSeconds}s",
            _settings.Value.IntervalSeconds);

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
