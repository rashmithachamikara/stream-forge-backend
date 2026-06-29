using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents a video with metadata and settings
/// </summary>
public class Video : BaseEntity
{
    /// <summary>
    /// Video title
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Video description
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Uploader user ID
    /// </summary>
    public Guid UploaderId { get; private set; }

    /// <summary>
    /// Category ID
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// Video visibility
    /// </summary>
    public VideoVisibility Visibility { get; private set; }

    /// <summary>
    /// Video lifecycle status
    /// </summary>
    public VideoStatus Status { get; private set; }

    /// <summary>
    /// Whether comments are allowed
    /// </summary>
    public bool AllowComments { get; private set; }

    /// <summary>
    /// Whether likes are allowed
    /// </summary>
    public bool AllowLikes { get; private set; }

    /// <summary>
    /// Autoplay setting
    /// </summary>
    public bool Autoplay { get; private set; }

    /// <summary>
    /// Loop playback setting
    /// </summary>
    public bool Loop { get; private set; }

    /// <summary>
    /// Default volume (0-100)
    /// </summary>
    public int DefaultVolume { get; private set; }

    /// <summary>
    /// Whether captions are enabled
    /// </summary>
    public bool CaptionsEnabled { get; private set; }

    /// <summary>
    /// Player theme
    /// </summary>
    public string PlayerTheme { get; private set; }

    /// <summary>
    /// Total view count (denormalized for performance)
    /// </summary>
    public long ViewCount { get; private set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public User Uploader { get; private set; } = null!;
    public Category? Category { get; private set; }
    public ICollection<VideoVersion> VideoVersions { get; private set; }
    public ICollection<VideoThumbnail> VideoThumbnails { get; private set; }
    public ICollection<VideoProcessingJob> VideoProcessingJobs { get; private set; }
    public ICollection<VideoTranscription> VideoTranscriptions { get; private set; }
    public ICollection<VideoTranscriptChunk> VideoTranscriptChunks { get; private set; }
    public ICollection<VideoTag> VideoTags { get; private set; }
    public ICollection<VideoReaction> VideoReactions { get; private set; }
    public ICollection<VideoComment> VideoComments { get; private set; }
    public ICollection<Bookmark> Bookmarks { get; private set; }
    public ICollection<PlaylistVideo> PlaylistVideos { get; private set; }
    public ICollection<AccessControl> AccessControls { get; private set; }
    public ICollection<Notification> Notifications { get; private set; }
    public ICollection<AnalyticsEvent> AnalyticsEvents { get; private set; }

    // Private constructor for EF Core
    private Video() : base()
    {
        Title = string.Empty;
        PlayerTheme = "default";
        VideoVersions = new List<VideoVersion>();
        VideoThumbnails = new List<VideoThumbnail>();
        VideoProcessingJobs = new List<VideoProcessingJob>();
        VideoTranscriptions = new List<VideoTranscription>();
        VideoTranscriptChunks = new List<VideoTranscriptChunk>();
        VideoTags = new List<VideoTag>();
        VideoReactions = new List<VideoReaction>();
        VideoComments = new List<VideoComment>();
        Bookmarks = new List<Bookmark>();
        PlaylistVideos = new List<PlaylistVideo>();
        AccessControls = new List<AccessControl>();
        Notifications = new List<Notification>();
        AnalyticsEvents = new List<AnalyticsEvent>();
    }

    /// <summary>
    /// Creates a new video
    /// </summary>
    public static Video Create(
        string title, 
        string? description, 
        Guid uploaderId, 
        Guid? categoryId = null,
        VideoVisibility visibility = VideoVisibility.Public,
        VideoStatus status = VideoStatus.Ready)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (uploaderId == Guid.Empty)
            throw new ArgumentException("Uploader ID is required", nameof(uploaderId));

        var video = new Video
        {
            Title = title,
            Description = description,
            UploaderId = uploaderId,
            CategoryId = categoryId,
            Visibility = visibility,
            Status = status,
            AllowComments = true,
            AllowLikes = true,
            Autoplay = false,
            Loop = false,
            DefaultVolume = 100,
            CaptionsEnabled = true,
            PlayerTheme = "default",
            ViewCount = 0,
            UpdatedAt = DateTime.UtcNow
        };

        return video;
    }

    /// <summary>
    /// Updates video metadata
    /// </summary>
    public void UpdateMetadata(string title, string? description, Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Title = title;
        Description = description;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates video visibility
    /// </summary>
    public void UpdateVisibility(VideoVisibility visibility)
    {
        Visibility = visibility;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the video as uploading.
    /// </summary>
    public void MarkAsUploading()
    {
        Status = VideoStatus.Uploading;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the video as processing.
    /// </summary>
    public void MarkAsProcessing()
    {
        Status = VideoStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the video as ready.
    /// </summary>
    public void MarkAsReady()
    {
        Status = VideoStatus.Ready;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the video as failed.
    /// </summary>
    public void MarkAsFailed()
    {
        Status = VideoStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the video as deleted.
    /// </summary>
    public void MarkAsDeleted()
    {
        Status = VideoStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates engagement settings
    /// </summary>
    public void UpdateEngagementSettings(bool allowComments, bool allowLikes)
    {
        AllowComments = allowComments;
        AllowLikes = allowLikes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates player settings
    /// </summary>
    public void UpdatePlayerSettings(bool autoplay, bool loop, int defaultVolume, bool captionsEnabled, string playerTheme)
    {
        if (defaultVolume < 0 || defaultVolume > 100)
            throw new ArgumentException("Volume must be between 0 and 100", nameof(defaultVolume));

        Autoplay = autoplay;
        Loop = loop;
        DefaultVolume = defaultVolume;
        CaptionsEnabled = captionsEnabled;
        PlayerTheme = playerTheme;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments view count
    /// </summary>
    public void IncrementViewCount()
    {
        ViewCount++;
    }
}
