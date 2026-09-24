using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkContactRequestsToTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ComplaintCategoryId",
                table: "ContactRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContactReasonId",
                table: "ContactRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_ComplaintCategoryId",
                table: "ContactRequests",
                column: "ComplaintCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactRequests_ContactReasonId_CreatedAt",
                table: "ContactRequests",
                columns: new[] { "ContactReasonId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ContactRequests_ComplaintCategories_ComplaintCategoryId",
                table: "ContactRequests",
                column: "ComplaintCategoryId",
                principalTable: "ComplaintCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactRequests_ContactReasons_ContactReasonId",
                table: "ContactRequests",
                column: "ContactReasonId",
                principalTable: "ContactReasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactRequests_ComplaintCategories_ComplaintCategoryId",
                table: "ContactRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ContactRequests_ContactReasons_ContactReasonId",
                table: "ContactRequests");

            migrationBuilder.DropIndex(
                name: "IX_ContactRequests_ComplaintCategoryId",
                table: "ContactRequests");

            migrationBuilder.DropIndex(
                name: "IX_ContactRequests_ContactReasonId_CreatedAt",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "ComplaintCategoryId",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "ContactReasonId",
                table: "ContactRequests");
        }
    }
}
