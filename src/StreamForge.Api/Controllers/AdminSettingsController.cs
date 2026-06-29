using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.UseCases.Transcriptions;

namespace StreamForge.Api.Controllers;

[ApiController]
[Route("api/v1/admin/settings/transcription")]
[Authorize(Roles = "Admin")]
public sealed class AdminSettingsController : ControllerBase
{
    private readonly GetAdminTranscriptionSettingsService _getAdminTranscriptionSettings;
    private readonly UpdateAdminTranscriptionSettingsService _updateAdminTranscriptionSettings;

    public AdminSettingsController(
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
