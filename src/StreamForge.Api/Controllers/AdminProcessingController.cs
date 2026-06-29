using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Content;
using StreamForge.Application.DTOs.Processing;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.UseCases.Processing;
using StreamForge.Application.UseCases.Transcriptions;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/admin/processing")]
[Authorize(Roles = "Admin")]
public sealed class AdminProcessingController : ControllerBase
{
    private readonly ListAdminTranscriptionJobsService _listAdminTranscriptionJobs;
    private readonly GetAdminTranscriptionJobService _getAdminTranscriptionJob;
    private readonly RetryAdminTranscriptionJobService _retryAdminTranscriptionJob;
    private readonly ResyncAdminTranscriptionJobService _resyncAdminTranscriptionJob;
    private readonly ListAdminVideoProcessingJobsService _listAdminVideoProcessingJobs;
    private readonly GetAdminVideoProcessingJobService _getAdminVideoProcessingJob;
    private readonly RetryAdminVideoProcessingJobService _retryAdminVideoProcessingJob;
    private readonly ResyncAdminVideoProcessingJobService _resyncAdminVideoProcessingJob;

    public AdminProcessingController(
        ListAdminTranscriptionJobsService listAdminTranscriptionJobs,
        GetAdminTranscriptionJobService getAdminTranscriptionJob,
        RetryAdminTranscriptionJobService retryAdminTranscriptionJob,
        ResyncAdminTranscriptionJobService resyncAdminTranscriptionJob,
        ListAdminVideoProcessingJobsService listAdminVideoProcessingJobs,
        GetAdminVideoProcessingJobService getAdminVideoProcessingJob,
        RetryAdminVideoProcessingJobService retryAdminVideoProcessingJob,
        ResyncAdminVideoProcessingJobService resyncAdminVideoProcessingJob)
    {
        _listAdminTranscriptionJobs = listAdminTranscriptionJobs;
        _getAdminTranscriptionJob = getAdminTranscriptionJob;
        _retryAdminTranscriptionJob = retryAdminTranscriptionJob;
        _resyncAdminTranscriptionJob = resyncAdminTranscriptionJob;
        _listAdminVideoProcessingJobs = listAdminVideoProcessingJobs;
        _getAdminVideoProcessingJob = getAdminVideoProcessingJob;
        _retryAdminVideoProcessingJob = retryAdminVideoProcessingJob;
        _resyncAdminVideoProcessingJob = resyncAdminVideoProcessingJob;
    }

    [HttpGet("transcription-jobs")]
    public async Task<ActionResult<PagedResponseDto<AdminTranscriptionJobDto>>> ListTranscriptionJobs(
        [FromQuery] AdminTranscriptionJobsQueryDto query,
        CancellationToken cancellationToken)
    {
        return Ok(await _listAdminTranscriptionJobs.Handle(query, cancellationToken));
    }

    [HttpGet("transcription-jobs/{jobKey}")]
    public async Task<ActionResult<AdminTranscriptionJobDto>> GetTranscriptionJob(
        string jobKey,
        CancellationToken cancellationToken)
    {
        return Ok(await _getAdminTranscriptionJob.Handle(jobKey, cancellationToken));
    }

    [HttpPost("transcription-jobs/{jobKey}/retry")]
    public async Task<ActionResult<AdminTranscriptionJobDto>> RetryTranscriptionJob(
        string jobKey,
        CancellationToken cancellationToken)
    {
        return Ok(await _retryAdminTranscriptionJob.Handle(jobKey, cancellationToken));
    }

    [HttpPost("transcription-jobs/{jobKey}/resync")]
    public async Task<ActionResult<AdminTranscriptionJobDto>> ResyncTranscriptionJob(
        string jobKey,
        CancellationToken cancellationToken)
    {
        return Ok(await _resyncAdminTranscriptionJob.Handle(jobKey, cancellationToken));
    }

    [HttpGet("video-jobs")]
    public async Task<ActionResult<PagedResponseDto<AdminVideoProcessingJobDto>>> ListVideoJobs(
        [FromQuery] AdminVideoProcessingJobsQueryDto query,
        CancellationToken cancellationToken)
    {
        return Ok(await _listAdminVideoProcessingJobs.Handle(query, cancellationToken));
    }

    [HttpGet("video-jobs/{jobKey}")]
    public async Task<ActionResult<AdminVideoProcessingJobDto>> GetVideoJob(
        string jobKey,
        CancellationToken cancellationToken)
    {
        return Ok(await _getAdminVideoProcessingJob.Handle(jobKey, cancellationToken));
    }

    [HttpPost("video-jobs/{jobKey}/retry")]
    public async Task<ActionResult<AdminVideoProcessingJobDto>> RetryVideoJob(
        string jobKey,
        CancellationToken cancellationToken)
    {
        return Ok(await _retryAdminVideoProcessingJob.Handle(jobKey, cancellationToken));
    }

    [HttpPost("video-jobs/{jobKey}/resync")]
    public async Task<ActionResult<AdminVideoProcessingJobDto>> ResyncVideoJob(
        string jobKey,
        CancellationToken cancellationToken)
    {
        return Ok(await _resyncAdminVideoProcessingJob.Handle(jobKey, cancellationToken));
    }
}
