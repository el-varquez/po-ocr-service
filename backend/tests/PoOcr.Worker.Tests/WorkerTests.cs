using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PoOcr.Application.Abstractions;
using PoOcr.Application.Extraction;

namespace PoOcr.Worker.Tests;

public sealed class WorkerTests
{
    [Fact]
    public async Task ProcessOnceAsync_WhenProcessorIsRegistered_ProcessesNextExtractionJob()
    {
        var processor = new FakeExtractionJobProcessor();
        var services = new ServiceCollection()
            .AddScoped<IExtractionJobProcessor>(_ => processor)
            .BuildServiceProvider();

        var worker = new PoOcr.Worker.Worker(
            NullLogger<PoOcr.Worker.Worker>.Instance,
            services.GetRequiredService<IServiceScopeFactory>());

        var result = await worker.ProcessOnceAsync(CancellationToken.None);

        Assert.True(result.ProcessedJob);
        Assert.Equal(1, processor.CallCount);
    }

    private sealed class FakeExtractionJobProcessor : IExtractionJobProcessor
    {
        public int CallCount { get; private set; }

        public Task<ProcessNextExtractionJobResult> Handle(CancellationToken cancellationToken)
        {
            CallCount++;

            return Task.FromResult(new ProcessNextExtractionJobResult(
                IsSuccess: true,
                ProcessedJob: true,
                Error: ""));
        }
    }
}
