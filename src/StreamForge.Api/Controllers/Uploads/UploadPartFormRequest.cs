using Microsoft.AspNetCore.Mvc;

namespace StreamForge.Api.Controllers.Uploads;

/// <summary>
/// Multipart form payload for upload part requests.
/// </summary>
public sealed class UploadPartFormRequest
{
    [FromForm(Name = "file")]
    public required IFormFile File { get; init; }

    [FromForm(Name = "checksum")]
    public required string Checksum { get; init; }
}