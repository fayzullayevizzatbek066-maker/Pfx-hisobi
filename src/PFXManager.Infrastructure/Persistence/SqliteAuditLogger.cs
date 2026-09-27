using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Persistence;

public sealed class SqliteAuditLogger : IAuditLogger
{
    private readonly IOperationLogRepository _repository;

    public SqliteAuditLogger(IOperationLogRepository repository)
    {
        _repository = repository;
    }

    public Task LogAsync(string action, string? details = null, bool success = true, CancellationToken cancellationToken = default)
    {
        return _repository.AddAsync(new OperationLogEntry
        {
            Action = action,
            Details = details,
            Success = success
        }, cancellationToken);
    }
}
