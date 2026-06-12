using PoOcr.Infrastructure.Messaging;

namespace PoOcr.Infrastructure.Tests.Messaging;

public sealed class ChannelExtractionJobSignalTests
{
    [Fact]
    public async Task WaitForJobAsync_AfterNotify_Completes()
    {
        var signal = new ChannelExtractionJobSignal();
        signal.NotifyJobQueued();

        var wait = signal.WaitForJobAsync(CancellationToken.None);
        var completed = await Task.WhenAny(wait, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.Same(wait, completed);
    }

    [Fact]
    public async Task WaitForJobAsync_WithoutNotify_DoesNotComplete()
    {
        var signal = new ChannelExtractionJobSignal();

        var wait = signal.WaitForJobAsync(CancellationToken.None);
        var completed = await Task.WhenAny(wait, Task.Delay(TimeSpan.FromMilliseconds(100)));

        Assert.NotSame(wait, completed);
    }

    [Fact]
    public async Task NotifyJobQueued_CalledManyTimes_CoalescesIntoSinglePendingSignal()
    {
        var signal = new ChannelExtractionJobSignal();
        signal.NotifyJobQueued();
        signal.NotifyJobQueued();
        signal.NotifyJobQueued();

        await signal.WaitForJobAsync(CancellationToken.None);

        var secondWait = signal.WaitForJobAsync(CancellationToken.None);
        var completed = await Task.WhenAny(secondWait, Task.Delay(TimeSpan.FromMilliseconds(100)));

        Assert.NotSame(secondWait, completed);
    }
}