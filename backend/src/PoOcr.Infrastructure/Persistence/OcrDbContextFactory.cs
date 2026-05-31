using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PoOcr.Infrastructure.Configuration;

namespace PoOcr.Infrastructure.Persistence;

public sealed class OcrDbContextFactory : IDesignTimeDbContextFactory<OcrDbContext>
{
    public OcrDbContext CreateDbContext(string[] args)
    {
        var connectionString = OcrDatabaseConnectionString.Resolve(args.FirstOrDefault())
            ?? throw new InvalidOperationException("Database connection is required. Set PO_OCR_CONNECTION_STRING or PO_OCR_DB_* values in .env.");

        var options = new DbContextOptionsBuilder<OcrDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 31)))
            .Options;

        return new OcrDbContext(options);
    }
}
