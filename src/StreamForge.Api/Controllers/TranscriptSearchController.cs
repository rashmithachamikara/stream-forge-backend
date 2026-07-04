using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.UseCases.TranscriptIntelligence;

namespace StreamForge.Api.Controllers;

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
