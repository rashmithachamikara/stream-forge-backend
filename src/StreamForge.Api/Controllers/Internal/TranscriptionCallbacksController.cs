using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StreamForge.Application.Common;
using StreamForge.Application.DTOs.Transcriptions;
using StreamForge.Application.UseCases.Transcriptions;

namespace StreamForge.Api.Controllers.Internal;

/// <summary>
/// Receives authenticated callbacks from the transcription worker.
/// </summary>
[ApiController]
[Route("internal/transcriptions")]
public sealed class TranscriptionCallbacksController : ControllerBase
{
    private readonly CompleteVideoTranscriptionCallbackService _completeVideoTranscriptionCallback;
    private readonly TranscriptionOptions _options;

    public TranscriptionCallbacksController(
        CompleteVideoTranscriptionCallbackService completeVideoTranscriptionCallback,
        IOptions<TranscriptionOptions> options)
    {
        _completeVideoTranscriptionCallback = completeVideoTranscriptionCallback;
        _options = options.Value;
    }

    /// <summary>
    /// Accepts a transcription worker callback and finalizes transcription state.
    /// </summary>
    [HttpPost("callback")]
    public async Task<IActionResult> Callback(
        [FromBody] TranscriptionCallbackRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(Request))
        {
            return Unauthorized();
        }

        await _completeVideoTranscriptionCallback.Handle(request, cancellationToken);
        return NoContent();
    }

    private bool IsAuthorized(HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.WorkerCallbackSecret))
        {
            return true;
        }

        if (request.Headers.Authorization.Count > 0)
        {
            var authorization = request.Headers.Authorization.ToString();
            const string bearerPrefix = "Bearer ";
            if (authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var token = authorization[bearerPrefix.Length..].Trim();
                if (string.Equals(token, _options.WorkerCallbackSecret, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        if (request.Headers.TryGetValue(_options.WorkerCallbackAuthHeader, out var values))
        {
            return string.Equals(values.ToString(), _options.WorkerCallbackSecret, StringComparison.Ordinal);
        }

        return false;
    }
}
