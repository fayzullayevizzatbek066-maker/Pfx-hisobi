namespace PFXManager.Core.Models;

public sealed class ScanSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
    public bool WasCancelled { get; set; }

    public long FilesChecked { get; set; }
    public long PfxFound { get; set; }
    public long ExpiredCount { get; set; }
    public long ErrorCount { get; set; }

    public IReadOnlyList<string> RootsScanned { get; set; } = Array.Empty<string>();
}
