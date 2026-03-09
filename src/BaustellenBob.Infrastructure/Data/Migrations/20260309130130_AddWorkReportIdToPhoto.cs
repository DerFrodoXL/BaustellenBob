using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaustellenBob.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkReportIdToPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkReportId",
                table: "Photos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_WorkReportId",
                table: "Photos",
                column: "WorkReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_WorkReports_WorkReportId",
                table: "Photos",
                column: "WorkReportId",
                principalTable: "WorkReports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_WorkReports_WorkReportId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_WorkReportId",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "WorkReportId",
                table: "Photos");
        }
    }
}
