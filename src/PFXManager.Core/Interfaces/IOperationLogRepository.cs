using PFXManager.Core.Models;

namespace PFXManager.Core.Interfaces;

public interface IOperationLogRepository
{
    Task AddAsync(OperationLogEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<OperationLogEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken);
}
