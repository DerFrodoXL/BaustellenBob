using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaustellenBob.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoreImagesInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureContentType",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfilePictureData",
                table: "Users",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                table: "Tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoData",
                table: "Tenants",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileContentType",
                table: "Photos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "Photos",
                type: "bytea",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "LogoContentType", "LogoData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "LogoContentType", "LogoData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                columns: new[] { "ProfilePictureContentType", "ProfilePictureData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020"),
                columns: new[] { "ProfilePictureContentType", "ProfilePictureData" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureContentType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfilePictureData",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LogoContentType",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LogoData",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FileContentType",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "FileData",
                table: "Photos");
        }
    }
}
