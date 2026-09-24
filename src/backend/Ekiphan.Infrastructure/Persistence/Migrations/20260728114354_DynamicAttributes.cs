using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DynamicAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    UnitDimension = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Dimension = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ConversionFactorToBase = table.Column<decimal>(type: "decimal(24,10)", precision: 24, scale: 10, nullable: false),
                    IsBaseUnit = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.CheckConstraint("CK_Units_ConversionFactorToBase", "[ConversionFactorToBase] > 0");
                });

            migrationBuilder.CreateTable(
                name: "AttributeOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeOptions_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttributeTranslations",
                columns: table => new
                {
                    AttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeTranslations", x => new { x.AttributeId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_AttributeTranslations_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryAttributes",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsFilterable = table.Column<bool>(type: "bit", nullable: false),
                    IsVisibleOnProduct = table.Column<bool>(type: "bit", nullable: false),
                    IsVisibleOnComparison = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryAttributes", x => new { x.CategoryId, x.AttributeId });
                    table.ForeignKey(
                        name: "FK_CategoryAttributes_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryAttributes_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttributeOptionTranslations",
                columns: table => new
                {
                    AttributeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeOptionTranslations", x => new { x.AttributeOptionId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_AttributeOptionTranslations_AttributeOptions_AttributeOptionId",
                        column: x => x.AttributeOptionId,
                        principalTable: "AttributeOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NumericValue = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    AttributeOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RawValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeValues", x => x.Id);
                    table.CheckConstraint("CK_ProductAttributeValues_Sequence", "[Sequence] >= 0");
                    table.CheckConstraint("CK_ProductAttributeValues_SingleTypedValue", "(CASE WHEN [TextValue] IS NULL THEN 0 ELSE 1 END + CASE WHEN [NumericValue] IS NULL THEN 0 ELSE 1 END + CASE WHEN [BooleanValue] IS NULL THEN 0 ELSE 1 END + CASE WHEN [AttributeOptionId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.CheckConstraint("CK_ProductAttributeValues_UnitRequiresNumber", "[UnitId] IS NULL OR [NumericValue] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_AttributeOptions_AttributeOptionId",
                        column: x => x.AttributeOptionId,
                        principalTable: "AttributeOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeOptions_AttributeId_Code",
                table: "AttributeOptions",
                columns: new[] { "AttributeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attributes_Code",
                table: "Attributes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_AttributeId",
                table: "CategoryAttributes",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CategoryId_IsFilterable_SortOrder",
                table: "CategoryAttributes",
                columns: new[] { "CategoryId", "IsFilterable", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CategoryId_IsVisibleOnComparison_SortOrder",
                table: "CategoryAttributes",
                columns: new[] { "CategoryId", "IsVisibleOnComparison", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_AttributeId_AttributeOptionId_ProductId",
                table: "ProductAttributeValues",
                columns: new[] { "AttributeId", "AttributeOptionId", "ProductId" },
                filter: "[AttributeOptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_AttributeId_NumericValue_ProductId",
                table: "ProductAttributeValues",
                columns: new[] { "AttributeId", "NumericValue", "ProductId" },
                filter: "[NumericValue] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_AttributeOptionId",
                table: "ProductAttributeValues",
                column: "AttributeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_ProductId_AttributeId_Sequence",
                table: "ProductAttributeValues",
                columns: new[] { "ProductId", "AttributeId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_UnitId",
                table: "ProductAttributeValues",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Code",
                table: "Units",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_Dimension",
                table: "Units",
                column: "Dimension",
                unique: true,
                filter: "[IsBaseUnit] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttributeOptionTranslations");

            migrationBuilder.DropTable(
                name: "AttributeTranslations");

            migrationBuilder.DropTable(
                name: "CategoryAttributes");

            migrationBuilder.DropTable(
                name: "ProductAttributeValues");

            migrationBuilder.DropTable(
                name: "AttributeOptions");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Attributes");
        }
    }
}
