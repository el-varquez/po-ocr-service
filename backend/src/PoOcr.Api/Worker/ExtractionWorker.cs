using PoOcr.Application.Abstractions;

namespace PoOcr.Api.Workers;

public sealed class ExtractionWorker : BackgroundService
{
    private readonly ILogger<ExtractionWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IExtractionJobSignal _signal;

    public ExtractionWorker(
        ILogger<ExtractionWorker> logger,
        IServiceScopeFactory scopeFactory,
        IExtractionJobSignal signal)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _signal = signal; 
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var drainPending = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!drainPending)
                    await _signal.WaitForJobAsync(stoppingToken);

                drainPending = false;
                await DrainQueueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Extraction worker failed.");
                drainPending = true;
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public async Task<int> DrainQueueAsync(CancellationToken cancellationToken)
    {
        var processed = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IExtractionJobProcessor>();

            var result = await processor.Handle(cancellationToken);
            if(!result.ProcessedJob)
                break;

            processed++;
        }

        return processed;
    }
}