using PFXManager.Core.Enums;

namespace PFXManager.Core.Models;

/// <summary>
/// A single discovered .pfx/.p12 file together with parsed certificate metadata (when readable)
/// and file system metadata. This is the unit persisted in CertificateRecords and shown in the
/// PFX Files grid.
/// </summary>
public sealed class CertificateRecord
{
    /// <summary>Stable identifier. Assigned on first discovery, preserved across re-scans of the same path.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // --- File metadata (always available, even for unreadable files) ---
    public required string FullPath { get; set; }
    public string FileName => Path.GetFileName(FullPath);
    public string Extension => Path.GetExtension(FullPath);
    public string Directory => Path.GetDirectoryName(FullPath) ?? string.Empty;
    public string Drive { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedTimeUtc { get; set; }
    public DateTime LastModifiedTimeUtc { get; set; }

    // --- Certificate metadata (populated only when parsing succeeds) ---
    public string? Subject { get; set; }
    public string? CommonName { get; set; }
    public string? Organization { get; set; }
    public string? OrganizationalUnit { get; set; }
    public string? Issuer { get; set; }
    public string? SerialNumber { get; set; }
    public string? Thumbprint { get; set; }
    public DateTime? NotBefore { get; set; }
    public DateTime? NotAfter { get; set; }
    public bool HasPrivateKey { get; set; }
    public string? SignatureAlgorithm { get; set; }
    public string? FriendlyName { get; set; }
    public int? CertificateVersion { get; set; }
    public string? KeyAlgorithm { get; set; }

    // --- Extracted local identifiers (best-effort, never authoritative for deletion decisions) ---
    public string? RawSubject { get; set; }
    public string? Stir { get; set; }
    public string? Pinfl { get; set; }
    public string? OwnerDisplayName { get; set; }

    // --- Derived / computed ---
    public CertificateStatus Status { get; set; } = CertificateStatus.ReadError;
    public int? RemainingDays { get; set; }
    public string? ReadErrorMessage { get; set; }
    public bool IsPasswordProtected { get; set; }

    // --- Duplicate detection ---
    public Guid? DuplicateGroupId { get; set; }

    public DateTime DiscoveredAtUtc { get; set; } = DateTime.UtcNow;
    public Guid ScanSessionId { get; set; }
}
