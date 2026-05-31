  using PoOcr.Domain.Drafts;

  namespace PoOcr.Domain.Tests.Drafts;

  public sealed class PoDraftTests
  {
      [Fact]
      public void
      CreateFromExtraction_WhenFieldsAreComplete_CreatesDraftWithoutWarnings()
      {
          var draft = PoDraft.CreateFromExtraction(
              Guid.NewGuid(),
              "Computer Seller",
              new DateOnly(2026, 5, 31),
              "0016",
              new DateOnly(2026, 6, 30),
              "",
              "Courier",
              "Net 30",
              2615,
              [
                  new PoDraftLine(
                      5,
                      "MON2000",
                      "1877 Solera Reserva 1.75ml",
                      523,
                      2615)
              ],
              "system");

          Assert.Equal("Computer Seller", draft.VendorName);
          Assert.Equal(new DateOnly(2026, 5, 31), draft.PoDate);
          Assert.Equal("0016", draft.ReferenceNumber);
          Assert.Equal(new DateOnly(2026, 6, 30), draft.DateExpected);
          Assert.Equal("Courier", draft.ShipVia);
          Assert.Equal("Net 30", draft.PaymentTerms);
          Assert.Equal(2615, draft.TotalAmount);
          Assert.Empty(draft.Warnings);

          var line = Assert.Single(draft.Lines);
          Assert.Equal(5, line.Quantity);
          Assert.Equal("MON2000", line.ItemCode);
          Assert.Equal("1877 Solera Reserva 1.75ml", line.Description);
          Assert.Equal(523, line.UnitPrice);
          Assert.Equal(2615, line.Amount);
      }

      [Fact]
      public void CreateFromExtraction_WhenRequiredFieldsAreMissing_AddsWarnings()
      {
          var draft = PoDraft.CreateFromExtraction(
              Guid.NewGuid(),
              "",
              null,
              "",
              null,
              "",
              "",
              "",
              null,
              [],
              "system");

          Assert.Contains("Vendor name is missing.", draft.Warnings);
          Assert.Contains("PO date is missing.", draft.Warnings);
          Assert.Contains("Reference number is missing.", draft.Warnings);
          Assert.Contains("Date expected is missing.", draft.Warnings);
          Assert.Contains("Payment terms is missing.", draft.Warnings);
          Assert.Contains("Total amount is missing.", draft.Warnings);
          Assert.Contains("No PO lines were extracted.", draft.Warnings);
      }

      [Fact]
      public void SaveChanges_WhenDraftIsEdited_UpdatesFieldsAndWarnings()
      {
          var draft = PoDraft.CreateFromExtraction(
              Guid.NewGuid(),
              "",
              null,
              "",
              null,
              "",
              "",
              "",
              null,
              [],
              "system");

          draft.SaveChanges(
              "Computer Seller",
              new DateOnly(2026, 5, 31),
              "0016",
              new DateOnly(2026, 6, 30),
              "",
              "Courier",
              "Net 30",
              2615,
              [
                  new PoDraftLine(
                      5,
                      "MON2000",
                      "1877 Solera Reserva 1.75ml",
                      523,
                      2615)
              ],
              "admin");

          Assert.Equal("Computer Seller", draft.VendorName);
          Assert.Equal("admin", draft.UpdatedBy);
          Assert.NotNull(draft.UpdatedAt);
          Assert.Empty(draft.Warnings);
      }

      [Fact]
      public void SoftDelete_WhenDraftIsActive_MarksDraftDeleted()
      {
          var draft = PoDraft.CreateFromExtraction(
              Guid.NewGuid(),
              "Computer Seller",
              new DateOnly(2026, 5, 31),
              "0016",
              new DateOnly(2026, 6, 30),
              "",
              "Courier",
              "Net 30",
              2615,
              [
                  new PoDraftLine(
                      5,
                      "MON2000",
                      "1877 Solera Reserva 1.75ml",
                      523,
                      2615)
              ],
              "system");

          draft.SoftDelete("admin");

          Assert.True(draft.IsDeleted);
          Assert.NotNull(draft.DeletedAt);
          Assert.Equal("admin", draft.DeletedBy);
      }
  }
