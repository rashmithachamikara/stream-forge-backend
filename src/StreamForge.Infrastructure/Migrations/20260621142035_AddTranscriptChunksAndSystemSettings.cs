using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptChunksAndSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VideoTranscriptChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TranscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StartSeconds = table.Column<double>(type: "double precision", nullable: false),
                    EndSeconds = table.Column<double>(type: "double precision", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTranscriptChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoTranscriptChunks_VideoTranscriptions_TranscriptionId",
                        column: x => x.TranscriptionId,
                        principalTable: "VideoTranscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoTranscriptChunks_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptChunks_TranscriptionId",
                table: "VideoTranscriptChunks",
                column: "TranscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptChunks_VideoId",
                table: "VideoTranscriptChunks",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptChunks_VideoId_Language",
                table: "VideoTranscriptChunks",
                columns: new[] { "VideoId", "Language" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptChunks_VideoId_StartSeconds",
                table: "VideoTranscriptChunks",
                columns: new[] { "VideoId", "StartSeconds" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "VideoTranscriptChunks");
        }
    }
}
