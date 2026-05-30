using Microsoft.EntityFrameworkCore;
using PoOcr.Application.Abstractions;
using PoOcr.Application.Extraction;
using PoOcr.Domain.Uploads;
using PoOcr.Infrastructure.Parsing;
using PoOcr.Infrastructure.Persistence;

namespace PoOcr.Infrastructure.Tests.Workflow;

public sealed class ExtractionWorkflowTests
{
    [Fact]
    public async Task UploadQueueAndProcess_WhenOcrTextIsParsed_CreatesDraftForReview()
    {
        await using var dbContext = CreateDbContext();
        var uploadRepository = new UploadRepository(dbContext);
        var extractionJobRepository = new ExtractionJobRepository(dbContext);
        var draftRepository = new DraftRepository(dbContext);
        var auditWriter = new AuditWriter(dbContext);

        var upload = UploadFile.Create(
            "sample-po.png",
            "image/png",
            1200,
            "uploads/sample-po.png",
            "abc123",
            "admin");

        await uploadRepository.AddAsync(upload, CancellationToken.None);
        await uploadRepository.SaveChangesAsync(CancellationToken.None);

        var queueUseCase = new QueueExtractionUseCase(
            uploadRepository,
            extractionJobRepository,
            auditWriter);

        var queueResult = await queueUseCase.Handle(
            new QueueExtractionCommand([upload.Id], "admin"),
            CancellationToken.None);

        var processUseCase = new ProcessNextExtractionJobUseCase(
            extractionJobRepository,
            uploadRepository,
            draftRepository,
            new FakeFileTextExtractor(),
            new RuleBasedPurchaseOrderParser(),
            auditWriter);

        var processResult = await processUseCase.Handle(CancellationToken.None);

        Assert.True(queueResult.IsSuccess);
        Assert.True(processResult.IsSuccess);
        Assert.True(processResult.ProcessedJob);

        dbContext.ChangeTracker.Clear();
        var savedUpload = await dbContext.UploadFiles.SingleAsync();
        Assert.Equal(UploadStatus.NeedsReview, savedUpload.Status);

        var draft = await dbContext.PoDrafts
            .Include(x => x.Lines)
            .SingleAsync();
        Assert.Equal(upload.Id, draft.UploadFileId);
        Assert.Equal("PO-2001", draft.PoNumber);
        Assert.Equal(new DateOnly(2026, 5, 30), draft.PoDate);
        Assert.Equal("ABC Trading", draft.CustomerName);
        var line = Assert.Single(draft.Lines);
        Assert.Equal("ITEM-001", line.ItemCode);
        Assert.Equal(2, line.Quantity);

        var job = await dbContext.ExtractionJobs.SingleAsync();
        Assert.True(job.IsCompleted);
        Assert.Contains(dbContext.AuditEvents, x => x.Action == "extraction.queued");
        Assert.Contains(dbContext.AuditEvents, x => x.Action == "extraction.completed");
    }

    private static OcrDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OcrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OcrDbContext(options);
    }

    private sealed class FakeFileTextExtractor : IFileTextExtractor
    {
        public Task<string> ExtractTextAsync(
            string storagePath,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                """
                PO No: PO-2001
                Date: 05/30/2026
                Customer: ABC Trading
                ITEM-001 Sample Item 2 PCS 10 20
                """);
        }
    }
}
