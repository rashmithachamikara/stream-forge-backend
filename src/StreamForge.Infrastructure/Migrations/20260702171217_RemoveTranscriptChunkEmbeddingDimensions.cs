using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTranscriptChunkEmbeddingDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddingDimensions",
                table: "VideoTranscriptChunks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmbeddingDimensions",
                table: "VideoTranscriptChunks",
                type: "integer",
                nullable: true);
        }
    }
}
