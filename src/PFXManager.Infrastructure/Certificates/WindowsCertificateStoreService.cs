using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Certificates;

/// <summary>
/// Reads and manages Windows Certificate Store entries (CurrentUser\My, LocalMachine\My, ...).
/// Deliberately has no knowledge of file-based PFX records: removing a store entry never touches
/// a file on disk, and file deletion never touches the store.
/// </summary>
public sealed class WindowsCertificateStoreService : IWindowsCertificateStoreService
{
    private readonly ICertificateStatusEngine _statusEngine;
    private readonly ILogger<WindowsCertificateStoreService> _logger;

    public WindowsCertificateStoreService(ICertificateStatusEngine statusEngine, ILogger<WindowsCertificateStoreService> logger)
    {
        _statusEngine = statusEngine;
        _logger = logger;
    }

    public IReadOnlyList<WindowsCertificateEntry> GetCertificates(CertStoreLocation location, string storeName = "My")
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogInformation("Windows Certificate Store is only available on Windows.");
            return Array.Empty<WindowsCertificateEntry>();
        }

        return GetCertificatesWindows(location, storeName);
    }

    [SupportedOSPlatform("windows")]
    private IReadOnlyList<WindowsCertificateEntry> GetCertificatesWindows(CertStoreLocation location, string storeName)
    {
        var storeLocation = location == CertStoreLocation.CurrentUser
            ? System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser
            : System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine;

        using var store = new X509Store(storeName, storeLocation);
        var results = new List<WindowsCertificateEntry>();

        try
        {
            store.Open(OpenFlags.ReadOnly);
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not open certificate store {Store}\\{Location}", storeName, storeLocation);
            return results;
        }

        foreach (var certificate in store.Certificates)
        {
            var notAfterUtc = certificate.NotAfter.ToUniversalTime();
            var status = _statusEngine.DetermineStatus(notAfterUtc, passwordRequired: false, readError: false);

            results.Add(new WindowsCertificateEntry
            {
                Subject = certificate.Subject,
                Issuer = certificate.Issuer,
                SerialNumber = certificate.SerialNumber,
                Thumbprint = certificate.Thumbprint,
                NotBefore = certificate.NotBefore.ToUniversalTime(),
                NotAfter = notAfterUtc,
                Status = status,
                RemainingDays = _statusEngine.ComputeRemainingDays(notAfterUtc),
                StoreLocation = location,
                StoreName = storeName,
                HasPrivateKey = certificate.HasPrivateKey
            });
        }

        return results;
    }

    public bool RemoveCertificate(CertStoreLocation location, string storeName, string thumbprint)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        return RemoveCertificateWindows(location, storeName, thumbprint);
    }

    [SupportedOSPlatform("windows")]
    private bool RemoveCertificateWindows(CertStoreLocation location, string storeName, string thumbprint)
    {
        var storeLocation = location == CertStoreLocation.CurrentUser
            ? System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser
            : System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine;

        using var store = new X509Store(storeName, storeLocation);
        try
        {
            store.Open(OpenFlags.ReadWrite);
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not open certificate store {Store}\\{Location} for write (elevation may be required)", storeName, storeLocation);
            return false;
        }

        var matches = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        if (matches.Count == 0)
        {
            return false;
        }

        store.RemoveRange(matches);
        return true;
    }
}
