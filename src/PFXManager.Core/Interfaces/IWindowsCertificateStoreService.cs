using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

/// <summary>
/// Reads and manages certificates installed in the Windows Certificate Store. Kept entirely
/// separate from file-based PFX operations: removing a store entry never touches a file on disk,
/// and deleting a PFX file never touches the store.
/// </summary>
public interface IWindowsCertificateStoreService
{
    IReadOnlyList<WindowsCertificateEntry> GetCertificates(CertStoreLocation location, string storeName = "My");

    /// <summary>Removing from LocalMachine requires elevation; caller must have already obtained it.</summary>
    bool RemoveCertificate(CertStoreLocation location, string storeName, string thumbprint);
}
