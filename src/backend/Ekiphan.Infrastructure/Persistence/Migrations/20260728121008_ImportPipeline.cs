using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImportPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    SourceSha256Checksum = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    IsDryRun = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidationStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValidationCompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishingStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalRowCount = table.Column<int>(type: "int", nullable: false),
                    ValidRowCount = table.Column<int>(type: "int", nullable: false),
                    InvalidRowCount = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    PublishedRowCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.CheckConstraint("CK_ImportJobs_FileSource", "([SourceType] = 4 AND [OriginalFileName] IS NULL) OR ([SourceType] <> 4 AND [OriginalFileName] IS NOT NULL)");
                    table.CheckConstraint("CK_ImportJobs_RowCounts", "[TotalRowCount] >= 0 AND [ValidRowCount] >= 0 AND [InvalidRowCount] >= 0 AND [WarningCount] >= 0 AND [PublishedRowCount] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ImportRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SheetName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    SKU = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRows", x => x.Id);
                    table.CheckConstraint("CK_ImportRows_RowNumber", "[RowNumber] > 0");
                    table.ForeignKey(
                        name: "FK_ImportRows_ImportJobs_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "ImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RawValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportIssues_ImportRows_ImportRowId",
                        column: x => x.ImportRowId,
                        principalTable: "ImportRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportIssues_ImportRowId_Severity_Code",
                table: "ImportIssues",
                columns: new[] { "ImportRowId", "Severity", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CreatedByUserId_CreatedAt",
                table: "ImportJobs",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_SourceSha256Checksum",
                table: "ImportJobs",
                column: "SourceSha256Checksum",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status_CreatedAt",
                table: "ImportJobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRows_ImportJobId_SheetName_RowNumber",
                table: "ImportRows",
                columns: new[] { "ImportJobId", "SheetName", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportRows_ImportJobId_Status_RowNumber",
                table: "ImportRows",
                columns: new[] { "ImportJobId", "Status", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRows_SKU_Status",
                table: "ImportRows",
                columns: new[] { "SKU", "Status" },
                filter: "[SKU] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportIssues");

            migrationBuilder.DropTable(
                name: "ImportRows");

            migrationBuilder.DropTable(
                name: "ImportJobs");
        }
    }
}
