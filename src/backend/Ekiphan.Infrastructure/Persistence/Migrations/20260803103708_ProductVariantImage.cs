using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductVariantImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MediaAssetId",
                table: "ProductVariants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_MediaAssetId",
                table: "ProductVariants",
                column: "MediaAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_MediaAssets_MediaAssetId",
                table: "ProductVariants",
                column: "MediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_MediaAssets_MediaAssetId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_MediaAssetId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "MediaAssetId",
                table: "ProductVariants");
        }
    }
}
