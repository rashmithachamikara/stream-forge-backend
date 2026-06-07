using StreamForge.Application.Interfaces;

namespace StreamForge.Api.Authentication;

public sealed class RequestMetadataAccessor : IRequestMetadataAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestMetadataAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
