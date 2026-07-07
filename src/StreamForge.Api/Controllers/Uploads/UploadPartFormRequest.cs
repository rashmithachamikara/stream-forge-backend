using Microsoft.AspNetCore.Mvc;

namespace StreamForge.Api.Controllers.Uploads;

/// <summary>
/// Multipart form payload for upload part requests.
/// </summary>
public sealed class UploadPartFormRequest
{
    /// <summary>
    /// Multipart file payload for the uploaded chunk.
    /// </summary>
    [FromForm(Name = "file")]
    public required IFormFile File { get; init; }

    /// <summary>
    /// Client-supplied checksum used to validate the uploaded chunk.
    /// </summary>
    [FromForm(Name = "checksum")]
    public required string Checksum { get; init; }
}
