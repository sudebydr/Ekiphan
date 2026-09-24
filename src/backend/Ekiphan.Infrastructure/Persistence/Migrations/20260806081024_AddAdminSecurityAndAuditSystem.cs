using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSecurityAndAuditSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorEnabled",
                table: "AdminUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastFailedLoginAt",
                table: "AdminUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordChangedAt",
                table: "AdminUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AdminUsers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TwoFactorEnabledAt",
                table: "AdminUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TwoFactorFailedAttemptCount",
                table: "AdminUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TwoFactorLastVerifiedAt",
                table: "AdminUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TwoFactorLockedUntil",
                table: "AdminUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecretEncrypted",
                table: "AdminUsers",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TwoFactorStatus",
                table: "AdminUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "AuthorizationVersionAtCreation",
                table: "AdminSessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "AdminSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "AdminSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "AdminSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AdminSessions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "AdminSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokeReason",
                table: "AdminSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevokedByUserId",
                table: "AdminSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStampAtCreation",
                table: "AdminSessions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AdminSessions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminLoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TwoFactorRequired = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TwoFactorSucceeded = table.Column<bool>(type: "bit", nullable: true),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminLoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureCode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordHistories_AdminUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RequestedIpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequestedUserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityNotificationOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProtectedPayload = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityNotificationOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemporarySecurityTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporarySecurityTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TwoFactorRecoveryCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwoFactorRecoveryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TwoFactorRecoveryCodes_AdminUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminSessions_DeviceId",
                table: "AdminSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminSessions_ExpiresAt",
                table: "AdminSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminSessions_RevokedAt",
                table: "AdminSessions",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginAttempts_AdminUserId",
                table: "AdminLoginAttempts",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginAttempts_AttemptedAt",
                table: "AdminLoginAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginAttempts_FailureReason",
                table: "AdminLoginAttempts",
                column: "FailureReason");

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginAttempts_IpAddress",
                table: "AdminLoginAttempts",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginAttempts_NormalizedEmail",
                table: "AdminLoginAttempts",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginAttempts_WasSuccessful",
                table: "AdminLoginAttempts",
                column: "WasSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditLogs_Action",
                table: "AuthorizationAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditLogs_ActorUserId",
                table: "AuthorizationAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditLogs_Category",
                table: "AuthorizationAuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditLogs_CorrelationId",
                table: "AuthorizationAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditLogs_CreatedAt",
                table: "AuthorizationAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditLogs_EntityType_EntityId",
                table: "AuthorizationAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditLogs_TargetUserId",
                table: "AuthorizationAuditLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_AdminUserId_CreatedAt",
                table: "PasswordHistories",
                columns: new[] { "AdminUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_AdminUserId",
                table: "PasswordResetTokens",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_ExpiresAt",
                table: "PasswordResetTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UsedAt",
                table: "PasswordResetTokens",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_AdminUserId",
                table: "SecurityEvents",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_CreatedAt",
                table: "SecurityEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_EventType",
                table: "SecurityEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_IsReviewed",
                table: "SecurityEvents",
                column: "IsReviewed");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_Severity",
                table: "SecurityEvents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityNotificationOutbox_ProcessedAt_CreatedAt",
                table: "SecurityNotificationOutbox",
                columns: new[] { "ProcessedAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TemporarySecurityTokens_AdminUserId_Purpose_ExpiresAt",
                table: "TemporarySecurityTokens",
                columns: new[] { "AdminUserId", "Purpose", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TemporarySecurityTokens_TokenHash",
                table: "TemporarySecurityTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorRecoveryCodes_AdminUserId",
                table: "TwoFactorRecoveryCodes",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorRecoveryCodes_CodeHash",
                table: "TwoFactorRecoveryCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorRecoveryCodes_RevokedAt",
                table: "TwoFactorRecoveryCodes",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TwoFactorRecoveryCodes_UsedAt",
                table: "TwoFactorRecoveryCodes",
                column: "UsedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminLoginAttempts");

            migrationBuilder.DropTable(
                name: "AuthorizationAuditLogs");

            migrationBuilder.DropTable(
                name: "PasswordHistories");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "SecurityNotificationOutbox");

            migrationBuilder.DropTable(
                name: "TemporarySecurityTokens");

            migrationBuilder.DropTable(
                name: "TwoFactorRecoveryCodes");

            migrationBuilder.DropIndex(
                name: "IX_AdminSessions_DeviceId",
                table: "AdminSessions");

            migrationBuilder.DropIndex(
                name: "IX_AdminSessions_ExpiresAt",
                table: "AdminSessions");

            migrationBuilder.DropIndex(
                name: "IX_AdminSessions_RevokedAt",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "IsTwoFactorEnabled",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "LastFailedLoginAt",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabledAt",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorFailedAttemptCount",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorLastVerifiedAt",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorLockedUntil",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecretEncrypted",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorStatus",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "AuthorizationVersionAtCreation",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "Browser",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "RevokeReason",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "RevokedByUserId",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "SecurityStampAtCreation",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AdminSessions");
        }
    }
}
