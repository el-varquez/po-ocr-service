using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PoOcr.Domain.Drafts;
using PoOcr.Infrastructure.Persistence;

namespace PoOcr.Api.Tests;

public sealed class DraftsApiTests
{
    [Fact]
    public async Task GetDrafts_WhenDraftExists_ReturnsRecentDrafts()
    {
        await using var factory = new OcrApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OcrDbContext>();
        var draft = CreateDraft("PO-1001");
        await dbContext.PoDrafts.AddAsync(draft);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/drafts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var drafts = await response.Content.ReadFromJsonAsync<List<DraftListResponse>>();
        Assert.NotNull(drafts);
        var returnedDraft = Assert.Single(drafts);
        Assert.Equal(draft.Id, returnedDraft.Id);
        Assert.Equal("PO-1001", returnedDraft.ReferenceNumber);
        Assert.Equal("ABC Trading", returnedDraft.VendorName);
        Assert.Equal(new DateOnly(2026, 6, 30), returnedDraft.DateExpected);
        Assert.Equal("Net 30", returnedDraft.PaymentTerms);
        Assert.Equal(20, returnedDraft.TotalAmount);
        Assert.Equal(1, returnedDraft.LineCount);
    }

    [Fact]
    public async Task GetDraftById_WhenDraftExists_ReturnsDraftWithLines()
    {
        await using var factory = new OcrApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OcrDbContext>();
        var draft = CreateDraft("PO-1002");
        await dbContext.PoDrafts.AddAsync(draft);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/drafts/{draft.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedDraft = await response.Content.ReadFromJsonAsync<DraftDetailResponse>();
        Assert.NotNull(returnedDraft);
        Assert.Equal(draft.Id, returnedDraft.Id);
        Assert.Equal("PO-1002", returnedDraft.ReferenceNumber);
        Assert.Equal("ABC Trading", returnedDraft.VendorName);
        Assert.Equal("Courier", returnedDraft.ShipVia);
        Assert.Equal("Net 30", returnedDraft.PaymentTerms);
        var line = Assert.Single(returnedDraft.Lines);
        Assert.Equal("ITEM-001", line.ItemCode);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(10, line.UnitPrice);
        Assert.Equal(20, line.Amount);
        Assert.Empty(returnedDraft.Warnings);
    }

    [Fact]
    public async Task GetDraftById_WhenDraftDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new OcrApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/drafts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDrafts_WhenDraftIsDeleted_DoesNotReturnDeletedDraft()
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

        var response = await client.GetAsync("/api/drafts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var drafts = await response.Content.ReadFromJsonAsync<List<DraftListResponse>>();
        Assert.NotNull(drafts);
        var returnedDraft = Assert.Single(drafts);
        Assert.Equal(activeDraft.Id, returnedDraft.Id);
    }

    [Fact]
    public async Task GetDraftById_WhenDraftIsDeleted_ReturnsNotFound()
    {
        await using var factory = new OcrApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OcrDbContext>();
        var draft = CreateDraft("PO-DELETED");
        draft.SoftDelete("admin");
        await dbContext.PoDrafts.AddAsync(draft);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/drafts/{draft.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutDraftById_WhenDraftExists_UpdatesDraftFieldsAndLines()
    {
        await using var factory = new OcrApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OcrDbContext>();
        var draft = CreateDraft("PO-1003");
        await dbContext.PoDrafts.AddAsync(draft);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/drafts/{draft.Id}",
            new DraftUpdateRequest(
                "Updated Vendor",
                new DateOnly(2026, 6, 1),
                "PO-UPDATED",
                new DateOnly(2026, 6, 30),
                "",
                "Courier",
                "Net 30",
                60,
                [
                    new DraftLineUpdateRequest(
                        5,
                        "ITEM-999",
                        "Updated Item",
                        12,
                        60)
                ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        dbContext.ChangeTracker.Clear();
        var savedDraft = await dbContext.PoDrafts
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == draft.Id);
        Assert.Equal("PO-UPDATED", savedDraft.ReferenceNumber);
        Assert.Equal(new DateOnly(2026, 6, 1), savedDraft.PoDate);
        Assert.Equal("Updated Vendor", savedDraft.VendorName);
        Assert.Equal(new DateOnly(2026, 6, 30), savedDraft.DateExpected);
        Assert.Equal("Courier", savedDraft.ShipVia);
        Assert.Equal("Net 30", savedDraft.PaymentTerms);
        Assert.Equal(60, savedDraft.TotalAmount);
        Assert.Equal("test-user", savedDraft.UpdatedBy);
        var savedLine = Assert.Single(savedDraft.Lines);
        Assert.Equal("ITEM-999", savedLine.ItemCode);
        Assert.Equal(5, savedLine.Quantity);
        Assert.Equal(12, savedLine.UnitPrice);
        Assert.Equal(60, savedLine.Amount);
    }

    [Fact]
    public async Task PutDraftById_WhenDraftDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new OcrApiFactory();
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/drafts/{Guid.NewGuid()}",
            new DraftUpdateRequest(
                "Updated Vendor",
                new DateOnly(2026, 6, 1),
                "PO-UPDATED",
                new DateOnly(2026, 6, 30),
                "",
                "Courier",
                "Net 30",
                60,
                []));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDraft_WhenDraftExists_SoftDeletesDraft()
    {
        await using var factory = new OcrApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OcrDbContext>();
        var draft = CreateDraft("PO-DELETE");
        await dbContext.PoDrafts.AddAsync(draft);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/drafts/{draft.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        dbContext.ChangeTracker.Clear();
        var savedDraft = await dbContext.PoDrafts.SingleAsync(x => x.Id == draft.Id);
        Assert.True(savedDraft.IsDeleted);
        Assert.NotNull(savedDraft.DeletedAt);
        Assert.Equal("test-user", savedDraft.DeletedBy);
    }

    [Fact]
    public async Task DeleteDraft_WhenDraftDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new OcrApiFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/drafts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private sealed record DraftListResponse(
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
        IReadOnlyCollection<string> Warnings);
    private sealed record DraftDetailResponse(
        Guid Id,
        Guid UploadFileId,
        string VendorName,
        DateOnly? PoDate,
        string ReferenceNumber,
        DateOnly? DateExpected,
        string ShipTo,
        string ShipVia,
        string PaymentTerms,
        decimal? TotalAmount,
        DateTimeOffset CreatedAt,
        IReadOnlyCollection<string> Warnings,
        IReadOnlyCollection<DraftLineResponse> Lines);

    private sealed record DraftLineResponse(
        decimal Quantity,
        string ItemCode,
        string Description,
        decimal UnitPrice,
        decimal Amount);

    private sealed record DraftUpdateRequest(
        string? VendorName,
        DateOnly? PoDate,
        string? ReferenceNumber,
        DateOnly? DateExpected,
        string? ShipTo,
        string? ShipVia,
        string? PaymentTerms,
        decimal? TotalAmount,
        IReadOnlyCollection<DraftLineUpdateRequest> Lines);

    private sealed record DraftLineUpdateRequest(
        decimal Quantity,
        string ItemCode,
        string Description,
        decimal UnitPrice,
        decimal Amount);
}
