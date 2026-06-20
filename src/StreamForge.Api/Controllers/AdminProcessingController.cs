using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.UseCases.Transcriptions;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/admin/processing/transcription-jobs")]
[Authorize(Roles = "Admin")]
public sealed class AdminProcessingController : ControllerBase
{
    private readonly ListAdminTranscriptionJobsService _listAdminTranscriptionJobs;
    private readonly GetAdminTranscriptionJobService _getAdminTranscriptionJob;
    private readonly RetryAdminTranscriptionJobService _retryAdminTranscriptionJob;
    private readonly ResyncAdminTranscriptionJobService _resyncAdminTranscriptionJob;

    public AdminProcessingController(
        ListAdminTranscriptionJobsService listAdminTranscriptionJobs,
        GetAdminTranscriptionJobService getAdminTranscriptionJob,
        RetryAdminTranscriptionJobService retryAdminTranscriptionJob,
        ResyncAdminTranscriptionJobService resyncAdminTranscriptionJob)
    {
        _listAdminTranscriptionJobs = listAdminTranscriptionJobs;
        _getAdminTranscriptionJob = getAdminTranscriptionJob;
        _retryAdminTranscriptionJob = retryAdminTranscriptionJob;
        _resyncAdminTranscriptionJob = resyncAdminTranscriptionJob;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VideoTranscriptionJobDto>>> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        return Ok(await _listAdminTranscriptionJobs.Handle(status, cancellationToken));
    }

    [HttpGet("{jobKey}")]
    public async Task<ActionResult<VideoTranscriptionJobDto>> Get(
        string jobKey,
        CancellationToken cancellationToken)
    {
        return Ok(await _getAdminTranscriptionJob.Handle(jobKey, cancellationToken));
    }

    [HttpPost("{jobKey}/retry")]
    public async Task<ActionResult<VideoTranscriptionJobDto>> Retry(
        string jobKey,
        CancellationToken cancellationToken)
    {
        return Ok(await _retryAdminTranscriptionJob.Handle(jobKey, cancellationToken));
    }

    [HttpPost("{jobKey}/resync")]
    public async Task<ActionResult<VideoTranscriptionJobDto>> Resync(
        string jobKey,
        CancellationToken cancellationToken)
    {
        return Ok(await _resyncAdminTranscriptionJob.Handle(jobKey, cancellationToken));
    }
}
