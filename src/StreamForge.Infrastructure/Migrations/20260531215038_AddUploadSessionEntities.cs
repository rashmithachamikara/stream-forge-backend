using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadSessionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedSize = table.Column<long>(type: "bigint", nullable: false),
                    StorageProviderType = table.Column<int>(type: "integer", nullable: false),
                    TemporaryStoragePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    VideoTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VideoDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VideoVisibility = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadSessionParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessionParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadSessionParts_UploadSessions_UploadSessionId",
                        column: x => x.UploadSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessionParts_IsComplete",
                table: "UploadSessionParts",
                column: "IsComplete");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessionParts_SessionId_PartNumber",
                table: "UploadSessionParts",
                columns: new[] { "UploadSessionId", "PartNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessionParts_UploadSessionId",
                table: "UploadSessionParts",
                column: "UploadSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_ExpiresAt",
                table: "UploadSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_Status",
                table: "UploadSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_UserId",
                table: "UploadSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_VideoId",
                table: "UploadSessions",
                column: "VideoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadSessionParts");

            migrationBuilder.DropTable(
                name: "UploadSessions");
        }
    }
}
