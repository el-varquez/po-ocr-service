using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoOcr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AUDIT_EVENT",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ACTION = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ACTOR = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MESSAGE = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OCCURRED_AT = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_EVENT", x => x.ID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EXTRACTION_JOB",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UPLOAD_FILE_ID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QUEUED_AT = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    STARTED_AT = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    COMPLETED_AT = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    FAILURE_REASON = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EXTRACTION_JOB", x => x.ID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PO_DRAFT",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UPLOAD_FILE_ID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PO_NUMBER = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PO_DATE = table.Column<DateOnly>(type: "date", nullable: true),
                    CUSTOMER_NAME = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CREATED_BY = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CREATED_AT = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UPDATED_BY = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UPDATED_AT = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PO_DRAFT", x => x.ID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UPLOAD_FILE",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ORIGINAL_FILE_NAME = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CONTENT_TYPE = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SIZE_BYTES = table.Column<long>(type: "bigint", nullable: false),
                    STORE_PATH = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CHECKSUM = table.Column<string>(type: "varchar(123)", maxLength: 123, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UPLOADED_BY = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UPLOADED_AT = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    STATUS = table.Column<int>(type: "int", nullable: false),
                    FAILURE_REASON = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPLOAD_FILE", x => x.ID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PO_DRAFT_LINE",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ITEM_CODE = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DESCRIPTION = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QUANTITY = table.Column<decimal>(type: "decimal(16,6)", precision: 16, scale: 6, nullable: false),
                    UNIT = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UNIT_PRICE = table.Column<decimal>(type: "decimal(16,4)", precision: 16, scale: 4, nullable: false),
                    LINE_TOTAL = table.Column<decimal>(type: "decimal(16,4)", precision: 16, scale: 4, nullable: false),
                    PO_DRAFT_ID = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PO_DRAFT_LINE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PO_DRAFT_LINE_PO_DRAFT_PO_DRAFT_ID",
                        column: x => x.PO_DRAFT_ID,
                        principalTable: "PO_DRAFT",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_EVENT_ACTION",
                table: "AUDIT_EVENT",
                column: "ACTION");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_EVENT_OCCURRED_AT",
                table: "AUDIT_EVENT",
                column: "OCCURRED_AT");

            migrationBuilder.CreateIndex(
                name: "IX_EXTRACTION_JOB_QUEUED_AT",
                table: "EXTRACTION_JOB",
                column: "QUEUED_AT");

            migrationBuilder.CreateIndex(
                name: "IX_EXTRACTION_JOB_UPLOAD_FILE_ID",
                table: "EXTRACTION_JOB",
                column: "UPLOAD_FILE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PO_DRAFT_PO_NUMBER",
                table: "PO_DRAFT",
                column: "PO_NUMBER");

            migrationBuilder.CreateIndex(
                name: "IX_PO_DRAFT_UPLOAD_FILE_ID",
                table: "PO_DRAFT",
                column: "UPLOAD_FILE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PO_DRAFT_LINE_PO_DRAFT_ID",
                table: "PO_DRAFT_LINE",
                column: "PO_DRAFT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_UPLOAD_FILE_CHECKSUM",
                table: "UPLOAD_FILE",
                column: "CHECKSUM");

            migrationBuilder.CreateIndex(
                name: "IX_UPLOAD_FILE_STATUS",
                table: "UPLOAD_FILE",
                column: "STATUS");

            migrationBuilder.CreateIndex(
                name: "IX_UPLOAD_FILE_UPLOADED_AT",
                table: "UPLOAD_FILE",
                column: "UPLOADED_AT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AUDIT_EVENT");

            migrationBuilder.DropTable(
                name: "EXTRACTION_JOB");

            migrationBuilder.DropTable(
                name: "PO_DRAFT_LINE");

            migrationBuilder.DropTable(
                name: "UPLOAD_FILE");

            migrationBuilder.DropTable(
                name: "PO_DRAFT");
        }
    }
}
