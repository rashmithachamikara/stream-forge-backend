using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.DTOs.Uploads;
using StreamForge.Application.UseCases.Uploads;
using StreamForge.Application.UseCases.Uploads.CreateSession;

namespace StreamForge.Api.Controllers.Uploads;

[ApiController]
[Route("api/v1/uploads/sessions")]
[Authorize]
public sealed class UploadSessionsController : ControllerBase
{
    private readonly CreateUploadSessionService _createUploadSession;
    private readonly GetUploadTargetService _getUploadTarget;
    private readonly UploadPartService _uploadPart;
    private readonly CompleteUploadSessionService _completeUploadSession;

    public UploadSessionsController(
        CreateUploadSessionService createUploadSession,
        GetUploadTargetService getUploadTarget,
        UploadPartService uploadPart,
        CompleteUploadSessionService completeUploadSession)
    {
        _createUploadSession = createUploadSession;
        _getUploadTarget = getUploadTarget;
        _uploadPart = uploadPart;
        _completeUploadSession = completeUploadSession;
    }

    [HttpPost]
    public async Task<ActionResult<CreateUploadSessionResponseDto>> CreateSession(
        [FromBody] CreateUploadSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateUploadSessionRequest(
            request.Title,
            request.Description,
            request.TotalSize,
            request.ContentType,
            request.CategoryId,
            request.Visibility,
            request.TagIds);

        return Ok(await _createUploadSession.Handle(command, cancellationToken));
    }

    [HttpGet("{sessionId:guid}/target")]
    public async Task<ActionResult<UploadTargetDto>> GetUploadTarget(
        Guid sessionId,
        [FromQuery] int partNumber,
        [FromQuery] long partSize,
        CancellationToken cancellationToken)
    {
        var command = new GetUploadTargetCommand(sessionId, partNumber, partSize);
        return Ok(await _getUploadTarget.Handle(command, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/parts/{partNumber:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadPartResponseDto>> UploadPart(
        Guid sessionId,
        int partNumber,
        [FromForm] UploadPartFormRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();
        var command = new UploadPartCommand(
            sessionId,
            partNumber,
            stream,
            request.File.FileName,
            request.File.Length,
            request.Checksum);

        return Ok(await _uploadPart.Handle(command, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/complete")]
    public async Task<ActionResult<CompleteUploadSessionResponseDto>> CompleteSession(
        Guid sessionId,
        [FromBody] CompleteUploadSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteUploadSessionCommand(sessionId, request.FileName);
        return Ok(await _completeUploadSession.Handle(command, cancellationToken));
    }
}
