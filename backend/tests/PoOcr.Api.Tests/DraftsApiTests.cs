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
        Assert.Equal("PO-1001", returnedDraft.PoNumber);
        Assert.Equal("ABC Trading", returnedDraft.CustomerName);
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
        Assert.Equal("PO-1002", returnedDraft.PoNumber);
        var line = Assert.Single(returnedDraft.Lines);
        Assert.Equal("ITEM-001", line.ItemCode);
        Assert.Equal(2, line.Quantity);
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
                "PO-UPDATED",
                new DateOnly(2026, 6, 1),
                "Updated Customer",
                [
                    new DraftLineUpdateRequest(
                        "ITEM-999",
                        "Updated Item",
                        5,
                        "BOX",
                        12,
                        60)
                ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        dbContext.ChangeTracker.Clear();
        var savedDraft = await dbContext.PoDrafts
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == draft.Id);
        Assert.Equal("PO-UPDATED", savedDraft.PoNumber);
        Assert.Equal(new DateOnly(2026, 6, 1), savedDraft.PoDate);
        Assert.Equal("Updated Customer", savedDraft.CustomerName);
        Assert.Equal("test-user", savedDraft.UpdatedBy);
        var savedLine = Assert.Single(savedDraft.Lines);
        Assert.Equal("ITEM-999", savedLine.ItemCode);
        Assert.Equal(5, savedLine.Quantity);
    }

    [Fact]
    public async Task PutDraftById_WhenDraftDoesNotExist_ReturnsNotFound()
    {
        await using var factory = new OcrApiFactory();
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/drafts/{Guid.NewGuid()}",
            new DraftUpdateRequest(
                "PO-UPDATED",
                new DateOnly(2026, 6, 1),
                "Updated Customer",
                []));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static PoDraft CreateDraft(string poNumber)
    {
        return PoDraft.CreateFromExtraction(
            Guid.NewGuid(),
            poNumber,
            new DateOnly(2026, 5, 30),
            "ABC Trading",
            [
                new PoDraftLine(
                    "ITEM-001",
                    "Sample Item",
                    2,
                    "PCS",
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
        string PoNumber,
        DateOnly? PoDate,
        string CustomerName,
        int LineCount,
        DateTimeOffset CreatedAt,
        IReadOnlyCollection<string> Warnings);

    private sealed record DraftDetailResponse(
        Guid Id,
        Guid UploadFileId,
        string PoNumber,
        DateOnly? PoDate,
        string CustomerName,
        DateTimeOffset CreatedAt,
        IReadOnlyCollection<string> Warnings,
        IReadOnlyCollection<DraftLineResponse> Lines);

    private sealed record DraftLineResponse(
        string ItemCode,
        string Description,
        decimal Quantity,
        string Unit,
        decimal UnitPrice,
        decimal LineTotal);

    private sealed record DraftUpdateRequest(
        string? PoNumber,
        DateOnly? PoDate,
        string? CustomerName,
        IReadOnlyCollection<DraftLineUpdateRequest> Lines);

    private sealed record DraftLineUpdateRequest(
        string ItemCode,
        string Description,
        decimal Quantity,
        string Unit,
        decimal UnitPrice,
        decimal LineTotal);
}
