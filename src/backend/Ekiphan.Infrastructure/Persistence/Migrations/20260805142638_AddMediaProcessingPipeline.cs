using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaProcessingPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "MediaAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalExtension",
                table: "MediaAssets",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalHeight",
                table: "MediaAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalWidth",
                table: "MediaAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedAt",
                table: "MediaAssets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingErrorCode",
                table: "MediaAssets",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingErrorMessage",
                table: "MediaAssets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingStatus",
                table: "MediaAssets",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MediaAssets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<string>(
                name: "StorageProvider",
                table: "MediaAssets",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "Local");

            migrationBuilder.CreateTable(
                name: "MediaVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantType = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Extension = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Quality = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaVariants_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_CreatedAt",
                table: "MediaAssets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ProcessingStatus",
                table: "MediaAssets",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVariants_MediaAssetId_VariantType",
                table: "MediaVariants",
                columns: new[] { "MediaAssetId", "VariantType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaVariants_StorageKey",
                table: "MediaVariants",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaVariants");

            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_CreatedAt",
                table: "MediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_ProcessingStatus",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "OriginalExtension",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "OriginalHeight",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "OriginalWidth",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ProcessingErrorCode",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ProcessingErrorMessage",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                table: "MediaAssets");
        }
    }
}
