using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Processing;

namespace StreamForge.Api.Controllers.Videos;

[ApiController]
[Route("api/v1/videos/{videoId:guid}")]
public sealed class VideoPlaybackController : ControllerBase
{
    private readonly GetPlaybackManifestService _getPlaybackManifest;
    private readonly GetStreamingAssetService _getStreamingAsset;
    private readonly GetVideoThumbnailService _getVideoThumbnail;

    public VideoPlaybackController(
        GetPlaybackManifestService getPlaybackManifest,
        GetStreamingAssetService getStreamingAsset,
        GetVideoThumbnailService getVideoThumbnail)
    {
        _getPlaybackManifest = getPlaybackManifest;
        _getStreamingAsset = getStreamingAsset;
        _getVideoThumbnail = getVideoThumbnail;
    }

    [HttpGet("playback/manifest")]
    [AllowAnonymous]
    public async Task<IActionResult> GetManifest(
        Guid videoId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        var file = await _getPlaybackManifest.Handle(videoId, shareToken, cancellationToken);
        return ToFileResult(file);
    }

    [HttpGet("playback/assets/{*assetPath}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAsset(
        Guid videoId,
        string assetPath,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        var file = await _getStreamingAsset.Handle(videoId, assetPath, shareToken, cancellationToken);
        return ToFileResult(file);
    }

    [HttpGet("thumbnail")]
    [AllowAnonymous]
    public async Task<IActionResult> GetThumbnail(
        Guid videoId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        var file = await _getVideoThumbnail.Handle(videoId, shareToken, cancellationToken);
        return ToFileResult(file);
    }

    private FileStreamResult ToFileResult(StoredFileDescriptor file)
    {
        if (file.Length.HasValue)
        {
            Response.ContentLength = file.Length.Value;
        }

        return File(file.Stream, file.ContentType, enableRangeProcessing: true);
    }
}
