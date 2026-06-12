using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PoOcr.Domain.Drafts;
using PoOcr.Domain.Uploads;
using PoOcr.Infrastructure.Persistence;

namespace PoOcr.Api.Tests;

public sealed class HistoryApiTests
{
    [Fact]
    public async Task GetUploadHistory_WhenUploadsExist_ReturnsActiveAndDeletedUploads()
    {
        await using var factory = new OcrApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OcrDbContext>();
        var activeUpload = CreateUpload("active-po.png");
        var deletedUpload = CreateUpload("deleted-po.png");
        deletedUpload.SoftDelete("admin");
        await dbContext.UploadFiles.AddRangeAsync(activeUpload, deletedUpload);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/history/uploads");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var uploads = await response.Content.ReadFromJsonAsync<List<UploadHistoryResponse>>();
        Assert.NotNull(uploads);
        Assert.Equal(2, uploads.Count);
        Assert.Contains(uploads, upload =>
            upload.Id == activeUpload.Id &&
            upload.IsDeleted == false &&
            upload.DeletedAt is null &&
            upload.DeletedBy is null);
        Assert.Contains(uploads, upload =>
            upload.Id == deletedUpload.Id &&
            upload.IsDeleted &&
            upload.DeletedAt is not null &&
            upload.DeletedBy == "admin");
    }

    [Fact]
    public async Task GetDraftHistory_WhenDraftsExist_ReturnsActiveAndDeletedDrafts()
    {
        await using var factory = new OcrApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OcrDbContext>();
        var activeDraft = CreateDraft("PO-ACTIVE");
        var deletedDraft = CreateDraft("PO-DELETED");
        deletedDraft.SoftDelete("admin");
        await dbContext.PoDrafts.AddRangeAsync(activeDraft, deletedDraft);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/history/drafts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var drafts = await response.Content.ReadFromJsonAsync<List<DraftHistoryResponse>>();
        Assert.NotNull(drafts);
        Assert.Equal(2, drafts.Count);
        Assert.Contains(drafts, draft =>
            draft.Id == activeDraft.Id &&
            draft.IsDeleted == false &&
            draft.DeletedAt is null &&
            draft.DeletedBy is null);
        Assert.Contains(drafts, draft =>
            draft.Id == deletedDraft.Id &&
            draft.IsDeleted &&
            draft.DeletedAt is not null &&
            draft.DeletedBy == "admin");
    }

    private static UploadFile CreateUpload(string fileName)
    {
        return UploadFile.Create(
            fileName,
            "image/png",
            1200,
            $"uploads/{fileName}",
            Guid.NewGuid().ToString("N"),
            "admin");
    }

    private static PoDraft CreateDraft(string referenceNumber)
    {
        return PoDraft.CreateFromExtraction(
            Guid.NewGuid(),
            "ABC Trading",
            new DateOnly(2026, 5, 30),
            referenceNumber,
            new DateOnly(2026, 6, 30),
            "",
            "Courier",
            "Net 30",
            20,
            [
                new PoDraftLine(
                    2,
                    "ITEM-001",
                    "Sample Item",
                    10,
                    20)
            ],
            "system");
    }

    private sealed class OcrApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                var descriptors = services
                    .Where(service =>
                        service.ServiceType == typeof(DbContextOptions<OcrDbContext>)
                        || service.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true)
                    .ToList();

                foreach (var descriptor in descriptors)
                    services.Remove(descriptor);

                services.AddDbContext<OcrDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }

    private sealed record UploadHistoryResponse(
        Guid Id,
        string OriginalFileName,
        string ContentType,
        long SizeBytes,
        string Status,
        string UploadedBy,
        DateTimeOffset UploadedAt,
        string? FailureReason,
        bool IsDeleted,
        DateTimeOffset? DeletedAt,
        string? DeletedBy);

    private sealed record DraftHistoryResponse(
        Guid Id,
        Guid UploadFileId,
        string VendorName,
        DateOnly? PoDate,
        string ReferenceNumber,
        DateOnly? DateExpected,
        string PaymentTerms,
        decimal? TotalAmount,
        int LineCount,
        DateTimeOffset CreatedAt,
        IReadOnlyCollection<string> Warnings,
        bool IsDeleted,
        DateTimeOffset? DeletedAt,
        string? DeletedBy);
}
