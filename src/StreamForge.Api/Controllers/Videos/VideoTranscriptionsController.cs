using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Transcriptions;

namespace StreamForge.Api.Controllers.Videos;

[ApiController]
[Route("api/v1/videos/{videoId:guid}/transcriptions")]
public sealed class VideoTranscriptionsController : ControllerBase
{
    private readonly ListVideoTranscriptionsService _listVideoTranscriptions;
    private readonly GetVideoTranscriptionFileService _getVideoTranscriptionFile;
    private readonly StartVideoTranscriptionService _startVideoTranscription;

    public VideoTranscriptionsController(
        ListVideoTranscriptionsService listVideoTranscriptions,
        GetVideoTranscriptionFileService getVideoTranscriptionFile,
        StartVideoTranscriptionService startVideoTranscription)
    {
        _listVideoTranscriptions = listVideoTranscriptions;
        _getVideoTranscriptionFile = getVideoTranscriptionFile;
        _startVideoTranscription = startVideoTranscription;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<VideoTranscriptionDto>>> List(
        Guid videoId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _listVideoTranscriptions.Handle(videoId, shareToken, cancellationToken));
    }

    [HttpGet("{transcriptionId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(
        Guid videoId,
        Guid transcriptionId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        var file = await _getVideoTranscriptionFile.Handle(videoId, transcriptionId, shareToken, cancellationToken);
        if (file.Length.HasValue)
        {
            Response.ContentLength = file.Length.Value;
        }

        return File(file.Stream, file.ContentType, enableRangeProcessing: true);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<VideoTranscriptionDto>>> RequestTranscription(
        Guid videoId,
        [FromBody] RequestVideoTranscriptionRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _startVideoTranscription.Handle(videoId, request.Language, cancellationToken));
    }
}
