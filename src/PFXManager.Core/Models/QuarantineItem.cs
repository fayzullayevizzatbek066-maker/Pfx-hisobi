namespace PFXManager.Core.Models;

public sealed class QuarantineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CertificateRecordId { get; set; }

    public required string OriginalPath { get; set; }
    public required string QuarantinePath { get; set; }
    public required string FileName { get; set; }

    public string? Thumbprint { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? NotAfter { get; set; }

    public DateTime QuarantinedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime OriginalLastModifiedUtc { get; set; }
    public long FileSizeBytes { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>True once the item has been restored or permanently deleted; kept for history.</summary>
    public bool IsResolved { get; set; }
    public string? ResolutionKind { get; set; } // "Restored" | "Deleted"
    public DateTime? ResolvedAtUtc { get; set; }
}
