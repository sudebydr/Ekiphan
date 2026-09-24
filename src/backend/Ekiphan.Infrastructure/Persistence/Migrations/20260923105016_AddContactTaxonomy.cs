using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactReasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsComplaintReason = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplaintCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactReasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintCategories_ContactReasons_ContactReasonId",
                        column: x => x.ContactReasonId,
                        principalTable: "ContactReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintCategories_ContactReasonId_Name",
                table: "ComplaintCategories",
                columns: new[] { "ContactReasonId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintCategories_IsActive_SortOrder",
                table: "ComplaintCategories",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactReasons_IsActive_SortOrder",
                table: "ContactReasons",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactReasons_IsComplaintReason",
                table: "ContactReasons",
                column: "IsComplaintReason",
                unique: true,
                filter: "[IsComplaintReason] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ContactReasons_Name",
                table: "ContactReasons",
                column: "Name",
                unique: true);

            migrationBuilder.InsertData(
                table: "ContactReasons",
                columns: new[]
                {
                    "Id", "Name", "SortOrder", "IsActive",
                    "IsComplaintReason", "CreatedAt", "UpdatedAt"
                },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Müşteri Şikayeti", 0, true, true, new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero) },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Genel Bilgi", 1, true, false, new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero) },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Satış", 2, true, false, new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero) },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Merkez", 3, true, false, new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero) },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Fabrika ve Depo", 4, true, false, new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplaintCategories");

            migrationBuilder.DropTable(
                name: "ContactReasons");
        }
    }
}
