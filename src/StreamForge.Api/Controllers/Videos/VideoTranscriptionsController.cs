using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.TranscriptIntelligence;
using StreamForge.Application.UseCases.Transcriptions;

namespace StreamForge.Api.Controllers.Videos;

/// <summary>
/// Exposes per-video transcription metadata, artifacts, transcript search, and grounded question-answering endpoints.
/// </summary>
[ApiController]
[Route("api/v1/videos/{videoId:guid}/transcriptions")]
public sealed class VideoTranscriptionsController : ControllerBase
{
    private readonly ListVideoTranscriptionsService _listVideoTranscriptions;
    private readonly ListVideoTranscriptionJobsService _listVideoTranscriptionJobs;
    private readonly GetVideoTranscriptionStatusService _getVideoTranscriptionStatus;
    private readonly GetVideoTranscriptionFileService _getVideoTranscriptionFile;
    private readonly SearchVideoTranscriptService _searchVideoTranscript;
    private readonly SearchVideoTranscriptSemanticService _searchVideoTranscriptSemantic;
    private readonly SearchVideoTranscriptHybridService _searchVideoTranscriptHybrid;
    private readonly AskVideoQuestionService _askVideoQuestion;
    private readonly GetVideoTranscriptionChunksService _getVideoTranscriptionChunks;
    private readonly StartVideoTranscriptionService _startVideoTranscription;

    public VideoTranscriptionsController(
        ListVideoTranscriptionsService listVideoTranscriptions,
        ListVideoTranscriptionJobsService listVideoTranscriptionJobs,
        GetVideoTranscriptionStatusService getVideoTranscriptionStatus,
        GetVideoTranscriptionFileService getVideoTranscriptionFile,
        SearchVideoTranscriptService searchVideoTranscript,
        SearchVideoTranscriptSemanticService searchVideoTranscriptSemantic,
        SearchVideoTranscriptHybridService searchVideoTranscriptHybrid,
        AskVideoQuestionService askVideoQuestion,
        GetVideoTranscriptionChunksService getVideoTranscriptionChunks,
        StartVideoTranscriptionService startVideoTranscription)
    {
        _listVideoTranscriptions = listVideoTranscriptions;
        _listVideoTranscriptionJobs = listVideoTranscriptionJobs;
        _getVideoTranscriptionStatus = getVideoTranscriptionStatus;
        _getVideoTranscriptionFile = getVideoTranscriptionFile;
        _searchVideoTranscript = searchVideoTranscript;
        _searchVideoTranscriptSemantic = searchVideoTranscriptSemantic;
        _searchVideoTranscriptHybrid = searchVideoTranscriptHybrid;
        _askVideoQuestion = askVideoQuestion;
        _getVideoTranscriptionChunks = getVideoTranscriptionChunks;
        _startVideoTranscription = startVideoTranscription;
    }

    /// <summary>
    /// Lists transcription artifacts available for a video.
    /// </summary>
    /// <param name="videoId">Video identifier.</param>
    /// <param name="shareToken">Optional share token for anonymously shared videos.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcription artifacts available for the video.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<VideoTranscriptionDto>>> List(
        Guid videoId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _listVideoTranscriptions.Handle(videoId, shareToken, cancellationToken));
    }

    /// <summary>
    /// Lists grouped transcription jobs for a video.
    /// </summary>
    [HttpGet("/api/v1/videos/{videoId:guid}/transcription-jobs")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<VideoTranscriptionJobDto>>> ListJobs(
        Guid videoId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _listVideoTranscriptionJobs.Handle(videoId, shareToken, cancellationToken));
    }

    /// <summary>
    /// Gets status details for a single transcription artifact.
    /// </summary>
    [HttpGet("{transcriptionId:guid}/status")]
    [AllowAnonymous]
    public async Task<ActionResult<VideoTranscriptionDto>> GetStatus(
        Guid videoId,
        Guid transcriptionId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _getVideoTranscriptionStatus.Handle(videoId, transcriptionId, shareToken, cancellationToken));
    }

    /// <summary>
    /// Downloads or streams the stored transcription artifact content.
    /// </summary>
    [HttpGet("{transcriptionId:guid}")]
    [HttpGet("{transcriptionId:guid}/content")]
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

    /// <summary>
    /// Gets structured transcript chunks for transcript-reader experiences.
    /// </summary>
    [HttpGet("{transcriptionId:guid}/chunks")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<TranscriptChunkDto>>> GetChunks(
        Guid videoId,
        Guid transcriptionId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _getVideoTranscriptionChunks.Handle(videoId, transcriptionId, shareToken, cancellationToken));
    }

    /// <summary>
    /// Searches transcript chunks lexically within a single video.
    /// </summary>
    [HttpGet("/api/v1/videos/{videoId:guid}/transcript-search")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponseDto<TranscriptSearchResultDto>>> Search(
        Guid videoId,
        [FromQuery(Name = "q")] string query,
        [FromQuery] string? language,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _searchVideoTranscript.Handle(videoId, query, language, page, pageSize, shareToken, cancellationToken));
    }

    /// <summary>
    /// Searches transcript chunks semantically within a single video.
    /// </summary>
    [HttpGet("/api/v1/videos/{videoId:guid}/transcript-semantic-search")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponseDto<TranscriptSemanticSearchResultDto>>> SearchSemantic(
        Guid videoId,
        [FromQuery(Name = "q")] string query,
        [FromQuery] string? language,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _searchVideoTranscriptSemantic.Handle(videoId, query, language, page, pageSize, shareToken, cancellationToken));
    }

    /// <summary>
    /// Searches transcript chunks with hybrid lexical and semantic retrieval within a single video.
    /// </summary>
    [HttpGet("/api/v1/videos/{videoId:guid}/transcript-hybrid-search")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponseDto<TranscriptHybridSearchResultDto>>> SearchHybrid(
        Guid videoId,
        [FromQuery(Name = "q")] string query,
        [FromQuery] string? language,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _searchVideoTranscriptHybrid.Handle(videoId, query, language, page, pageSize, shareToken, cancellationToken));
    }

    /// <summary>
    /// Answers a grounded question about a single video using cited transcript evidence.
    /// </summary>
    [HttpPost("/api/v1/videos/{videoId:guid}/questions")]
    [AllowAnonymous]
    public async Task<ActionResult<GroundedQuestionAnswerDto>> AskQuestion(
        Guid videoId,
        [FromBody] AskVideoQuestionRequestDto request,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _askVideoQuestion.Handle(
            videoId,
            request.Question,
            request.Language,
            shareToken,
            cancellationToken));
    }

    /// <summary>
    /// Requests transcription generation for a video.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<VideoTranscriptionDto>>> RequestTranscription(
        Guid videoId,
        [FromBody] RequestVideoTranscriptionRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _startVideoTranscription.Handle(videoId, request.Language, request.OutputFormats, cancellationToken));
    }
}
