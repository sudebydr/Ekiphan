using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase8SeoManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoFollow",
                table: "ProductTranslations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OpenGraphDescription",
                table: "ProductTranslations",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpenGraphImageMediaId",
                table: "ProductTranslations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenGraphTitle",
                table: "ProductTranslations",
                type: "nvarchar(95)",
                maxLength: 95,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "PressReleaseTranslations",
                type: "varchar(2048)",
                unicode: false,
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "PressReleaseTranslations",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "PressReleaseTranslations",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoFollow",
                table: "PressReleaseTranslations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NoIndex",
                table: "PressReleaseTranslations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OpenGraphDescription",
                table: "PressReleaseTranslations",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpenGraphImageMediaId",
                table: "PressReleaseTranslations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenGraphTitle",
                table: "PressReleaseTranslations",
                type: "nvarchar(95)",
                maxLength: 95,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "PressReleaseTranslations",
                type: "varchar(250)",
                unicode: false,
                maxLength: 250,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [PressReleaseTranslations]
                SET [Slug] = CONCAT('press-', LOWER(REPLACE(CONVERT(varchar(36), [PressReleaseId]), '-', '')))
                WHERE [Slug] IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "PressReleaseTranslations",
                type: "varchar(250)",
                unicode: false,
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(250)",
                oldUnicode: false,
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenGraphDescription",
                table: "ContentPageTranslations",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpenGraphImageMediaId",
                table: "ContentPageTranslations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenGraphTitle",
                table: "ContentPageTranslations",
                type: "nvarchar(95)",
                maxLength: 95,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "CategoryTranslations",
                type: "varchar(2048)",
                unicode: false,
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "CategoryTranslations",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "CategoryTranslations",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NoFollow",
                table: "CategoryTranslations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NoIndex",
                table: "CategoryTranslations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OpenGraphDescription",
                table: "CategoryTranslations",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpenGraphImageMediaId",
                table: "CategoryTranslations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenGraphTitle",
                table: "CategoryTranslations",
                type: "nvarchar(95)",
                maxLength: 95,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PressReleaseTranslations_LanguageCode_Slug",
                table: "PressReleaseTranslations",
                columns: new[] { "LanguageCode", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PressReleaseTranslations_LanguageCode_Slug",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "NoFollow",
                table: "ProductTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphDescription",
                table: "ProductTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphImageMediaId",
                table: "ProductTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphTitle",
                table: "ProductTranslations");

            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "NoFollow",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "NoIndex",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphDescription",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphImageMediaId",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphTitle",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "PressReleaseTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphDescription",
                table: "ContentPageTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphImageMediaId",
                table: "ContentPageTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphTitle",
                table: "ContentPageTranslations");

            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "CategoryTranslations");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "CategoryTranslations");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "CategoryTranslations");

            migrationBuilder.DropColumn(
                name: "NoFollow",
                table: "CategoryTranslations");

            migrationBuilder.DropColumn(
                name: "NoIndex",
                table: "CategoryTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphDescription",
                table: "CategoryTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphImageMediaId",
                table: "CategoryTranslations");

            migrationBuilder.DropColumn(
                name: "OpenGraphTitle",
                table: "CategoryTranslations");
        }
    }
}
