  using Microsoft.EntityFrameworkCore;
  using PoOcr.Domain.Uploads;
  using PoOcr.Infrastructure.Persistence;

  namespace PoOcr.Infrastructure.Tests.Persistence;

  public sealed class OcrDbContextTests
  {
      [Fact]
      public async Task SaveChanges_WhenUploadFileIsAdded_PersistsUpload()
      {
          var options = new DbContextOptionsBuilder<OcrDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

          await using var dbContext = new OcrDbContext(options);

          var upload = UploadFile.Create(
              "sample-po.pdf",
              "application/pdf",
              1250,
              "uploads/sample-po.pdf",
              "abc123",
              "admin");

          await dbContext.UploadFiles.AddAsync(upload);
          await dbContext.SaveChangesAsync();

          var saved = await dbContext.UploadFiles.SingleAsync();

          Assert.Equal(upload.Id, saved.Id);
          Assert.Equal("sample-po.pdf", saved.OriginalFileName);
          Assert.Equal(UploadStatus.PendingExtraction, saved.Status);
      }
  }