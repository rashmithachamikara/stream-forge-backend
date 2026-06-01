using Microsoft.Extensions.Options;
using StreamForge.Application.Common;

namespace StreamForge.Api.Options;

public sealed class RateLimiterOptionsValidator : IValidateOptions<RateLimiterOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimiterOptions options)
    {
        if (options.PermitLimit < 1)
        {
            return ValidateOptionsResult.Fail("PermitLimit must be at least 1.");
        }

        if (options.WindowMinutes < 1)
        {
            return ValidateOptionsResult.Fail("WindowMinutes must be at least 1.");
        }

        if (options.SegmentsPerWindow < 1)
        {
            return ValidateOptionsResult.Fail("SegmentsPerWindow must be at least 1.");
        }

        if (options.UploadPermitLimit < 1)
        {
            return ValidateOptionsResult.Fail("UploadPermitLimit must be at least 1.");
        }

        if (options.UploadWindowMinutes < 1)
        {
            return ValidateOptionsResult.Fail("UploadWindowMinutes must be at least 1.");
        }

        if (options.UploadSegmentsPerWindow < 1)
        {
            return ValidateOptionsResult.Fail("UploadSegmentsPerWindow must be at least 1.");
        }

        return ValidateOptionsResult.Success;
    }
}
