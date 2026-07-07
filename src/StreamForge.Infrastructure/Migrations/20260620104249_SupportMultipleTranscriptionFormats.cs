using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupportMultipleTranscriptionFormats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoTranscriptions_VideoId_Language",
                table: "VideoTranscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptions_VideoId_Language_Format",
                table: "VideoTranscriptions",
                columns: new[] { "VideoId", "Language", "Format" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoTranscriptions_VideoId_Language_Format",
                table: "VideoTranscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptions_VideoId_Language",
                table: "VideoTranscriptions",
                columns: new[] { "VideoId", "Language" },
                unique: true);
        }
    }
}
