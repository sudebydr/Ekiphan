using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingProductRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingProductRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetNormalizedSku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelationType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingProductRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingProductRelations_Products_SourceProductId",
                        column: x => x.SourceProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingProductRelations_SourceProductId_TargetNormalizedSku_RelationType",
                table: "PendingProductRelations",
                columns: new[] { "SourceProductId", "TargetNormalizedSku", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingProductRelations_TargetNormalizedSku",
                table: "PendingProductRelations",
                column: "TargetNormalizedSku");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingProductRelations");
        }
    }
}
