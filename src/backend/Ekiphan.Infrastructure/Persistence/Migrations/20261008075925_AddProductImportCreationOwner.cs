using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ekiphan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImportCreationOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByImportJobId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedByImportJobId",
                table: "Products",
                column: "CreatedByImportJobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ImportJobs_CreatedByImportJobId",
                table: "Products",
                column: "CreatedByImportJobId",
                principalTable: "ImportJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ImportJobs_CreatedByImportJobId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CreatedByImportJobId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedByImportJobId",
                table: "Products");
        }
    }
}
