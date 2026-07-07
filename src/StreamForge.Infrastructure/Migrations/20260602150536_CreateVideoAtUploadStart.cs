using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateVideoAtUploadStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "VideoDescription",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "VideoTitle",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "VideoVisibility",
                table: "UploadSessions");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Videos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Ready");

            migrationBuilder.Sql("DELETE FROM \"UploadSessionParts\" WHERE \"UploadSessionId\" IN (SELECT \"Id\" FROM \"UploadSessions\" WHERE \"VideoId\" IS NULL);");
            migrationBuilder.Sql("DELETE FROM \"UploadSessions\" WHERE \"VideoId\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "VideoId",
                table: "UploadSessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_Status",
                table: "Videos",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_UploadSessions_Videos_VideoId",
                table: "UploadSessions",
                column: "VideoId",
                principalTable: "Videos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UploadSessions_Videos_VideoId",
                table: "UploadSessions");

            migrationBuilder.DropIndex(
                name: "IX_Videos_Status",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Videos");

            migrationBuilder.AlterColumn<Guid>(
                name: "VideoId",
                table: "UploadSessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "UploadSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoDescription",
                table: "UploadSessions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoTitle",
                table: "UploadSessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VideoVisibility",
                table: "UploadSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
