using PoOcr.Application.Abstractions;
using PoOcr.Application.Extraction;
using PoOcr.Domain.Audit;
using PoOcr.Domain.Drafts;
using PoOcr.Domain.Extraction;
using PoOcr.Domain.Uploads;

namespace PoOcr.Application.Tests;

public sealed class ProcessNextExtractionJobUseCaseTests
{
    [Fact]
    public async Task
Handle_WhenQueuedJobExists_CreatesDraftAndMovesUploadToNeedsReview()
    {
        var upload = UploadFile.Create(
            "sample-po.pdf",
            "application/pdf",
            1250,
            "uploads/sample-po.pdf",
            "abc123",
            "admin");

        upload.QueueForExtraction();

        var job = ExtractionJob.Create(upload.Id);

        var uploadRepository = new FakeUploadRepository([upload]);
        var extractionJobRepository = new
FakeExtractionJobRepository(job);
        var draftRepository = new FakeDraftRepository();
        var fileTextExtractor = new FakeFileTextExtractor("PO No: PO-1001");
        var purchaseOrderParser = new FakePurchaseOrderParser();
        var auditWriter = new FakeAuditWriter();

        var useCase = new ProcessNextExtractionJobUseCase(
            extractionJobRepository,
            uploadRepository,
            draftRepository,
            fileTextExtractor,
            purchaseOrderParser,
            auditWriter);

        var result = await useCase.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.ProcessedJob);
        Assert.Equal(UploadStatus.NeedsReview, upload.Status);
        Assert.True(job.IsCompleted);
        Assert.Single(draftRepository.AddedDrafts);
        var draft = draftRepository.AddedDrafts[0];
        Assert.Equal("Computer Seller", draft.VendorName);
        Assert.Equal("0016", draft.ReferenceNumber);
        Assert.Equal(new DateOnly(2026, 5, 31), draft.PoDate);
        Assert.Equal(new DateOnly(2026, 6, 30), draft.DateExpected);
        Assert.Equal("Courier", draft.ShipVia);
        Assert.Equal("Net 30", draft.PaymentTerms);
        Assert.Equal(2615, draft.TotalAmount);

        var line = Assert.Single(draft.Lines);
        Assert.Equal(5, line.Quantity);
        Assert.Equal("MON2000", line.ItemCode);
        Assert.Equal(523, line.UnitPrice);
        Assert.Equal(2615, line.Amount);
        Assert.Contains(auditWriter.Events, x => x.Action ==
"extraction.completed");
    }

    [Fact]
    public async Task
Handle_WhenNoQueuedJobExists_ReturnsSuccessWithoutProcessing()
    {
        var useCase = new ProcessNextExtractionJobUseCase(
            new FakeExtractionJobRepository(null),
            new FakeUploadRepository([]),
            new FakeDraftRepository(),
            new FakeFileTextExtractor(""),
            new FakePurchaseOrderParser(),
            new FakeAuditWriter());

        var result = await useCase.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.ProcessedJob);
    }

    [Fact]
    public async Task
Handle_WhenTextExtractionFails_MarksUploadAndJobFailed()
    {
        var upload = UploadFile.Create(
            "bad-po.pdf",
            "application/pdf",
            1250,
            "uploads/bad-po.pdf",
            "abc123",
            "admin");

        upload.QueueForExtraction();

        var job = ExtractionJob.Create(upload.Id);

        var useCase = new ProcessNextExtractionJobUseCase(
            new FakeExtractionJobRepository(job),
            new FakeUploadRepository([upload]),
            new FakeDraftRepository(),
            new FailingFileTextExtractor(),
            new FakePurchaseOrderParser(),
            new FakeAuditWriter());

        var result = await useCase.Handle(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.ProcessedJob);
        Assert.Equal(UploadStatus.Failed, upload.Status);
        Assert.True(job.IsFailed);
        Assert.Contains("OCR text file was not found",
upload.FailureReason);
    }

    private sealed class FakeUploadRepository(List<UploadFile> uploads) : IUploadRepository
    {
        public Task<IReadOnlyList<UploadFile>> GetByIdAsync(
            IReadOnlyCollection<Guid> uploadIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<UploadFile> matches = uploads
                .Where(x => uploadIds.Contains(x.Id))
                .ToList();

            return Task.FromResult(matches);
        }

        public Task<IReadOnlyList<UploadFile>> GetRecentAsync(
            int take,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<UploadFile>>(
                uploads.Take(take).ToList());
        }

        public Task<IReadOnlyList<UploadFile>> GetHistoryAsync(
            int take,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<UploadFile>>(
                uploads.Take(take).ToList());
        }

        public Task AddAsync(
            UploadFile upload,
            CancellationToken cancellationToken)
        {
            uploads.Add(upload);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeExtractionJobRepository(ExtractionJob?
job) : IExtractionJobRepository
    {
        public Task AddAsync(
            ExtractionJob job,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ExtractionJob?> GetNextQueuedAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(job);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDraftRepository : IDraftRepository
    {
        public List<PoDraft> AddedDrafts { get; } = [];

        public Task AddAsync(
            PoDraft draft,
            CancellationToken cancellationToken)
        {
            AddedDrafts.Add(draft);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PoDraft>> GetRecentAsync(
            int take,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<PoDraft>>(
                AddedDrafts
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(take)
                    .ToList());
        }

        public Task<IReadOnlyList<PoDraft>> GetHistoryAsync(
            int take,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<PoDraft>>(
                AddedDrafts
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(take)
                    .ToList());
        }

        public Task<PoDraft?> GetByIdAsync(
            Guid draftId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<PoDraft?>(
                AddedDrafts.SingleOrDefault(x => x.Id == draftId));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFileTextExtractor(string text) :
IFileTextExtractor
    {
        public Task<string> ExtractTextAsync(
            string storagePath,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(text);
        }
    }

    private sealed class FailingFileTextExtractor : IFileTextExtractor
    {
        public Task<string> ExtractTextAsync(
            string storagePath,
            string contentType,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("OCR text file was not found.");
        }
    }

    private sealed class FakePurchaseOrderParser : IPurchaseOrderParser
    {
        public Task<ParsedPurchaseOrder> ParseAsync(
            string text,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ParsedPurchaseOrder(
                "Computer Seller",
                new DateOnly(2026, 5, 31),
                "0016",
                new DateOnly(2026, 6, 30),
                "",
                "Courier",
                "Net 30",
                2615,
                [
                    new ParsedPurchaseOrderLine(
                        5,
                        "MON2000",
                        "1877 Solera Reserva 1.75ml",
                        523,
                        2615)
                ],
                []));
        }
    }

    private sealed class FakeAuditWriter : IAuditWriter
    {
        public List<AuditEvent> Events { get; } = [];

        public Task WriteAsync(
            string action,
            string actor,
            string message,
            CancellationToken cancellationToken)
        {
            Events.Add(AuditEvent.Create(action, actor, message));
            return Task.CompletedTask;
        }
    }
}
