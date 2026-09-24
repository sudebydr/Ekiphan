using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedProductManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchivedByUserId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastQualityCheckAt",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedSku",
                table: "Products",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedByUserId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityScore",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedForReviewAt",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedForReviewByUserId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProductBulkOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalItemCount = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    RequestPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultSummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBulkOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BulkOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductRevisions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductBulkOperationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BulkOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousVersionNumber = table.Column<int>(type: "int", nullable: true),
                    NewVersionNumber = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBulkOperationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBulkOperationItems_ProductBulkOperations_BulkOperationId",
                        column: x => x.BulkOperationId,
                        principalTable: "ProductBulkOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_NormalizedSku",
                table: "Products",
                column: "NormalizedSku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_QualityScore",
                table: "Products",
                column: "QualityScore");

            migrationBuilder.CreateIndex(
                name: "IX_Products_WorkflowStatus",
                table: "Products",
                column: "WorkflowStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBulkOperationItems_BulkOperationId",
                table: "ProductBulkOperationItems",
                column: "BulkOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBulkOperationItems_ProductId",
                table: "ProductBulkOperationItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBulkOperationItems_Status",
                table: "ProductBulkOperationItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBulkOperations_OperationType",
                table: "ProductBulkOperations",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBulkOperations_RequestedAt",
                table: "ProductBulkOperations",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBulkOperations_RequestedByUserId",
                table: "ProductBulkOperations",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBulkOperations_Status",
                table: "ProductBulkOperations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRevisions_CreatedAt",
                table: "ProductRevisions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRevisions_CreatedByUserId",
                table: "ProductRevisions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRevisions_ProductId",
                table: "ProductRevisions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRevisions_ProductId_VersionNumber",
                table: "ProductRevisions",
                columns: new[] { "ProductId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductBulkOperationItems");

            migrationBuilder.DropTable(
                name: "ProductRevisions");

            migrationBuilder.DropTable(
                name: "ProductBulkOperations");

            migrationBuilder.DropIndex(
                name: "IX_Products_NormalizedSku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_QualityScore",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_WorkflowStatus",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LastQualityCheckAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NormalizedSku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PublishedByUserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SubmittedForReviewAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SubmittedForReviewByUserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "Products");
        }
    }
}
