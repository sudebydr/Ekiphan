using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManagedHomepageHero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomepageHeroes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DesktopMediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MobileMediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomepageHeroes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomepageHeroes_MediaAssets_DesktopMediaId",
                        column: x => x.DesktopMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HomepageHeroes_MediaAssets_MobileMediaId",
                        column: x => x.MobileMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HomepageHeroTranslations",
                columns: table => new
                {
                    HomepageHeroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrimaryCtaLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrimaryCtaUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: false),
                    SecondaryCtaLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SecondaryCtaUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomepageHeroTranslations", x => new { x.HomepageHeroId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_HomepageHeroTranslations_HomepageHeroes_HomepageHeroId",
                        column: x => x.HomepageHeroId,
                        principalTable: "HomepageHeroes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomepageHeroes_DesktopMediaId",
                table: "HomepageHeroes",
                column: "DesktopMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_HomepageHeroes_IsPublished_SortOrder",
                table: "HomepageHeroes",
                columns: new[] { "IsPublished", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_HomepageHeroes_MobileMediaId",
                table: "HomepageHeroes",
                column: "MobileMediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomepageHeroTranslations");

            migrationBuilder.DropTable(
                name: "HomepageHeroes");
        }
    }
}
