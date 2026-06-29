using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.UseCases.Transcriptions;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/admin/transcription/settings")]
[Authorize(Roles = "Admin")]
public sealed class AdminTranscriptionSettingsController : ControllerBase
{
    private readonly GetAdminTranscriptionSettingsService _getAdminTranscriptionSettings;
    private readonly UpdateAdminTranscriptionSettingsService _updateAdminTranscriptionSettings;

    public AdminTranscriptionSettingsController(
        GetAdminTranscriptionSettingsService getAdminTranscriptionSettings,
        UpdateAdminTranscriptionSettingsService updateAdminTranscriptionSettings)
    {
        _getAdminTranscriptionSettings = getAdminTranscriptionSettings;
        _updateAdminTranscriptionSettings = updateAdminTranscriptionSettings;
    }

    [HttpGet]
    public async Task<ActionResult<AdminTranscriptionSettingsDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _getAdminTranscriptionSettings.Handle(cancellationToken));
    }

    [HttpPut]
    public async Task<ActionResult<AdminTranscriptionSettingsDto>> Update(
        [FromBody] UpdateAdminTranscriptionSettingsRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _updateAdminTranscriptionSettings.Handle(request, cancellationToken));
    }
}
