namespace PFXManager.Core.Models;

/// <summary>A group of CertificateRecords that represent copies of the same certificate identity.</summary>
public sealed class DuplicateGroup
{
    public Guid GroupId { get; init; } = Guid.NewGuid();
    public required string Thumbprint { get; init; }
    public required IReadOnlyList<CertificateRecord> Copies { get; init; }
    public int CopyCount => Copies.Count;
}
