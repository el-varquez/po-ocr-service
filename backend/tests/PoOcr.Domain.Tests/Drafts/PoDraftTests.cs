  using PoOcr.Domain.Drafts;

  namespace PoOcr.Domain.Tests;

  public sealed class PoDraftTests
  {
      [Fact]
      public void CreateFromExtraction_WhenRequiredFieldsAreMissing_AddsWarnings()
      {
          var draft = PoDraft.CreateFromExtraction(
              uploadFileId: Guid.NewGuid(),
              poNumber: "",
              poDate: null,
              customerName: "",
              lines: [],
              createdBy: "admin");

          Assert.Contains("PO number is missing.", draft.Warnings);
          Assert.Contains("PO date is missing.", draft.Warnings);
          Assert.Contains("Customer name is missing.", draft.Warnings);
          Assert.Contains("No PO lines were extracted.", draft.Warnings);
      }

      [Fact]
      public void SaveChanges_AllowsIncompleteDraft()
      {
          var draft = PoDraft.CreateFromExtraction(
              uploadFileId: Guid.NewGuid(),
              poNumber: "",
              poDate: null,
              customerName: "",
              lines: [],
              createdBy: "admin");

          draft.SaveChanges(
              poNumber: "PO-1001",
              poDate: null,
              customerName: "",
              lines: [],
              changedBy: "reviewer");

          Assert.Equal("PO-1001", draft.PoNumber);
          Assert.Equal("reviewer", draft.UpdatedBy);
          Assert.NotNull(draft.UpdatedAt);
      }
  }