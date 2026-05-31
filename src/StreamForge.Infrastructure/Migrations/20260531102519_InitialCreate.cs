using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StorageProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Configuration = table.Column<string>(type: "jsonb", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VideoCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Playlists_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UploaderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AllowComments = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowLikes = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowBookmarks = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Autoplay = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Loop = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DefaultVolume = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    CaptionsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PlayerTheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "default"),
                    ViewCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Videos_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Videos_Users_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShareToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PermissionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessControls_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessControls_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: true),
                    DurationWatched = table.Column<int>(type: "integer", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookmarks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bookmarks_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: true),
                    NotificationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistVideos",
                columns: table => new
                {
                    PlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistVideos", x => new { x.PlaylistId, x.VideoId });
                    table.ForeignKey(
                        name: "FK_PlaylistVideos_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaylistVideos_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoComments_VideoComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "VideoComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideoComments_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoProcessingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoProcessingJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoProcessingJobs_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoReactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoReactions_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoTags",
                columns: table => new
                {
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTags", x => new { x.VideoId, x.TagId });
                    table.ForeignKey(
                        name: "FK_VideoTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoTags_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoThumbnails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TimestampSeconds = table.Column<int>(type: "integer", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoThumbnails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoThumbnails_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoTranscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTranscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoTranscriptions_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Resolution = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Bitrate = table.Column<int>(type: "integer", nullable: true),
                    Codec = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoVersions_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoFiles_StorageProviders_StorageProviderId",
                        column: x => x.StorageProviderId,
                        principalTable: "StorageProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideoFiles_VideoVersions_VideoVersionId",
                        column: x => x.VideoVersionId,
                        principalTable: "VideoVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControls_ShareToken",
                table: "AccessControls",
                column: "ShareToken",
                unique: true,
                filter: "\"ShareToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessControls_UserId",
                table: "AccessControls",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessControls_VideoId",
                table: "AccessControls",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessControls_VideoId_UserId",
                table: "AccessControls",
                columns: new[] { "VideoId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_CreatedAt",
                table: "AnalyticsEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_EventType",
                table: "AnalyticsEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_SessionId",
                table: "AnalyticsEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_UserId",
                table: "AnalyticsEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_UserId1",
                table: "AnalyticsEvents",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_VideoId",
                table: "AnalyticsEvents",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_VideoId_EventTime",
                table: "AnalyticsEvents",
                columns: new[] { "VideoId", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_VideoId",
                table: "Bookmarks",
                columns: new[] { "UserId", "VideoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_VideoId",
                table: "Bookmarks",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_VideoId",
                table: "Notifications",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_CreatedAt",
                table: "Playlists",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_OwnerId",
                table: "Playlists",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_Visibility",
                table: "Playlists",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistVideos_PlaylistId_OrderIndex",
                table: "PlaylistVideos",
                columns: new[] { "PlaylistId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistVideos_PlaylistId_VideoId",
                table: "PlaylistVideos",
                columns: new[] { "PlaylistId", "VideoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistVideos_VideoId",
                table: "PlaylistVideos",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageProviders_IsActive",
                table: "StorageProviders",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_StorageProviders_IsDefault",
                table: "StorageProviders",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_StorageProviders_Type",
                table: "StorageProviders",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UsageCount",
                table: "Tags",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_VideoComments_CreatedAt",
                table: "VideoComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VideoComments_ParentCommentId",
                table: "VideoComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoComments_UserId",
                table: "VideoComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoComments_VideoId",
                table: "VideoComments",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoFiles_Checksum",
                table: "VideoFiles",
                column: "Checksum");

            migrationBuilder.CreateIndex(
                name: "IX_VideoFiles_FilePath",
                table: "VideoFiles",
                column: "FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_VideoFiles_StorageProviderId",
                table: "VideoFiles",
                column: "StorageProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoFiles_VideoVersionId",
                table: "VideoFiles",
                column: "VideoVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProcessingJobs_Status",
                table: "VideoProcessingJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProcessingJobs_VideoId",
                table: "VideoProcessingJobs",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProcessingJobs_VideoId_JobType",
                table: "VideoProcessingJobs",
                columns: new[] { "VideoId", "JobType" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoReactions_UserId",
                table: "VideoReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoReactions_VideoId_ReactionType",
                table: "VideoReactions",
                columns: new[] { "VideoId", "ReactionType" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoReactions_VideoId_UserId",
                table: "VideoReactions",
                columns: new[] { "VideoId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_CategoryId",
                table: "Videos",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_CreatedAt",
                table: "Videos",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_UploaderId",
                table: "Videos",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ViewCount",
                table: "Videos",
                column: "ViewCount");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_Visibility",
                table: "Videos",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTags_TagId",
                table: "VideoTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoThumbnails_VideoId",
                table: "VideoThumbnails",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoThumbnails_VideoId_IsDefault",
                table: "VideoThumbnails",
                columns: new[] { "VideoId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptions_VideoId",
                table: "VideoTranscriptions",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptions_VideoId_Language",
                table: "VideoTranscriptions",
                columns: new[] { "VideoId", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoVersions_Resolution",
                table: "VideoVersions",
                column: "Resolution");

            migrationBuilder.CreateIndex(
                name: "IX_VideoVersions_VideoId",
                table: "VideoVersions",
                column: "VideoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessControls");

            migrationBuilder.DropTable(
                name: "AnalyticsEvents");

            migrationBuilder.DropTable(
                name: "Bookmarks");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PlaylistVideos");

            migrationBuilder.DropTable(
                name: "VideoComments");

            migrationBuilder.DropTable(
                name: "VideoFiles");

            migrationBuilder.DropTable(
                name: "VideoProcessingJobs");

            migrationBuilder.DropTable(
                name: "VideoReactions");

            migrationBuilder.DropTable(
                name: "VideoTags");

            migrationBuilder.DropTable(
                name: "VideoThumbnails");

            migrationBuilder.DropTable(
                name: "VideoTranscriptions");

            migrationBuilder.DropTable(
                name: "Playlists");

            migrationBuilder.DropTable(
                name: "StorageProviders");

            migrationBuilder.DropTable(
                name: "VideoVersions");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Videos");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
