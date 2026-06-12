using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PoOcr.Api.Workers;
using PoOcr.Application.Abstractions;
using PoOcr.Application.Extraction;
using PoOcr.Infrastructure.Messaging;

namespace PoOcr.Api.Tests;

public sealed class ExtractionWorkerTests
{
    [Fact]
    public async Task DrainQueueAsync_ProcessesJobsUntilQueueIsEmpty()
    {
        var processor = new FakeExtractionJobProcessor(jobsAvailable: 2);
        var worker = CreateWorker(processor, new ChannelExtractionJobSignal());

        var processed = await worker.DrainQueueAsync(CancellationToken.None);

        Assert.Equal(2, processed);
        Assert.Equal(3, processor.CallCount); 
    }

    [Fact]
    public async Task DrainQueueAsync_WhenQueueIsEmpty_ChecksOnceAndStops()
    {
        var processor = new FakeExtractionJobProcessor(jobsAvailable: 0);
        var worker = CreateWorker(processor, new ChannelExtractionJobSignal());

        var processed = await worker.DrainQueueAsync(CancellationToken.None);

        Assert.Equal(0, processed);
        Assert.Equal(1, processor.CallCount);
    }

    [Fact]
    public async Task Worker_WhenSignalled_WakesUpAndProcessesQueue()
    {
        var processor = new FakeExtractionJobProcessor(jobsAvailable: 0);
        var signal = new ChannelExtractionJobSignal();
        var worker = CreateWorker(processor, signal);

        await worker.StartAsync(CancellationToken.None);

        // Wait for the startup drain (one empty check), after which the
        // worker is awaiting the signal — not polling.
        await WaitUntilAsync(() => processor.CallCount >= 1);

        processor.AddJobs(1);
        signal.NotifyJobQueued();

        await WaitUntilAsync(() => processor.ProcessedCount >= 1);

        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, processor.ProcessedCount);
    }

    private static ExtractionWorker CreateWorker(
        IExtractionJobProcessor processor,
        IExtractionJobSignal signal)
    {
        var services = new ServiceCollection()
            .AddScoped<IExtractionJobProcessor>(_ => processor)
            .BuildServiceProvider();

        return new ExtractionWorker(
            NullLogger<ExtractionWorker>.Instance,
            services.GetRequiredService<IServiceScopeFactory>(),
            signal);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Condition was not met within 5 seconds.");

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }
    }

    private sealed class FakeExtractionJobProcessor(int jobsAvailable) : IExtractionJobProcessor
    {
        private int _jobsAvailable = jobsAvailable;
        private int _callCount;
        private int _processedCount;

        public int CallCount => Volatile.Read(ref _callCount);
        public int ProcessedCount => Volatile.Read(ref _processedCount);

        public void AddJobs(int count) => Interlocked.Add(ref _jobsAvailable, count);

        public Task<ProcessNextExtractionJobResult> Handle(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);

            if (Interlocked.Decrement(ref _jobsAvailable) >= 0)
            {
                Interlocked.Increment(ref _processedCount);
                return Task.FromResult(new ProcessNextExtractionJobResult(true, true, ""));
            }

            Interlocked.Increment(ref _jobsAvailable); // restore the floor at zero
            return Task.FromResult(new ProcessNextExtractionJobResult(true, false, ""));
        }
    }
}