using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedesignBookmarksAsVideoMarkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Bookmarks\";");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId_VideoId",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "AllowBookmarks",
                table: "Videos");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Bookmarks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimestampSeconds",
                table: "Bookmarks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookmarks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_VideoId",
                table: "Bookmarks",
                columns: new[] { "UserId", "VideoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_VideoId_TimestampSeconds",
                table: "Bookmarks",
                columns: new[] { "UserId", "VideoId", "TimestampSeconds" });

            migrationBuilder.AlterColumn<int>(
                name: "TimestampSeconds",
                table: "Bookmarks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookmarks",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId_VideoId",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId_VideoId_TimestampSeconds",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "TimestampSeconds",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Bookmarks");

            migrationBuilder.AddColumn<bool>(
                name: "AllowBookmarks",
                table: "Videos",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_VideoId",
                table: "Bookmarks",
                columns: new[] { "UserId", "VideoId" },
                unique: true);
        }
    }
}
