using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

public interface IQuarantineRepository
{
    Task AddAsync(QuarantineItem item, CancellationToken cancellationToken);
    Task UpdateAsync(QuarantineItem item, CancellationToken cancellationToken);
    Task<IReadOnlyList<QuarantineItem>> GetActiveAsync(CancellationToken cancellationToken);
    Task<QuarantineItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
