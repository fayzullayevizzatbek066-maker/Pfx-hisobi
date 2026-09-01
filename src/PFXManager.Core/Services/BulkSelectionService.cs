using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Core.Services;

/// <summary>
/// "Barcha muddati o'tganlarni tanlash" (select all expired) must only ever select records whose
/// status is verified <see cref="CertificateStatus.Expired"/> — never PasswordRequired or
/// ReadError, and never derived from file name or modified date.
/// </summary>
public interface IBulkSelectionService
{
    IReadOnlyList<CertificateRecord> SelectAllExpired(IEnumerable<CertificateRecord> records);
}

public sealed class BulkSelectionService : IBulkSelectionService
{
    public IReadOnlyList<CertificateRecord> SelectAllExpired(IEnumerable<CertificateRecord> records) =>
        records.Where(r => r.Status == CertificateStatus.Expired && r.NotAfter is not null).ToList();
}
