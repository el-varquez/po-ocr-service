using Microsoft.EntityFrameworkCore;
using PoOcr.Domain.Audit;
using PoOcr.Domain.Drafts;
using PoOcr.Domain.Extraction;
using PoOcr.Domain.Uploads;

namespace PoOcr.Infrastructure.Persistence;

public sealed class OcrDbContext(DbContextOptions<OcrDbContext> options) : DbContext(options)
{
    public DbSet<UploadFile> UploadFiles => Set<UploadFile>();
    public DbSet<ExtractionJob> ExtractionJobs => Set<ExtractionJob>();
    public DbSet<PoDraft> PoDrafts => Set<PoDraft>();
    public DbSet<PoDraftLine> PoDraftLines => Set<PoDraftLine>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUploadFile(modelBuilder);
        ConfigureExtractionJob(modelBuilder);
        ConfigurePoDraft(modelBuilder);
        ConfigurePoDraftLine(modelBuilder);
        ConfigureAuditEvent(modelBuilder);
    }

    private static void ConfigureUploadFile(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UploadFile>();

        entity.ToTable("UPLOAD_FILE");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasColumnName("ID");
        entity.Property(x => x.OriginalFileName).HasColumnName("ORIGINAL_FILE_NAME").HasMaxLength(255).IsRequired();
        entity.Property(x => x.ContentType).HasColumnName("CONTENT_TYPE").HasMaxLength(255).IsRequired();
        entity.Property(x => x.SizeBytes).HasColumnName("SIZE_BYTES");
        entity.Property(x => x.StorePath).HasColumnName("STORE_PATH").HasMaxLength(512).IsRequired();
        entity.Property(x => x.CheckSum).HasColumnName("CHECKSUM").HasMaxLength(123).IsRequired();
        entity.Property(x => x.UploadedBy).HasColumnName("UPLOADED_BY").HasMaxLength(100).IsRequired();
        entity.Property(x => x.UploadedAt).HasColumnName("UPLOADED_AT");
        entity.Property(x => x.Status).HasColumnName("STATUS").HasConversion<int>();
        entity.Property(x => x.FailureReason).HasColumnName("FAILURE_REASON").HasMaxLength(1000);
        entity.Property(x => x.DeletedAt).HasColumnName("DELETED_AT");
        entity.Property(x => x.DeletedBy).HasColumnName("DELETED_BY").HasMaxLength(100);
        entity.Ignore(x => x.IsDeleted);
        entity.HasIndex(x => x.CheckSum);
        entity.HasIndex(x => x.Status);
        entity.HasIndex(x => x.UploadedAt);
        entity.HasIndex(x => x.DeletedAt);
    }
    private static void ConfigureExtractionJob(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ExtractionJob>();

        entity.ToTable("EXTRACTION_JOB");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasColumnName("ID");
        entity.Property(x => x.UploadFileId).HasColumnName("UPLOAD_FILE_ID");
        entity.Property(x => x.QueuedAt).HasColumnName("QUEUED_AT");
        entity.Property(x => x.StartedAt).HasColumnName("STARTED_AT");
        entity.Property(x => x.CompletedAt).HasColumnName("COMPLETED_AT");
        entity.Property(x => x.FailureReason).HasColumnName("FAILURE_REASON").HasMaxLength(1000);
        entity.HasIndex(x => x.UploadFileId);
        entity.HasIndex(x => x.QueuedAt);
    }

    private static void ConfigurePoDraft(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PoDraft>();

        entity.ToTable("PO_DRAFT");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasColumnName("ID");
        entity.Property(x => x.UploadFileId).HasColumnName("UPLOAD_FILE_ID");
        entity.Property(x => x.VendorName).HasColumnName("VENDOR_NAME").HasMaxLength(255);
        entity.Property(x => x.PoDate).HasColumnName("PO_DATE");
        entity.Property(x => x.ReferenceNumber).HasColumnName("REFERENCE_NUMBER").HasMaxLength(50);
        entity.Property(x => x.DateExpected).HasColumnName("DATE_EXPECTED");
        entity.Property(x => x.ShipTo).HasColumnName("SHIP_TO").HasMaxLength(255);
        entity.Property(x => x.ShipVia).HasColumnName("SHIP_VIA").HasMaxLength(100);
        entity.Property(x => x.PaymentTerms).HasColumnName("PAYMENT_TERMS").HasMaxLength(100);
        entity.Property(x => x.TotalAmount).HasColumnName("TOTAL_AMOUNT").HasPrecision(16, 4);
        entity.Property(x => x.CreatedBy).HasColumnName("CREATED_BY").HasMaxLength(100);
        entity.Property(x => x.CreatedAt).HasColumnName("CREATED_AT");
        entity.Property(x => x.UpdatedBy).HasColumnName("UPDATED_BY").HasMaxLength(100).IsRequired(false);
        entity.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT");
        entity.HasMany(x => x.Lines).WithOne().HasForeignKey("PO_DRAFT_ID").OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Ignore(x => x.Warnings);
        entity.HasIndex(x => x.UploadFileId);
        entity.HasIndex(x => x.ReferenceNumber);
        entity.HasIndex(x => x.PoDate);
    }

    private static void ConfigurePoDraftLine(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PoDraftLine>();

        entity.ToTable("PO_DRAFT_LINE");
        entity.Property<Guid>("Id").HasColumnName("ID").ValueGeneratedOnAdd();
        entity.HasKey("Id");
        entity.Property<decimal>("Quantity").HasColumnName("QUANTITY").HasPrecision(16, 6);
        entity.Property<string>("ItemCode").HasColumnName("ITEM_CODE").HasMaxLength(50);
        entity.Property<string>("Description").HasColumnName("DESCRIPTION").HasMaxLength(255);
        entity.Property<decimal>("UnitPrice").HasColumnName("UNIT_PRICE").HasPrecision(16, 4);
        entity.Property<decimal>("Amount").HasColumnName("AMOUNT").HasPrecision(16, 4);
    }

    private static void ConfigureAuditEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditEvent>();
        entity.ToTable("AUDIT_EVENT");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasColumnName("ID");
        entity.Property(x => x.Action).HasColumnName("ACTION").HasMaxLength(100).IsRequired();
        entity.Property(x => x.Actor).HasColumnName("ACTOR").HasMaxLength(100).IsRequired();
        entity.Property(x => x.Message).HasColumnName("MESSAGE").HasMaxLength(1000).IsRequired();
        entity.Property(x => x.OccurredAt).HasColumnName("OCCURRED_AT");
        entity.HasIndex(x => x.OccurredAt);
        entity.HasIndex(x => x.Action);
    }
}
