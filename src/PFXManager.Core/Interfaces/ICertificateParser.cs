using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

/// <summary>
/// Parses a single .pfx/.p12 file. Must never throw for password-protected or corrupted input —
/// failures are represented as a result, not an exception, so the caller can keep scanning.
/// </summary>
public interface ICertificateParser
{
    Task<CertificateParseResult> ParseAsync(string filePath, string? password, CancellationToken cancellationToken);
}
