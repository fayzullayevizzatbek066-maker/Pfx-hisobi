namespace PFXManager.Core.Models;

/// <summary>Outcome of attempting to parse a single .pfx/.p12 file.</summary>
public sealed class CertificateParseResult
{
    public required string FullPath { get; init; }
    public bool Success { get; init; }
    public bool PasswordRequired { get; init; }
    public string? ErrorMessage { get; init; }

    public string? Subject { get; init; }
    public string? CommonName { get; init; }
    public string? Organization { get; init; }
    public string? OrganizationalUnit { get; init; }
    public string? Issuer { get; init; }
    public string? SerialNumber { get; init; }
    public string? Thumbprint { get; init; }
    public DateTime? NotBefore { get; init; }
    public DateTime? NotAfter { get; init; }
    public bool HasPrivateKey { get; init; }
    public string? SignatureAlgorithm { get; init; }
    public string? FriendlyName { get; init; }
    public int? CertificateVersion { get; init; }
    public string? KeyAlgorithm { get; init; }

    public static CertificateParseResult PasswordProtected(string path) => new()
    {
        FullPath = path,
        Success = false,
        PasswordRequired = true
    };

    public static CertificateParseResult Failed(string path, string message) => new()
    {
        FullPath = path,
        Success = false,
        PasswordRequired = false,
        ErrorMessage = message
    };
}
