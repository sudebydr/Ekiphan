using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MediaLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    StorageKey = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    MimeType = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Sha256Checksum = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    ExternalUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                    table.CheckConstraint("CK_MediaAssets_ArchiveState", "([Status] = 2 AND [ArchivedAt] IS NOT NULL) OR ([Status] <> 2 AND [ArchivedAt] IS NULL)");
                    table.CheckConstraint("CK_MediaAssets_FileOrExternal", "([AssetType] = 4 AND [ExternalUrl] IS NOT NULL AND [StorageKey] IS NULL AND [MimeType] IS NULL AND [FileSizeBytes] IS NULL AND [Sha256Checksum] IS NULL) OR ([AssetType] <> 4 AND [ExternalUrl] IS NULL AND [StorageKey] IS NOT NULL AND [MimeType] IS NOT NULL AND [FileSizeBytes] IS NOT NULL AND [Sha256Checksum] IS NOT NULL)");
                    table.CheckConstraint("CK_MediaAssets_FileSize", "[FileSizeBytes] IS NULL OR [FileSizeBytes] > 0");
                });

            migrationBuilder.CreateTable(
                name: "BrandMedia",
                columns: table => new
                {
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandMedia", x => new { x.BrandId, x.MediaAssetId, x.Role });
                    table.ForeignKey(
                        name: "FK_BrandMedia_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BrandMedia_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoryMedia",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryMedia", x => new { x.CategoryId, x.Role });
                    table.ForeignKey(
                        name: "FK_CategoryMedia_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryMedia_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssetTranslations",
                columns: table => new
                {
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssetTranslations", x => new { x.MediaAssetId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_MediaAssetTranslations_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductMedia",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMedia", x => new { x.ProductId, x.MediaAssetId, x.Role });
                    table.CheckConstraint("CK_ProductMedia_DefaultRole", "[IsDefault] = 0 OR [Role] = 1");
                    table.ForeignKey(
                        name: "FK_ProductMedia_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductMedia_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandMedia_BrandId",
                table: "BrandMedia",
                column: "BrandId",
                unique: true,
                filter: "[Role] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BrandMedia_BrandId_Role_SortOrder",
                table: "BrandMedia",
                columns: new[] { "BrandId", "Role", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BrandMedia_MediaAssetId",
                table: "BrandMedia",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryMedia_MediaAssetId",
                table: "CategoryMedia",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_AssetType_Status_CreatedAt",
                table: "MediaAssets",
                columns: new[] { "AssetType", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Sha256Checksum",
                table: "MediaAssets",
                column: "Sha256Checksum",
                unique: true,
                filter: "[Sha256Checksum] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_StorageKey",
                table: "MediaAssets",
                column: "StorageKey",
                unique: true,
                filter: "[StorageKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMedia_MediaAssetId",
                table: "ProductMedia",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMedia_ProductId",
                table: "ProductMedia",
                column: "ProductId",
                unique: true,
                filter: "[IsDefault] = 1 AND [Role] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMedia_ProductId_Role_SortOrder",
                table: "ProductMedia",
                columns: new[] { "ProductId", "Role", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandMedia");

            migrationBuilder.DropTable(
                name: "CategoryMedia");

            migrationBuilder.DropTable(
                name: "MediaAssetTranslations");

            migrationBuilder.DropTable(
                name: "ProductMedia");

            migrationBuilder.DropTable(
                name: "MediaAssets");
        }
    }
}
