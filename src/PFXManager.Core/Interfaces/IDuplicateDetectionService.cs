using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

/// <summary>
/// Groups certificate records that represent the same certificate identity found at multiple
/// file system locations. Primary key is Thumbprint; Serial Number, Subject and Issuer are used
/// as secondary corroboration only for records missing a thumbprint (e.g. read errors are never
/// grouped).
/// </summary>
public interface IDuplicateDetectionService
{
    IReadOnlyList<DuplicateGroup> FindDuplicates(IEnumerable<CertificateRecord> records);
}
