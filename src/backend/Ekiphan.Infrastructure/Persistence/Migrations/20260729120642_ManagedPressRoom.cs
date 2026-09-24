using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManagedPressRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PressReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoverMediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttachmentMediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PressReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PressReleases_MediaAssets_AttachmentMediaId",
                        column: x => x.AttachmentMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PressReleases_MediaAssets_CoverMediaId",
                        column: x => x.CoverMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PressReleaseTranslations",
                columns: table => new
                {
                    PressReleaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PressReleaseTranslations", x => new { x.PressReleaseId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_PressReleaseTranslations_PressReleases_PressReleaseId",
                        column: x => x.PressReleaseId,
                        principalTable: "PressReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PressReleases_AttachmentMediaId",
                table: "PressReleases",
                column: "AttachmentMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_PressReleases_CoverMediaId",
                table: "PressReleases",
                column: "CoverMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_PressReleases_IsPublished_PublishedAt",
                table: "PressReleases",
                columns: new[] { "IsPublished", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PressReleaseTranslations");

            migrationBuilder.DropTable(
                name: "PressReleases");
        }
    }
}
