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

    public TranscriptSearchController(
        SearchTranscriptSemanticAcrossVideosService searchTranscriptSemanticAcrossVideos)
    {
        _searchTranscriptSemanticAcrossVideos = searchTranscriptSemanticAcrossVideos;
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
}
