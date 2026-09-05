using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

public interface ICertificateRecordRepository
{
    Task UpsertManyAsync(IEnumerable<CertificateRecord> records, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateRecord>> GetAllAsync(CancellationToken cancellationToken);
    Task<CertificateRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteByPathAsync(string fullPath, CancellationToken cancellationToken);
    Task UpdateDuplicateGroupsAsync(IReadOnlyDictionary<Guid, Guid?> recordIdToGroupId, CancellationToken cancellationToken);
    Task ClearAllAsync(CancellationToken cancellationToken);
}
