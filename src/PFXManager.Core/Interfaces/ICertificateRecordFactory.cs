using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

/// <summary>
/// Builds a fully populated <see cref="CertificateRecord"/> (parsed certificate data + file
/// metadata + status + best-effort identifiers) for a single file path. Shared by the scan
/// pipeline and by quarantine restore, so a file is always turned into a record the same way.
/// </summary>
public interface ICertificateRecordFactory
{
    Task<CertificateRecord> BuildAsync(string filePath, string? password, Guid scanSessionId, CancellationToken cancellationToken);
}
