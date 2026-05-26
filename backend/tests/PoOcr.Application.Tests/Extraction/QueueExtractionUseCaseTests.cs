  using PoOcr.Application.Abstractions;
  using PoOcr.Application.Extraction;
  using PoOcr.Domain.Audit;
  using PoOcr.Domain.Extraction;
  using PoOcr.Domain.Uploads;

  namespace PoOcr.Application.Tests;

  public sealed class QueueExtractionUseCaseTests
  {
      [Fact]
      public async Task
  Handle_WhenUploadIsPending_QueuesUploadAndCreatesExtractionJob()
      {
          var upload = UploadFile.Create(
              "sample-po.pdf",
              "application/pdf",
              1250,
              "uploads/sample-po.pdf",
              "abc123",
              "admin");

          var uploadRepository = new FakeUploadRepository([upload]);
          var extractionJobRepository = new FakeExtractionJobRepository();
          var auditWriter = new FakeAuditWriter();

          var useCase = new QueueExtractionUseCase(
              uploadRepository,
              extractionJobRepository,
              auditWriter);

          var result = await useCase.Handle(
              new QueueExtractionCommand([upload.Id], "reviewer"),
              CancellationToken.None);

          Assert.True(result.IsSuccess);
          Assert.Equal(UploadStatus.QueuedForExtraction, upload.Status);
          Assert.Single(extractionJobRepository.AddedJobs);
          Assert.Equal(upload.Id,
  extractionJobRepository.AddedJobs[0].UploadFileId);
          Assert.Contains(auditWriter.Events, x => x.Action ==
  "extraction.queued");
      }

      [Fact]
      public async Task Handle_WhenUploadIsMissing_ReturnsFailure()
      {
          var useCase = new QueueExtractionUseCase(
              new FakeUploadRepository([]),
              new FakeExtractionJobRepository(),
              new FakeAuditWriter());

          var result = await useCase.Handle(
              new QueueExtractionCommand([Guid.NewGuid()], "reviewer"),
              CancellationToken.None);

          Assert.False(result.IsSuccess);
          Assert.Contains("not found", result.Error,
          StringComparison.OrdinalIgnoreCase);
      }

      [Fact]
      public async Task Handle_WhenNoUploadIdsAreProvided_ReturnsFailure()
      {
          var useCase = new QueueExtractionUseCase(
              new FakeUploadRepository([]),
              new FakeExtractionJobRepository(),
              new FakeAuditWriter());

          var result = await useCase.Handle(
              new QueueExtractionCommand([], "reviewer"),
              CancellationToken.None);

          Assert.False(result.IsSuccess);
          Assert.Contains("upload", result.Error,
  StringComparison.OrdinalIgnoreCase);
      }

      [Fact]
      public async Task Handle_WhenActorIsMissing_ReturnsFailure()
      {
          var upload = UploadFile.Create(
              "sample-po.pdf",
              "application/pdf",
              1250,
              "uploads/sample-po.pdf",
              "abc123",
              "admin");

          var useCase = new QueueExtractionUseCase(
              new FakeUploadRepository([upload]),
              new FakeExtractionJobRepository(),
              new FakeAuditWriter());

          var result = await useCase.Handle(
              new QueueExtractionCommand([upload.Id], ""),
              CancellationToken.None);

          Assert.False(result.IsSuccess);
          Assert.Contains("actor", result.Error,
  StringComparison.OrdinalIgnoreCase);
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

      private sealed class FakeExtractionJobRepository :
  IExtractionJobRepository
      {
          public List<ExtractionJob> AddedJobs { get; } = [];

          public Task AddAsync(
              ExtractionJob job,
              CancellationToken cancellationToken)
          {
              AddedJobs.Add(job);
              return Task.CompletedTask;
          }

          public Task<ExtractionJob?> GetNextQueuedAsync(
              CancellationToken cancellationToken)
          {
              return Task.FromResult<ExtractionJob?>(
                  AddedJobs.FirstOrDefault(x => x.StartedAt is null));
          }

          public Task SaveChangesAsync(CancellationToken cancellationToken)
          {
              return Task.CompletedTask;
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