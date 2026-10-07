using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FilterImportJobChecksumForNonDryRuns : Migration
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
                filter: "[IsDryRun] = 0");
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
                unique: true);
        }
    }
}
