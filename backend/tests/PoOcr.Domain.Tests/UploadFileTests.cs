  using PoOcr.Domain.Uploads;

  namespace PoOcr.Domain.Tests;

  public sealed class UploadFileTests
  {
      [Fact]
      public void QueueForExtraction_WhenPending_MarksUploadQueued()
      {
          var upload = UploadFile.Create(
              "sample-po.pdf",
              "application/pdf",
              1250,
              "uploads/sample-po.pdf",
              "abc123",
              "admin");

          upload.QueueForExtraction();

          Assert.Equal(UploadStatus.QueuedForExtraction, upload.Status);
      }

      [Fact]
      public void QueueForExtraction_WhenAlreadyExtracting_Throws()
      {
          var upload = UploadFile.Create(
              "sample-po.pdf",
              "application/pdf",
              1250,
              "uploads/sample-po.pdf",
              "abc123",
              "admin");

          upload.QueueForExtraction();
          upload.MarkExtracting();

          Assert.Throws<InvalidOperationException>(() => upload.QueueForExtraction());
      }

      [Fact]
      public void MarkFailed_StoresFailureReason()
      {
          var upload = UploadFile.Create(
              "sample-po.pdf",
              "application/pdf",
              1250,
              "uploads/sample-po.pdf",
              "abc123",
              "admin");

          upload.MarkFailed("OCR text file was not found.");

          Assert.Equal(UploadStatus.Failed, upload.Status);
          Assert.Equal("OCR text file was not found.", upload.FailureReason);
      }
  }