using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PoOcr.Domain.Uploads;
using PoOcr.Infrastructure.Persistence;

namespace PoOcr.Api.Tests;

public sealed class UploadsApiTests
{
    [Fact]
    public async Task GetUploads_WhenUploadExists_ReturnsRecentUploads()
    {
        await using var factory = new OcrApiFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OcrDbContext>();
        var upload = UploadFile.Create(
            "sample-po.png",
            "image/png",
            1200,
            "uploads/sample-po.png",
            "abc123",
            "admin");
        await dbContext.UploadFiles.AddAsync(upload);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/uploads");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var uploads = await response.Content.ReadFromJsonAsync<List<UploadResponse>>();
        Assert.NotNull(uploads);
        var returnedUpload = Assert.Single(uploads);
        Assert.Equal(upload.Id, returnedUpload.Id);
        Assert.Equal("sample-po.png", returnedUpload.OriginalFileName);
        Assert.Equal("PendingExtraction", returnedUpload.Status);
    }

    private sealed class OcrApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
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

    private sealed record UploadResponse(
        Guid Id,
        string OriginalFileName,
        string ContentType,
        long SizeBytes,
        string Status,
        string UploadedBy,
        DateTimeOffset UploadedAt,
        string? FailureReason);
}
