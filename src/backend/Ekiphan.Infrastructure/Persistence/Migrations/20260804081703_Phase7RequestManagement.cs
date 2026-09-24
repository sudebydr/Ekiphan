using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase7RequestManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "QuoteRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "ContactRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ContactRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ContactRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ContactRequestNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactRequestNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactRequestNotes_AdminUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactRequestNotes_ContactRequests_ContactRequestId",
                        column: x => x.ContactRequestId,
                        principalTable: "ContactRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactRequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactRequestStatusHistory_AdminUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContactRequestStatusHistory_ContactRequests_ContactRequestId",
                        column: x => x.ContactRequestId,
                        principalTable: "ContactRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteInternalNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteInternalNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteInternalNotes_AdminUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuoteInternalNotes_QuoteRequests_QuoteRequestId",
                        column: x => x.QuoteRequestId,
                        principalTable: "QuoteRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_AssignedToUserId",
                table: "QuoteRequests",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_AssignedToUserId",
                table: "ContactRequests",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_Status_CreatedAt",
                table: "ContactRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequestNotes_AuthorUserId",
                table: "ContactRequestNotes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequestNotes_ContactRequestId_RecordedAt",
                table: "ContactRequestNotes",
                columns: new[] { "ContactRequestId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequestStatusHistory_ChangedByUserId",
                table: "ContactRequestStatusHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequestStatusHistory_ContactRequestId_ChangedAt",
                table: "ContactRequestStatusHistory",
                columns: new[] { "ContactRequestId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteInternalNotes_AuthorUserId",
                table: "QuoteInternalNotes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteInternalNotes_QuoteRequestId_RecordedAt",
                table: "QuoteInternalNotes",
                columns: new[] { "QuoteRequestId", "RecordedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ContactRequests_AdminUsers_AssignedToUserId",
                table: "ContactRequests",
                column: "AssignedToUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                INSERT INTO [ContactRequestStatusHistory]
                    ([Id], [ContactRequestId], [FromStatus], [ToStatus],
                     [ChangedByUserId], [ChangedAt], [CreatedAt], [UpdatedAt])
                SELECT NEWID(), [Id], NULL, 1, NULL, [CreatedAt],
                       SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM [ContactRequests];

                INSERT INTO [AdminUserPermissions] ([AdminUserId], [Permission])
                SELECT grants.[AdminUserId], 'quotes.read'
                FROM [AdminUserPermissions] grants
                WHERE grants.[Permission] = 'quotes.manage'
                  AND NOT EXISTS (
                      SELECT 1 FROM [AdminUserPermissions] existing
                      WHERE existing.[AdminUserId] = grants.[AdminUserId]
                        AND existing.[Permission] = 'quotes.read');

                INSERT INTO [AdminUserPermissions] ([AdminUserId], [Permission])
                SELECT DISTINCT grants.[AdminUserId], 'contacts.read'
                FROM [AdminUserPermissions] grants
                WHERE grants.[Permission] IN ('quotes.manage', 'content.manage')
                  AND NOT EXISTS (
                      SELECT 1 FROM [AdminUserPermissions] existing
                      WHERE existing.[AdminUserId] = grants.[AdminUserId]
                        AND existing.[Permission] = 'contacts.read');

                INSERT INTO [AdminUserPermissions] ([AdminUserId], [Permission])
                SELECT DISTINCT grants.[AdminUserId], 'contacts.manage'
                FROM [AdminUserPermissions] grants
                WHERE grants.[Permission] IN ('quotes.manage', 'content.manage')
                  AND NOT EXISTS (
                      SELECT 1 FROM [AdminUserPermissions] existing
                      WHERE existing.[AdminUserId] = grants.[AdminUserId]
                        AND existing.[Permission] = 'contacts.manage');
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteRequests_AdminUsers_AssignedToUserId",
                table: "QuoteRequests",
                column: "AssignedToUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [AdminUserPermissions]
                WHERE [Permission] IN ('quotes.read', 'contacts.read', 'contacts.manage');
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ContactRequests_AdminUsers_AssignedToUserId",
                table: "ContactRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_QuoteRequests_AdminUsers_AssignedToUserId",
                table: "QuoteRequests");

            migrationBuilder.DropTable(
                name: "ContactRequestNotes");

            migrationBuilder.DropTable(
                name: "ContactRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "QuoteInternalNotes");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_AssignedToUserId",
                table: "QuoteRequests");

            migrationBuilder.DropIndex(
                name: "IX_ContactRequests_AssignedToUserId",
                table: "ContactRequests");

            migrationBuilder.DropIndex(
                name: "IX_ContactRequests_Status_CreatedAt",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ContactRequests");
        }
    }
}
