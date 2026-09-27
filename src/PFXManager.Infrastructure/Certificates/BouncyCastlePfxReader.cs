using System.Security.Cryptography;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Certificates;

/// <summary>
/// Fallback PKCS#12 reader for PFX files System.Security.Cryptography.X509Certificates cannot
/// open. The most common real-world case for this app's audience: Uzbekistan E-IMZO certificates
/// signed with GOST (28147-89 / R 34.10) algorithms, which .NET's built-in X509Certificate2 -
/// backed by Windows CryptoAPI/CNG or OpenSSL depending on platform - cannot decode without a
/// GOST cryptographic provider installed on the machine. BouncyCastle implements GOST in pure
/// managed code, so it works regardless of what's registered with the OS.
/// </summary>
internal static class BouncyCastlePfxReader
{
    public static bool TryRead(string filePath, string password, out CertificateParseResult? result, out string? errorDetail)
    {
        result = null;
        errorDetail = null;

        Pkcs12Store store;
        try
        {
            store = new Pkcs12StoreBuilder().Build();
            using var stream = File.OpenRead(filePath);
            store.Load(stream, password.ToCharArray());
        }
        catch (Exception ex)
        {
            // BouncyCastle does not expose a distinct "wrong password" exception type either;
            // since this reader only runs after the primary .NET parser already failed, an empty
            // password is the common case for these files and a genuine password requirement is
            // reported by the primary parser's own (more reliable) heuristic, not repeated here.
            errorDetail = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }

        string? alias = null;
        foreach (var candidate in store.Aliases)
        {
            if (store.IsCertificateEntry(candidate) || store.IsKeyEntry(candidate))
            {
                alias = candidate;
                if (store.IsKeyEntry(candidate))
                {
                    // Prefer an entry that actually carries the private key, matching what a real
                    // PFX export normally contains as its "main" certificate.
                    break;
                }
            }
        }

        if (alias is null)
        {
            errorDetail = "BouncyCastle: no certificate/key entry found in the PKCS12 store.";
            return false;
        }

        var certEntry = store.GetCertificate(alias);
        if (certEntry is null)
        {
            errorDetail = "BouncyCastle: alias resolved but GetCertificate returned null.";
            return false;
        }

        var certificate = certEntry.Certificate;
        var hasPrivateKey = store.IsKeyEntry(alias);

        string thumbprint;
        try
        {
            thumbprint = Convert.ToHexString(SHA1.HashData(certificate.GetEncoded()));
        }
        catch (Exception)
        {
            thumbprint = string.Empty;
        }

        // Use the two's-complement byte representation (not BigInteger.ToString(16)) so a serial
        // number whose top bit is set keeps the same leading 0x00 sign-padding byte that
        // X509Certificate2.SerialNumber includes - otherwise the hex string silently disagrees
        // with what .NET reports for the identical certificate.
        var serialHex = Convert.ToHexString(certificate.SerialNumber.ToByteArray());

        result = new CertificateParseResult
        {
            FullPath = filePath,
            Success = true,
            PasswordRequired = false,
            Subject = certificate.SubjectDN?.ToString(),
            CommonName = ExtractRdnValue(certificate.SubjectDN?.ToString(), "CN"),
            Organization = ExtractRdnValue(certificate.SubjectDN?.ToString(), "O"),
            OrganizationalUnit = ExtractRdnValue(certificate.SubjectDN?.ToString(), "OU"),
            Issuer = certificate.IssuerDN?.ToString(),
            SerialNumber = serialHex,
            Thumbprint = thumbprint,
            NotBefore = DateTime.SpecifyKind(certificate.NotBefore, DateTimeKind.Utc),
            NotAfter = DateTime.SpecifyKind(certificate.NotAfter, DateTimeKind.Utc),
            HasPrivateKey = hasPrivateKey,
            SignatureAlgorithm = certificate.SigAlgName,
            FriendlyName = null,
            CertificateVersion = certificate.Version,
            KeyAlgorithm = DescribePublicKeyAlgorithm(certificate)
        };

        return true;
    }

    private static string? DescribePublicKeyAlgorithm(X509Certificate certificate)
    {
        try
        {
            // BouncyCastle's public key type name is a reasonable, always-available stand-in for
            // a proper OID-to-friendly-name lookup (e.g. "ECGost3410PublicKeyParameters",
            // "RsaKeyParameters") - good enough for display, never used for any decision.
            return certificate.GetPublicKey()?.GetType().Name;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ExtractRdnValue(string? distinguishedName, string key)
    {
        if (string.IsNullOrWhiteSpace(distinguishedName))
        {
            return null;
        }

        foreach (var part in distinguishedName.Split(','))
        {
            var trimmed = part.Trim();
            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var partKey = trimmed[..separatorIndex].Trim();
            if (string.Equals(partKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[(separatorIndex + 1)..].Trim();
            }
        }

        return null;
    }
}
