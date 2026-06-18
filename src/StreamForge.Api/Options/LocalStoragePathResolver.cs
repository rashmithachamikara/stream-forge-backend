using StreamForge.Application.Common;

namespace StreamForge.Api.Options;

internal static class LocalStoragePathResolver
{
    private const string RepoMarkerFile = "StreamForge.sln";

    public static string ResolveEffectiveUploadStorageRoot(
        string contentRootPath,
        LocalStorageOptions localStorageOptions)
    {
        return ResolveUploadsRoot(contentRootPath, localStorageOptions);
    }

    public static string ResolveUploadsRoot(string contentRootPath, LocalStorageOptions localStorageOptions)
    {
        var sharedRoot = ResolveSharedRoot(contentRootPath, localStorageOptions.RootPath);
        return ResolveUnderBase(sharedRoot, localStorageOptions.UploadsRelativePath);
    }

    public static string ResolveTranscriptionOutputRoot(string contentRootPath, LocalStorageOptions localStorageOptions)
    {
        var sharedRoot = ResolveSharedRoot(contentRootPath, localStorageOptions.RootPath);
        return ResolveUnderBase(sharedRoot, localStorageOptions.TranscriptionOutputRelativePath);
    }

    private static string ResolveSharedRoot(string contentRootPath, string? configuredRootPath)
    {
        var anchorRoot = FindRepoRoot(contentRootPath) ?? Path.GetFullPath(contentRootPath);

        if (string.IsNullOrWhiteSpace(configuredRootPath))
        {
            return Path.GetFullPath(Path.Combine(anchorRoot, "data"));
        }

        return Path.IsPathRooted(configuredRootPath)
            ? Path.GetFullPath(configuredRootPath)
            : Path.GetFullPath(Path.Combine(anchorRoot, configuredRootPath));
    }

    private static string ResolveUnderBase(string basePath, string configuredPath)
    {
        return Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(basePath, configuredPath));
    }

    private static string? FindRepoRoot(string startPath)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        while (current is not null)
        {
            var markerPath = Path.Combine(current.FullName, RepoMarkerFile);
            if (File.Exists(markerPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}
