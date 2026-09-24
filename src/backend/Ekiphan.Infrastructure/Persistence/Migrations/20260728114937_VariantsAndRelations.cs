using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VariantsAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationType = table.Column<int>(type: "int", nullable: false),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    IsBidirectional = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRelations", x => x.Id);
                    table.CheckConstraint("CK_ProductRelations_DifferentProducts", "[SourceProductId] <> [TargetProductId]");
                    table.ForeignKey(
                        name: "FK_ProductRelations_Products_SourceProductId",
                        column: x => x.SourceProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductRelations_Products_TargetProductId",
                        column: x => x.TargetProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantGroups_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SKU = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantGroupTranslations",
                columns: table => new
                {
                    VariantGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantGroupTranslations", x => new { x.VariantGroupId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_ProductVariantGroupTranslations_ProductVariantGroups_VariantGroupId",
                        column: x => x.VariantGroupId,
                        principalTable: "ProductVariantGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantOptions", x => x.Id);
                    table.UniqueConstraint("AK_ProductVariantOptions_VariantGroupId_Id", x => new { x.VariantGroupId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProductVariantOptions_ProductVariantGroups_VariantGroupId",
                        column: x => x.VariantGroupId,
                        principalTable: "ProductVariantGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantOptionTranslations",
                columns: table => new
                {
                    VariantOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantOptionTranslations", x => new { x.VariantOptionId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_ProductVariantOptionTranslations_ProductVariantOptions_VariantOptionId",
                        column: x => x.VariantOptionId,
                        principalTable: "ProductVariantOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantSelections",
                columns: table => new
                {
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantSelections", x => new { x.ProductVariantId, x.VariantGroupId });
                    table.ForeignKey(
                        name: "FK_ProductVariantSelections_ProductVariantOptions_VariantGroupId_VariantOptionId",
                        columns: x => new { x.VariantGroupId, x.VariantOptionId },
                        principalTable: "ProductVariantOptions",
                        principalColumns: new[] { "VariantGroupId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariantSelections_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductRelations_SourceProductId_RelationType_IsActive_SortOrder",
                table: "ProductRelations",
                columns: new[] { "SourceProductId", "RelationType", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductRelations_SourceProductId_TargetProductId_RelationType",
                table: "ProductRelations",
                columns: new[] { "SourceProductId", "TargetProductId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductRelations_TargetProductId_RelationType_IsBidirectional_IsActive",
                table: "ProductRelations",
                columns: new[] { "TargetProductId", "RelationType", "IsBidirectional", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantGroups_ProductId_Code",
                table: "ProductVariantGroups",
                columns: new[] { "ProductId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantGroups_ProductId_SortOrder",
                table: "ProductVariantGroups",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptions_VariantGroupId_Code",
                table: "ProductVariantOptions",
                columns: new[] { "VariantGroupId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_SortOrder",
                table: "ProductVariants",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SKU",
                table: "ProductVariants",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantSelections_VariantGroupId_VariantOptionId",
                table: "ProductVariantSelections",
                columns: new[] { "VariantGroupId", "VariantOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantSelections_VariantOptionId",
                table: "ProductVariantSelections",
                column: "VariantOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductRelations");

            migrationBuilder.DropTable(
                name: "ProductVariantGroupTranslations");

            migrationBuilder.DropTable(
                name: "ProductVariantOptionTranslations");

            migrationBuilder.DropTable(
                name: "ProductVariantSelections");

            migrationBuilder.DropTable(
                name: "ProductVariantOptions");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "ProductVariantGroups");
        }
    }
}
