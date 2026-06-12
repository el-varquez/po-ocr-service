using System.Threading.Channels;
using PoOcr.Application.Abstractions;

namespace PoOcr.Infrastructure.Messaging;

public sealed class ChannelExtractionJobSignal : IExtractionJobSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        }       
    );

    public void NotifyJobQueued() => _channel.Writer.TryWrite(true);

    public Task WaitForJobAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAsync(cancellationToken).AsTask();
}