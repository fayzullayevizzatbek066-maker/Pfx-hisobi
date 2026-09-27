using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

public sealed record ScanOptions(
    IReadOnlyList<string> RootPaths,
    bool FollowReparsePoints = false,
    int MaxDegreeOfParallelism = 4);

/// <summary>
/// Recursively discovers *.pfx / *.p12 files under a set of root paths. Must never let a single
/// unreadable directory or file abort the overall scan.
/// </summary>
public interface IFileSystemScanner
{
    IAsyncEnumerable<string> ScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        Action<ScanError>? onError,
        CancellationToken cancellationToken);
}
