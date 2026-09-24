using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteQuoteOperationsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuoteInternalNotes_QuoteRequestId_RecordedAt",
                table: "QuoteInternalNotes");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnonymizedAt",
                table: "QuoteRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "QuoteRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "QuoteRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchivedByUserId",
                table: "QuoteRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymized",
                table: "QuoteRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastActivityAt",
                table: "QuoteRequests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "SpamRiskScore",
                table: "QuoteRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SpamStatus",
                table: "QuoteRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "QuoteInternalNotes",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "QuoteInternalNotes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "QuoteInternalNotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "QuoteInternalNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "QuoteInternalNotes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.CreateTable(
                name: "EmailQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TemplateDataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailQueueItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuoteActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviousValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteActivities_AdminUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QuoteActivities_QuoteRequests_QuoteRequestId",
                        column: x => x.QuoteRequestId,
                        principalTable: "QuoteRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteSpamAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    ReasonsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecommendedAction = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteSpamAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteSpamAssessments_QuoteRequests_QuoteRequestId",
                        column: x => x.QuoteRequestId,
                        principalTable: "QuoteRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_ArchivedAt",
                table: "QuoteRequests",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_IsAnonymized",
                table: "QuoteRequests",
                column: "IsAnonymized");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_LastActivityAt",
                table: "QuoteRequests",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteInternalNotes_QuoteRequestId_IsDeleted_RecordedAt",
                table: "QuoteInternalNotes",
                columns: new[] { "QuoteRequestId", "IsDeleted", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItems_CreatedAt",
                table: "EmailQueueItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItems_Recipient",
                table: "EmailQueueItems",
                column: "Recipient");

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItems_RelatedEntityType_RelatedEntityId",
                table: "EmailQueueItems",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailQueueItems_Status_NextAttemptAt",
                table: "EmailQueueItems",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteActivities_ActivityType",
                table: "QuoteActivities",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteActivities_ActorUserId",
                table: "QuoteActivities",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteActivities_CorrelationId",
                table: "QuoteActivities",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteActivities_QuoteRequestId_CreatedAt",
                table: "QuoteActivities",
                columns: new[] { "QuoteRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteSpamAssessments_QuoteRequestId",
                table: "QuoteSpamAssessments",
                column: "QuoteRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteSpamAssessments_RiskScore",
                table: "QuoteSpamAssessments",
                column: "RiskScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailQueueItems");

            migrationBuilder.DropTable(
                name: "QuoteActivities");

            migrationBuilder.DropTable(
                name: "QuoteSpamAssessments");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_ArchivedAt",
                table: "QuoteRequests");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_IsAnonymized",
                table: "QuoteRequests");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_LastActivityAt",
                table: "QuoteRequests");

            migrationBuilder.DropIndex(
                name: "IX_QuoteInternalNotes_QuoteRequestId_IsDeleted_RecordedAt",
                table: "QuoteInternalNotes");

            migrationBuilder.DropColumn(
                name: "AnonymizedAt",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "IsAnonymized",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "SpamRiskScore",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "SpamStatus",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "QuoteInternalNotes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "QuoteInternalNotes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "QuoteInternalNotes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "QuoteInternalNotes");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "QuoteInternalNotes",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteInternalNotes_QuoteRequestId_RecordedAt",
                table: "QuoteInternalNotes",
                columns: new[] { "QuoteRequestId", "RecordedAt" });
        }
    }
}
