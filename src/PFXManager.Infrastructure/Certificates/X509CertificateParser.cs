using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Certificates;

/// <summary>
/// Parses .pfx/.p12 files using System.Security.Cryptography.X509Certificates. Never throws for
/// password-protected or corrupted input: every failure path is captured in the returned
/// <see cref="CertificateParseResult"/> so the scanner can keep going.
/// </summary>
public sealed class X509CertificateParser : ICertificateParser
{
    private readonly ILogger<X509CertificateParser> _logger;

    public X509CertificateParser(ILogger<X509CertificateParser> logger)
    {
        _logger = logger;
    }

    public Task<CertificateParseResult> ParseAsync(string filePath, string? password, CancellationToken cancellationToken)
    {
        return Task.Run(() => Parse(filePath, password), cancellationToken);
    }

    private CertificateParseResult Parse(string filePath, string? password)
    {
        // No password supplied: try an empty/no password first (common for exported PFX files),
        // and treat any decryption failure as "password required" rather than a hard read error.
        var attemptedPassword = password ?? string.Empty;

        X509Certificate2? certificate = null;
        try
        {
            certificate = new X509Certificate2(
                filePath,
                attemptedPassword,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

            return BuildResult(filePath, certificate);
        }
        catch (CryptographicException ex) when (IsPasswordFailure(ex))
        {
            return CertificateParseResult.PasswordProtected(filePath);
        }
        catch (CryptographicException ex)
        {
            _logger.LogDebug(ex, "Corrupted or unsupported PFX at {Path}", filePath);
            return CertificateParseResult.Failed(filePath, "Sertifikatni o'qib bo'lmadi: fayl buzilgan yoki qo'llab-quvvatlanmaydigan format.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "I/O error reading PFX at {Path}", filePath);
            return CertificateParseResult.Failed(filePath, "Faylni o'qib bo'lmadi: kirish taqiqlangan yoki fayl band.");
        }
        finally
        {
            certificate?.Dispose();
        }
    }

    private static bool IsPasswordFailure(CryptographicException ex)
    {
        // .NET does not expose a typed "wrong password" exception; the underlying platform
        // (Windows CryptoAPI / OpenSSL) reports it through HResult / message text.
        const int NteBadKeySet = unchecked((int)0x80090016);
        const int NteBadData = unchecked((int)0x80090005);

        if (ex.HResult is NteBadKeySet or NteBadData)
        {
            return true;
        }

        var message = ex.Message;
        return message.Contains("password", StringComparison.OrdinalIgnoreCase)
               || message.Contains("MAC", StringComparison.OrdinalIgnoreCase)
               || message.Contains("invalid password", StringComparison.OrdinalIgnoreCase);
    }

    private static CertificateParseResult BuildResult(string filePath, X509Certificate2 certificate)
    {
        return new CertificateParseResult
        {
            FullPath = filePath,
            Success = true,
            PasswordRequired = false,
            Subject = certificate.Subject,
            CommonName = ExtractRdnComponent(certificate.Subject, "CN"),
            Organization = ExtractRdnComponent(certificate.Subject, "O"),
            OrganizationalUnit = ExtractRdnComponent(certificate.Subject, "OU"),
            Issuer = certificate.Issuer,
            SerialNumber = certificate.SerialNumber,
            Thumbprint = certificate.Thumbprint,
            NotBefore = certificate.NotBefore.ToUniversalTime(),
            NotAfter = certificate.NotAfter.ToUniversalTime(),
            HasPrivateKey = certificate.HasPrivateKey,
            SignatureAlgorithm = certificate.SignatureAlgorithm?.FriendlyName,
            FriendlyName = SafeFriendlyName(certificate),
            CertificateVersion = certificate.Version,
            KeyAlgorithm = certificate.GetKeyAlgorithm() is { } oid ? FriendlyOid(oid) : null
        };
    }

    private static string? SafeFriendlyName(X509Certificate2 certificate)
    {
        try
        {
            // FriendlyName is a Windows-only certificate store concept; on non-Windows platforms
            // the getter throws PlatformNotSupportedException.
            return certificate.FriendlyName;
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
    }

    private static string FriendlyOid(string oidValue)
    {
        try
        {
            return new Oid(oidValue).FriendlyName ?? oidValue;
        }
        catch
        {
            return oidValue;
        }
    }

    private static string? ExtractRdnComponent(string? distinguishedName, string key)
    {
        if (string.IsNullOrWhiteSpace(distinguishedName))
        {
            return null;
        }

        try
        {
            foreach (var part in X500DistinguishedName_SplitRdns(distinguishedName))
            {
                var separatorIndex = part.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var partKey = part[..separatorIndex].Trim();
                if (string.Equals(partKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    return part[(separatorIndex + 1)..].Trim();
                }
            }
        }
        catch
        {
            // Best-effort parsing only; malformed subjects must never break file discovery.
        }

        return null;
    }

    /// <summary>
    /// Splits a comma-separated RFC 2253 distinguished name into its RDN components, honoring
    /// backslash-escaped commas inside a value (e.g. "O=Acme\\, Inc.").
    /// </summary>
    private static IEnumerable<string> X500DistinguishedName_SplitRdns(string dn)
    {
        var current = new System.Text.StringBuilder();
        for (var i = 0; i < dn.Length; i++)
        {
            var c = dn[i];
            if (c == '\\' && i + 1 < dn.Length)
            {
                current.Append(c);
                current.Append(dn[i + 1]);
                i++;
                continue;
            }

            if (c == ',')
            {
                yield return current.ToString();
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }
}
