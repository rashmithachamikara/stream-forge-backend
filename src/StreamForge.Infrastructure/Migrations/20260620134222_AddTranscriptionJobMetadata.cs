using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptionJobMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "VideoTranscriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "VideoTranscriptions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "VideoTranscriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerJobId",
                table: "VideoTranscriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptions_CorrelationId",
                table: "VideoTranscriptions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptions_WorkerJobId",
                table: "VideoTranscriptions",
                column: "WorkerJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoTranscriptions_CorrelationId",
                table: "VideoTranscriptions");

            migrationBuilder.DropIndex(
                name: "IX_VideoTranscriptions_WorkerJobId",
                table: "VideoTranscriptions");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "VideoTranscriptions");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "VideoTranscriptions");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "VideoTranscriptions");

            migrationBuilder.DropColumn(
                name: "WorkerJobId",
                table: "VideoTranscriptions");
        }
    }
}
