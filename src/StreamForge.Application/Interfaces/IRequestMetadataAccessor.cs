namespace StreamForge.Application.Interfaces;

public interface IRequestMetadataAccessor
{
    string? IpAddress { get; }

    string? UserAgent { get; }
}
