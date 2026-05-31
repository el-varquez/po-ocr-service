using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoOcr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DELETED_AT",
                table: "PO_DRAFT",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DELETED_BY",
                table: "PO_DRAFT",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PO_DRAFT_DELETED_AT",
                table: "PO_DRAFT",
                column: "DELETED_AT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PO_DRAFT_DELETED_AT",
                table: "PO_DRAFT");

            migrationBuilder.DropColumn(
                name: "DELETED_AT",
                table: "PO_DRAFT");

            migrationBuilder.DropColumn(
                name: "DELETED_BY",
                table: "PO_DRAFT");
        }
    }
}
