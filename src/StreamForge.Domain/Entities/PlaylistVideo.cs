namespace StreamForge.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between Playlists and Videos with ordering
/// </summary>
public class PlaylistVideo
{
    /// <summary>
    /// Playlist ID
    /// </summary>
    public Guid PlaylistId { get; private set; }

    /// <summary>
    /// Video ID
    /// </summary>
    public Guid VideoId { get; private set; }

    /// <summary>
    /// Display order in the playlist
    /// </summary>
    public int OrderIndex { get; private set; }

    /// <summary>
    /// When the video was added to the playlist
    /// </summary>
    public DateTime AddedAt { get; private set; }

    // Navigation properties
    public Playlist Playlist { get; private set; } = null!;
    public Video Video { get; private set; } = null!;

    // Private constructor for EF Core
    private PlaylistVideo()
    {
        AddedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new playlist-video association
    /// </summary>
    public static PlaylistVideo Create(Guid playlistId, Guid videoId, int orderIndex)
    {
        if (playlistId == Guid.Empty)
            throw new ArgumentException("Playlist ID is required", nameof(playlistId));

        if (videoId == Guid.Empty)
            throw new ArgumentException("Video ID is required", nameof(videoId));

        if (orderIndex < 0)
            throw new ArgumentException("Order index cannot be negative", nameof(orderIndex));

        var playlistVideo = new PlaylistVideo
        {
            PlaylistId = playlistId,
            VideoId = videoId,
            OrderIndex = orderIndex,
            AddedAt = DateTime.UtcNow
        };

        return playlistVideo;
    }

    /// <summary>
    /// Updates the order index
    /// </summary>
    public void UpdateOrderIndex(int orderIndex)
    {
        if (orderIndex < 0)
            throw new ArgumentException("Order index cannot be negative", nameof(orderIndex));

        OrderIndex = orderIndex;
    }
}
