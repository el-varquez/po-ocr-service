using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoOcr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPoDraftFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UNIT",
                table: "PO_DRAFT_LINE");

            migrationBuilder.RenameColumn(
                name: "LINE_TOTAL",
                table: "PO_DRAFT_LINE",
                newName: "AMOUNT");

            migrationBuilder.RenameColumn(
                name: "PO_NUMBER",
                table: "PO_DRAFT",
                newName: "REFERENCE_NUMBER");

            migrationBuilder.RenameColumn(
                name: "CUSTOMER_NAME",
                table: "PO_DRAFT",
                newName: "VENDOR_NAME");

            migrationBuilder.RenameIndex(
                name: "IX_PO_DRAFT_PO_NUMBER",
                table: "PO_DRAFT",
                newName: "IX_PO_DRAFT_REFERENCE_NUMBER");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DATE_EXPECTED",
                table: "PO_DRAFT",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PAYMENT_TERMS",
                table: "PO_DRAFT",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SHIP_TO",
                table: "PO_DRAFT",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SHIP_VIA",
                table: "PO_DRAFT",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "TOTAL_AMOUNT",
                table: "PO_DRAFT",
                type: "decimal(16,4)",
                precision: 16,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PO_DRAFT_PO_DATE",
                table: "PO_DRAFT",
                column: "PO_DATE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PO_DRAFT_PO_DATE",
                table: "PO_DRAFT");

            migrationBuilder.DropColumn(
                name: "DATE_EXPECTED",
                table: "PO_DRAFT");

            migrationBuilder.DropColumn(
                name: "PAYMENT_TERMS",
                table: "PO_DRAFT");

            migrationBuilder.DropColumn(
                name: "SHIP_TO",
                table: "PO_DRAFT");

            migrationBuilder.DropColumn(
                name: "SHIP_VIA",
                table: "PO_DRAFT");

            migrationBuilder.DropColumn(
                name: "TOTAL_AMOUNT",
                table: "PO_DRAFT");

            migrationBuilder.RenameColumn(
                name: "AMOUNT",
                table: "PO_DRAFT_LINE",
                newName: "LINE_TOTAL");

            migrationBuilder.RenameColumn(
                name: "VENDOR_NAME",
                table: "PO_DRAFT",
                newName: "CUSTOMER_NAME");

            migrationBuilder.RenameColumn(
                name: "REFERENCE_NUMBER",
                table: "PO_DRAFT",
                newName: "PO_NUMBER");

            migrationBuilder.RenameIndex(
                name: "IX_PO_DRAFT_REFERENCE_NUMBER",
                table: "PO_DRAFT",
                newName: "IX_PO_DRAFT_PO_NUMBER");

            migrationBuilder.AddColumn<string>(
                name: "UNIT",
                table: "PO_DRAFT_LINE",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
