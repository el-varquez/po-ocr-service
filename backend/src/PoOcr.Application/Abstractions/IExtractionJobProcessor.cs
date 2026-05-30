using PoOcr.Application.Extraction;

namespace PoOcr.Application.Abstractions;

public interface IExtractionJobProcessor
{
    Task<ProcessNextExtractionJobResult> Handle(CancellationToken cancellationToken);
}
