using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PoOcr.Infrastructure.Persistence;

public sealed class OcrDbContextFactory : IDesignTimeDbContextFactory<OcrDbContext>
{
    public OcrDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.FirstOrDefault()
            ?? Environment.GetEnvironmentVariable("PO_OCR_CONNECTION_STRING")
            ?? "server=localhost;database=po_ocr_service;user=po_ocr_user;password=change-me";

        var options = new DbContextOptionsBuilder<OcrDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 31)))
            .Options;

        return new OcrDbContext(options);
    }
}
