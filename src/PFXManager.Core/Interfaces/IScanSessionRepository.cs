using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

public interface IScanSessionRepository
{
    Task AddAsync(ScanSession session, CancellationToken cancellationToken);
    Task UpdateAsync(ScanSession session, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScanSession>> GetAllAsync(CancellationToken cancellationToken);
}
