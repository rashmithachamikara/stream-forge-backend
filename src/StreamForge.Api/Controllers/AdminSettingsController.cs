using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.TranscriptIntelligence;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.UseCases.TranscriptIntelligence;
using StreamForge.Application.UseCases.Transcriptions;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/admin/settings")]
[Authorize(Roles = "Admin")]
public sealed class AdminSettingsController : ControllerBase
{
    private readonly GetAdminTranscriptionSettingsService _getAdminTranscriptionSettings;
    private readonly UpdateAdminTranscriptionSettingsService _updateAdminTranscriptionSettings;
    private readonly GetAdminRagSettingsService _getAdminRagSettingsService;
    private readonly UpdateAdminRagSettingsService _updateAdminRagSettingsService;

    public AdminSettingsController(
        GetAdminTranscriptionSettingsService getAdminTranscriptionSettings,
        UpdateAdminTranscriptionSettingsService updateAdminTranscriptionSettings,
        GetAdminRagSettingsService getAdminRagSettingsService,
        UpdateAdminRagSettingsService updateAdminRagSettingsService)
    {
        _getAdminTranscriptionSettings = getAdminTranscriptionSettings;
        _updateAdminTranscriptionSettings = updateAdminTranscriptionSettings;
        _getAdminRagSettingsService = getAdminRagSettingsService;
        _updateAdminRagSettingsService = updateAdminRagSettingsService;
    }

    [HttpGet("transcription")]
    public async Task<ActionResult<AdminTranscriptionSettingsDto>> GetTranscription(CancellationToken cancellationToken)
    {
        return Ok(await _getAdminTranscriptionSettings.Handle(cancellationToken));
    }

    [HttpPut("transcription")]
    public async Task<ActionResult<AdminTranscriptionSettingsDto>> UpdateTranscription(
        [FromBody] UpdateAdminTranscriptionSettingsRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updateAdminTranscriptionSettings.Handle(request, cancellationToken));
    }

    [HttpGet("rag")]
    public async Task<ActionResult<AdminRagSettingsDto>> GetRag(CancellationToken cancellationToken)
    {
        return Ok(await _getAdminRagSettingsService.Handle(cancellationToken));
    }

    [HttpPut("rag")]
    public async Task<ActionResult<AdminRagSettingsDto>> UpdateRag(
        [FromBody] UpdateAdminRagSettingsRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updateAdminRagSettingsService.Handle(request, cancellationToken));
    }
}
