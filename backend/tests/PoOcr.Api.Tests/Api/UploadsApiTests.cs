using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PoOcr.Application.Abstractions;
using PoOcr.Domain.Uploads;
using PoOcr.Infrastructure.Persistence;

namespace PoOcr.Api.Tests;

public sealed class UploadsApiTests
{
    [Fact]
    public async Task PostUploads_WhenFileIsValid_StoresFileMetadata()
    {
        await using var factory = new OcrApiFactory();
        var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("sample po image"));
        fileContent.Headers.ContentType = new("image/png");
        form.Add(fileContent, "files", "sample-po.png");

        var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OcrDbContext>();
        var upload = Assert.Single(dbContext.UploadFiles);
        Assert.Equal("sample-po.png", upload.OriginalFileName);
        Assert.Equal("image/png", upload.ContentType);
        Assert.Equal(UploadStatus.PendingExtraction, upload.Status);
        Assert.Equal("test-user", upload.UploadedBy);
    }

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

    [Fact]
    public async Task DeleteUpload_WhenUploadExists_SoftDeletesUpload()
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

        var response = await client.DeleteAsync($"/api/uploads/{upload.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var savedUpload = await dbContext.UploadFiles.SingleAsync(x => x.Id == upload.Id);
        await dbContext.Entry(savedUpload).ReloadAsync();
        Assert.True(savedUpload.IsDeleted);
        Assert.NotNull(savedUpload.DeletedAt);
        Assert.Equal("test-user", savedUpload.DeletedBy);
    }

    [Fact]
    public async Task GetUploads_WhenUploadIsDeleted_DoesNotReturnDeletedUpload()
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
        upload.SoftDelete("admin");
        await dbContext.UploadFiles.AddAsync(upload);
        await dbContext.SaveChangesAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/uploads");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var uploads = await response.Content.ReadFromJsonAsync<List<UploadResponse>>();
        Assert.NotNull(uploads);
        Assert.Empty(uploads);
    }

    private sealed class OcrApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OcrDatabase"] = "server=localhost;database=test;user=test;password=test"
                });
            });

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

                services.AddScoped<IFileStorage, FakeFileStorage>();
            });
        }
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public async Task<StoredFileResult> SaveAsync(
            FileStorageRequest request,
            CancellationToken cancellationToken)
        {
            using var memoryStream = new MemoryStream();
            await request.Content.CopyToAsync(memoryStream, cancellationToken);

            return new StoredFileResult(
                request.OriginalFileName,
                request.ContentType,
                memoryStream.Length,
                $"test-storage/{request.OriginalFileName}",
                "test-checksum",
                request.UploadedBy);
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
