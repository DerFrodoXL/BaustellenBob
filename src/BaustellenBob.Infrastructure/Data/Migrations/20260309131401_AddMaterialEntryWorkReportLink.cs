using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaustellenBob.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialEntryWorkReportLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInvoiced",
                table: "MaterialEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkReportId",
                table: "MaterialEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialEntries_WorkReportId",
                table: "MaterialEntries",
                column: "WorkReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialEntries_WorkReports_WorkReportId",
                table: "MaterialEntries",
                column: "WorkReportId",
                principalTable: "WorkReports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialEntries_WorkReports_WorkReportId",
                table: "MaterialEntries");

            migrationBuilder.DropIndex(
                name: "IX_MaterialEntries_WorkReportId",
                table: "MaterialEntries");

            migrationBuilder.DropColumn(
                name: "IsInvoiced",
                table: "MaterialEntries");

            migrationBuilder.DropColumn(
                name: "WorkReportId",
                table: "MaterialEntries");
        }
    }
}
