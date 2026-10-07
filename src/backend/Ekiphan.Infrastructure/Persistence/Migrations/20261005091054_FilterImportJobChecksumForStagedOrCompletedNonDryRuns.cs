using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FilterImportJobChecksumForStagedOrCompletedNonDryRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportJobs_SourceSha256Checksum",
                table: "ImportJobs");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_SourceSha256Checksum",
                table: "ImportJobs",
                column: "SourceSha256Checksum",
                unique: true,
                filter: "[IsDryRun] = 0 AND [Status] IN (3, 5, 6)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportJobs_SourceSha256Checksum",
                table: "ImportJobs");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_SourceSha256Checksum",
                table: "ImportJobs",
                column: "SourceSha256Checksum",
                unique: true,
                filter: "[IsDryRun] = 0");
        }
    }
}
