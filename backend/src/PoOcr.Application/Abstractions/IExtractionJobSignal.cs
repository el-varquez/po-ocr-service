namespace PoOcr.Application.Abstractions;

public interface IExtractionJobSignal
{
    void NotifyJobQueued();
    Task WaitForJobAsync(CancellationToken cancellationToken);
}