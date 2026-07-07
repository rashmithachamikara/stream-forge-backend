using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.UseCases.TranscriptIntelligence;

namespace StreamForge.Api.Controllers;

/// <summary>
/// Exposes cross-video transcript search and grounded question-answering endpoints.
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class TranscriptSearchController : ControllerBase
{
    private readonly SearchTranscriptSemanticAcrossVideosService _searchTranscriptSemanticAcrossVideos;
    private readonly SearchTranscriptHybridAcrossVideosService _searchTranscriptHybridAcrossVideos;
    private readonly AskQuestionAcrossVideosService _askQuestionAcrossVideos;

    public TranscriptSearchController(
        SearchTranscriptSemanticAcrossVideosService searchTranscriptSemanticAcrossVideos,
        SearchTranscriptHybridAcrossVideosService searchTranscriptHybridAcrossVideos,
        AskQuestionAcrossVideosService askQuestionAcrossVideos)
    {
        _searchTranscriptSemanticAcrossVideos = searchTranscriptSemanticAcrossVideos;
        _searchTranscriptHybridAcrossVideos = searchTranscriptHybridAcrossVideos;
        _askQuestionAcrossVideos = askQuestionAcrossVideos;
    }

    /// <summary>
    /// Searches transcript chunks semantically across the caller's authorized video scope.
    /// </summary>
    /// <param name="query">Natural-language search query passed as <c>q</c>.</param>
    /// <param name="language">Optional language filter applied before ranking.</param>
    /// <param name="videoIds">Optional explicit video scope intersected with authorized videos.</param>
    /// <param name="page">Requested result page.</param>
    /// <param name="pageSize">Requested page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged set of ranked semantic transcript matches.</returns>
    [HttpGet("transcript-semantic-search")]
    [Authorize]
    public async Task<ActionResult<PagedResponseDto<CrossVideoTranscriptSemanticSearchResultDto>>> SearchSemanticAcrossVideos(
        [FromQuery(Name = "q")] string query,
        [FromQuery] string? language,
        [FromQuery] Guid[]? videoIds,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        return Ok(await _searchTranscriptSemanticAcrossVideos.Handle(
            query,
            language,
            videoIds,
            page,
            pageSize,
            cancellationToken));
    }

    /// <summary>
    /// Searches transcript chunks with hybrid lexical and semantic retrieval across authorized videos.
    /// </summary>
    /// <param name="query">Search query passed as <c>q</c>.</param>
    /// <param name="language">Optional language filter applied before ranking.</param>
    /// <param name="videoIds">Optional explicit video scope intersected with authorized videos.</param>
    /// <param name="page">Requested result page.</param>
    /// <param name="pageSize">Requested page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged set of ranked hybrid transcript matches.</returns>
    [HttpGet("transcript-hybrid-search")]
    [Authorize]
    public async Task<ActionResult<PagedResponseDto<CrossVideoTranscriptHybridSearchResultDto>>> SearchHybridAcrossVideos(
        [FromQuery(Name = "q")] string query,
        [FromQuery] string? language,
        [FromQuery] Guid[]? videoIds,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        return Ok(await _searchTranscriptHybridAcrossVideos.Handle(
            query,
            language,
            videoIds,
            page,
            pageSize,
            cancellationToken));
    }

    /// <summary>
    /// Answers a grounded question across the caller's authorized video scope using cited transcript evidence.
    /// </summary>
    /// <param name="request">Question payload and optional explicit video scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A grounded answer with supporting transcript citations.</returns>
    [HttpPost("questions")]
    [Authorize]
    public async Task<ActionResult<GroundedQuestionAnswerDto>> AskAcrossVideos(
        [FromBody] AskQuestionAcrossVideosRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _askQuestionAcrossVideos.Handle(
            request.Question,
            request.Language,
            request.VideoIds,
            cancellationToken));
    }
}
