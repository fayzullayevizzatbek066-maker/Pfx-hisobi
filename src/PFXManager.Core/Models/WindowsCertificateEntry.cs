using PFXManager.Core.Enums;

namespace PFXManager.Core.Models;

public enum CertStoreLocation
{
    CurrentUser,
    LocalMachine
}

/// <summary>
/// A certificate installed in the Windows Certificate Store. Deliberately distinct from
/// <see cref="CertificateRecord"/> (a file on disk) — the two are never merged in the UI or in
/// deletion logic, since removing one has no effect on the other.
/// </summary>
public sealed class WindowsCertificateEntry
{
    public required string Subject { get; init; }
    public required string Issuer { get; init; }
    public required string SerialNumber { get; init; }
    public required string Thumbprint { get; init; }
    public DateTime NotBefore { get; init; }
    public DateTime NotAfter { get; init; }
    public CertificateStatus Status { get; init; }
    public int? RemainingDays { get; init; }
    public required CertStoreLocation StoreLocation { get; init; }
    public required string StoreName { get; init; }
    public bool HasPrivateKey { get; init; }
}
