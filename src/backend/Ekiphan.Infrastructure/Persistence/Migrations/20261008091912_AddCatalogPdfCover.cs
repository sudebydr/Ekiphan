using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogPdfCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CoverMediaAssetId",
                table: "MediaAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_CoverMediaAssetId",
                table: "MediaAssets",
                column: "CoverMediaAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaAssets_MediaAssets_CoverMediaAssetId",
                table: "MediaAssets",
                column: "CoverMediaAssetId",
                principalTable: "MediaAssets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaAssets_MediaAssets_CoverMediaAssetId",
                table: "MediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_CoverMediaAssetId",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "CoverMediaAssetId",
                table: "MediaAssets");
        }
    }
}
