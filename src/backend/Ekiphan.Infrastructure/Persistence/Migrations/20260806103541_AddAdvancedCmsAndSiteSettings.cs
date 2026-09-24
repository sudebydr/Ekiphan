using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedCmsAndSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BannerGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Placement = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannerGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CmsRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CmsRevisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerLogos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AltTextTr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AltTextEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLogos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FooterColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleTr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FooterColumns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProjectDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CoverMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowStatus = table.Column<int>(type: "int", nullable: false),
                    PublishAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Showrooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoverMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VirtualTourUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmbedCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkflowStatus = table.Column<int>(type: "int", nullable: false),
                    PublishAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Showrooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Banners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BannerGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DesktopMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MobileMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkTarget = table.Column<int>(type: "int", nullable: false),
                    WorkflowStatus = table.Column<int>(type: "int", nullable: false),
                    PublishAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishEndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Banners_BannerGroups_BannerGroupId",
                        column: x => x.BannerGroupId,
                        principalTable: "BannerGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FooterLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FooterColumnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabelTr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LabelEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LinkTarget = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FooterLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FooterLinks_FooterColumns_FooterColumnId",
                        column: x => x.FooterColumnId,
                        principalTable: "FooterColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceProjectMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    CaptionTr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaptionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceProjectMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceProjectMedia_ReferenceProjects_ReferenceProjectId",
                        column: x => x.ReferenceProjectId,
                        principalTable: "ReferenceProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceProjectProducts",
                columns: table => new
                {
                    ReferenceProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceProjectProducts", x => new { x.ReferenceProjectId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_ReferenceProjectProducts_ReferenceProjects_ReferenceProjectId",
                        column: x => x.ReferenceProjectId,
                        principalTable: "ReferenceProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceProjectTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenGraphTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenGraphDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenGraphMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceProjectTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceProjectTranslations_ReferenceProjects_ReferenceProjectId",
                        column: x => x.ReferenceProjectId,
                        principalTable: "ReferenceProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowroomHotspots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShowroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TitleTr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionTr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PositionX = table.Column<double>(type: "float", nullable: false),
                    PositionY = table.Column<double>(type: "float", nullable: false),
                    PositionZ = table.Column<double>(type: "float", nullable: false),
                    SceneIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowroomHotspots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowroomHotspots_Showrooms_ShowroomId",
                        column: x => x.ShowroomId,
                        principalTable: "Showrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowroomMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShowroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CaptionTr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaptionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowroomMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowroomMedia_Showrooms_ShowroomId",
                        column: x => x.ShowroomId,
                        principalTable: "Showrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowroomTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShowroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenGraphTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenGraphDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpenGraphMediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowroomTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowroomTranslations_Showrooms_ShowroomId",
                        column: x => x.ShowroomId,
                        principalTable: "Showrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BannerTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subtitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CtaText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessibleLabel = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannerTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BannerTranslations_Banners_BannerId",
                        column: x => x.BannerId,
                        principalTable: "Banners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BannerGroups_Code",
                table: "BannerGroups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BannerGroups_Placement",
                table: "BannerGroups",
                column: "Placement");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_BannerGroupId",
                table: "Banners",
                column: "BannerGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_PublishAt",
                table: "Banners",
                column: "PublishAt");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_PublishEndAt",
                table: "Banners",
                column: "PublishEndAt");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_SortOrder",
                table: "Banners",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_WorkflowStatus",
                table: "Banners",
                column: "WorkflowStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BannerTranslations_BannerId",
                table: "BannerTranslations",
                column: "BannerId");

            migrationBuilder.CreateIndex(
                name: "IX_CmsRevisions_CreatedAt",
                table: "CmsRevisions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CmsRevisions_EntityType_EntityId_VersionNumber",
                table: "CmsRevisions",
                columns: new[] { "EntityType", "EntityId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLogos_IsActive",
                table: "CustomerLogos",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLogos_IsFeatured",
                table: "CustomerLogos",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLogos_SortOrder",
                table: "CustomerLogos",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_FooterColumns_Code",
                table: "FooterColumns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FooterColumns_SortOrder",
                table: "FooterColumns",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_FooterLinks_FooterColumnId",
                table: "FooterLinks",
                column: "FooterColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_FooterLinks_SortOrder",
                table: "FooterLinks",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceProjectMedia_ReferenceProjectId_MediaAssetId",
                table: "ReferenceProjectMedia",
                columns: new[] { "ReferenceProjectId", "MediaAssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceProjectMedia_ReferenceProjectId_SortOrder",
                table: "ReferenceProjectMedia",
                columns: new[] { "ReferenceProjectId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceProjectProducts_ReferenceProjectId_SortOrder",
                table: "ReferenceProjectProducts",
                columns: new[] { "ReferenceProjectId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceProjects_IsFeatured",
                table: "ReferenceProjects",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceProjects_PublishedAt",
                table: "ReferenceProjects",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceProjects_SortOrder",
                table: "ReferenceProjects",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceProjects_WorkflowStatus",
                table: "ReferenceProjects",
                column: "WorkflowStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceProjectTranslations_LanguageCode_Slug",
                table: "ReferenceProjectTranslations",
                columns: new[] { "LanguageCode", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceProjectTranslations_ReferenceProjectId_LanguageCode",
                table: "ReferenceProjectTranslations",
                columns: new[] { "ReferenceProjectId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomHotspots_SceneIdentifier",
                table: "ShowroomHotspots",
                column: "SceneIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomHotspots_ShowroomId",
                table: "ShowroomHotspots",
                column: "ShowroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomHotspots_SortOrder",
                table: "ShowroomHotspots",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomMedia_ShowroomId",
                table: "ShowroomMedia",
                column: "ShowroomId");

            migrationBuilder.CreateIndex(
                name: "IX_Showrooms_SortOrder",
                table: "Showrooms",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Showrooms_WorkflowStatus",
                table: "Showrooms",
                column: "WorkflowStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomTranslations_LanguageCode_Slug",
                table: "ShowroomTranslations",
                columns: new[] { "LanguageCode", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowroomTranslations_ShowroomId_LanguageCode",
                table: "ShowroomTranslations",
                columns: new[] { "ShowroomId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_Category",
                table: "SiteSettings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_IsPublic",
                table: "SiteSettings",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_Key",
                table: "SiteSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannerTranslations");

            migrationBuilder.DropTable(
                name: "CmsRevisions");

            migrationBuilder.DropTable(
                name: "CustomerLogos");

            migrationBuilder.DropTable(
                name: "FooterLinks");

            migrationBuilder.DropTable(
                name: "ReferenceProjectMedia");

            migrationBuilder.DropTable(
                name: "ReferenceProjectProducts");

            migrationBuilder.DropTable(
                name: "ReferenceProjectTranslations");

            migrationBuilder.DropTable(
                name: "ShowroomHotspots");

            migrationBuilder.DropTable(
                name: "ShowroomMedia");

            migrationBuilder.DropTable(
                name: "ShowroomTranslations");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "Banners");

            migrationBuilder.DropTable(
                name: "FooterColumns");

            migrationBuilder.DropTable(
                name: "ReferenceProjects");

            migrationBuilder.DropTable(
                name: "Showrooms");

            migrationBuilder.DropTable(
                name: "BannerGroups");
        }
    }
}
