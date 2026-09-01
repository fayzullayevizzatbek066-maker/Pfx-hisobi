namespace PFXManager.Core.Models;

/// <summary>Progress snapshot reported by the file system scanner while a scan is in flight.</summary>
public sealed class ScanProgress
{
    public string CurrentDirectory { get; init; } = string.Empty;
    public long FilesChecked { get; init; }
    public long PfxFound { get; init; }
    public long ErrorCount { get; init; }
}
