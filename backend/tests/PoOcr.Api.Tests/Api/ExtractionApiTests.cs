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

public sealed class ExtractionApiTests
{
    [Fact]
    public async Task QueueExtraction_WhenUploadExists_QueuesUploadAndCreatesJob()
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

        var response = await client.PostAsJsonAsync(
            "/api/extraction/queue",
            new QueueExtractionRequest([upload.Id]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        dbContext.ChangeTracker.Clear();
        var savedUpload = await dbContext.UploadFiles.SingleAsync();
        Assert.Equal(UploadStatus.QueuedForExtraction, savedUpload.Status);
        var job = await dbContext.ExtractionJobs.SingleAsync();
        Assert.Equal(upload.Id, job.UploadFileId);
        var audit = await dbContext.AuditEvents.SingleAsync();
        Assert.Equal("extraction.queued", audit.Action);
    }

    [Fact]
    public async Task QueueExtraction_WhenUploadIsAlreadyQueued_ReturnsBadRequest()
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
        upload.QueueForExtraction();
        await dbContext.UploadFiles.AddAsync(upload);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/extraction/queue",
            new QueueExtractionRequest([upload.Id]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var message = await response.Content.ReadAsStringAsync();
        Assert.Contains("Upload cannot be queued", message);
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

    private sealed record QueueExtractionRequest(IReadOnlyCollection<Guid> UploadIds);
}
