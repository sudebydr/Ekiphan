using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductMediaZipImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByImportBatchId",
                table: "ProductMedia",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProductMedia",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ProductMedia",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "ProductMediaImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    OriginalFileHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalFiles = table.Column<int>(type: "int", nullable: false),
                    MatchedFileCount = table.Column<int>(type: "int", nullable: false),
                    ImportedFileCount = table.Column<int>(type: "int", nullable: false),
                    SkippedFileCount = table.Column<int>(type: "int", nullable: false),
                    ErrorFileCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RolledBackAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RolledBackByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMediaImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductMediaImportBatchItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemporaryFileId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExtractedSku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContentHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProductVersionAtImport = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMediaImportBatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMediaImportBatchItems_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductMediaImportBatchItems_ProductMediaImportBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductMediaImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductMediaImportBatchItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductMedia_CreatedByImportBatchId",
                table: "ProductMedia",
                column: "CreatedByImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaImportBatches_CreatedByUserId",
                table: "ProductMediaImportBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaImportBatches_OriginalFileHash",
                table: "ProductMediaImportBatches",
                column: "OriginalFileHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaImportBatches_Status",
                table: "ProductMediaImportBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaImportBatchItems_BatchId",
                table: "ProductMediaImportBatchItems",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaImportBatchItems_ContentHash",
                table: "ProductMediaImportBatchItems",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaImportBatchItems_ExtractedSku",
                table: "ProductMediaImportBatchItems",
                column: "ExtractedSku");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaImportBatchItems_MediaAssetId",
                table: "ProductMediaImportBatchItems",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaImportBatchItems_ProductId",
                table: "ProductMediaImportBatchItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaImportBatchItems_Status",
                table: "ProductMediaImportBatchItems",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMedia_ProductMediaImportBatches_CreatedByImportBatchId",
                table: "ProductMedia",
                column: "CreatedByImportBatchId",
                principalTable: "ProductMediaImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductMedia_ProductMediaImportBatches_CreatedByImportBatchId",
                table: "ProductMedia");

            migrationBuilder.DropTable(
                name: "ProductMediaImportBatchItems");

            migrationBuilder.DropTable(
                name: "ProductMediaImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_ProductMedia_CreatedByImportBatchId",
                table: "ProductMedia");

            migrationBuilder.DropColumn(
                name: "CreatedByImportBatchId",
                table: "ProductMedia");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProductMedia");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductMedia");
        }
    }
}
