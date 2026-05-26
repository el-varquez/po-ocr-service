  using Microsoft.EntityFrameworkCore;
  using PoOcr.Domain.Extraction;
  using PoOcr.Domain.Uploads;
  using PoOcr.Infrastructure.Persistence;

  namespace PoOcr.Infrastructure.Tests.Persistence;

  public sealed class RepositoryTests
  {
      [Fact]
      public async Task UploadRepository_GetByIdAsync_ReturnsMatchingUploads()
      {
          await using var dbContext = CreateDbContext();

          var upload = UploadFile.Create(
              "sample-po.pdf",
              "application/pdf",
              1250,
              "uploads/sample-po.pdf",
              "abc123",
              "admin");

          await dbContext.UploadFiles.AddAsync(upload);
          await dbContext.SaveChangesAsync();

          var repository = new UploadRepository(dbContext);

          var results = await repository.GetByIdAsync(
              [upload.Id],
              CancellationToken.None);

          Assert.Single(results);
          Assert.Equal(upload.Id, results[0].Id);
      }

      [Fact]
      public async Task
  ExtractionJobRepository_GetNextQueuedAsync_ReturnsOldestUnstartedJob()
      {
          await using var dbContext = CreateDbContext();

          var firstUpload = UploadFile.Create(
              "first-po.pdf",
              "application/pdf",
              100,
              "uploads/first-po.pdf",
              "abc1",
              "admin");

          var secondUpload = UploadFile.Create(
              "second-po.pdf",
              "application/pdf",
              100,
              "uploads/second-po.pdf",
              "abc2",
              "admin");

          firstUpload.QueueForExtraction();
          secondUpload.QueueForExtraction();

          var firstJob = ExtractionJob.Create(firstUpload.Id);
          var secondJob = ExtractionJob.Create(secondUpload.Id);

          await dbContext.UploadFiles.AddRangeAsync(firstUpload, secondUpload);
          await dbContext.ExtractionJobs.AddRangeAsync(secondJob, firstJob);
          await dbContext.SaveChangesAsync();

          var repository = new ExtractionJobRepository(dbContext);

          var nextJob = await repository.GetNextQueuedAsync(CancellationToken.None);

          Assert.NotNull(nextJob);
          Assert.Equal(firstJob.Id, nextJob.Id);
      }

      private static OcrDbContext CreateDbContext()
      {
          var options = new DbContextOptionsBuilder<OcrDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

          return new OcrDbContext(options);
      }
  }