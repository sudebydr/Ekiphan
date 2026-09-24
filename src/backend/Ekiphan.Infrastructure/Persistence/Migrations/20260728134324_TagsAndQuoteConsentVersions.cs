using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TagsAndQuoteConsentVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommercialCommunicationConsentVersion",
                table: "QuoteRequests",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KvkkConsentVersion",
                table: "QuoteRequests",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "legacy-unspecified");

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductTags",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTags", x => new { x.ProductId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ProductTags_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TagTranslations",
                columns: table => new
                {
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagTranslations", x => new { x.TagId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_TagTranslations_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuoteRequests_CommercialConsentPair",
                table: "QuoteRequests",
                sql: "([CommercialCommunicationConsentAt] IS NULL AND [CommercialCommunicationConsentVersion] IS NULL) OR ([CommercialCommunicationConsentAt] IS NOT NULL AND [CommercialCommunicationConsentVersion] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuoteRequests_KvkkConsentVersion",
                table: "QuoteRequests",
                sql: "LEN([KvkkConsentVersion]) > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTags_TagId_SortOrder_ProductId",
                table: "ProductTags",
                columns: new[] { "TagId", "SortOrder", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Code",
                table: "Tags",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagTranslations_LanguageCode_Name",
                table: "TagTranslations",
                columns: new[] { "LanguageCode", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_TagTranslations_LanguageCode_Slug",
                table: "TagTranslations",
                columns: new[] { "LanguageCode", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductTags");

            migrationBuilder.DropTable(
                name: "TagTranslations");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuoteRequests_CommercialConsentPair",
                table: "QuoteRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuoteRequests_KvkkConsentVersion",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "CommercialCommunicationConsentVersion",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "KvkkConsentVersion",
                table: "QuoteRequests");
        }
    }
}
