using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Core.Services;

/// <summary>
/// Groups by Thumbprint (the primary certificate identity). Records with no thumbprint
/// (PasswordRequired / ReadError) are never grouped — there is nothing to safely compare.
/// Serial Number / Subject / Issuer are exposed for secondary display/verification but are not
/// used to widen a group beyond exact thumbprint matches, to avoid false positives.
/// </summary>
public sealed class DuplicateDetectionService : IDuplicateDetectionService
{
    public IReadOnlyList<DuplicateGroup> FindDuplicates(IEnumerable<CertificateRecord> records)
    {
        var groups = records
            .Where(r => !string.IsNullOrWhiteSpace(r.Thumbprint)
                        && r.Status != CertificateStatus.PasswordRequired
                        && r.Status != CertificateStatus.ReadError)
            .GroupBy(r => r.Thumbprint!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateGroup
            {
                Thumbprint = g.Key,
                Copies = g.OrderBy(r => r.FullPath, StringComparer.OrdinalIgnoreCase).ToList()
            })
            .OrderByDescending(g => g.CopyCount)
            .ToList();

        return groups;
    }
}
