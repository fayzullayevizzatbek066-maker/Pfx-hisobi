using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Persistence;

public sealed class SqliteOperationLogRepository : IOperationLogRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteOperationLogRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(OperationLogEntry entry, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO OperationLogs (TimestampUtc, Action, Details, Success)
            VALUES ($ts, $action, $details, $success);
            """;
        command.Parameters.AddWithValue("$ts", entry.TimestampUtc.ToString("o"));
        command.Parameters.AddWithValue("$action", entry.Action);
        command.Parameters.AddWithValue("$details", entry.Details.AsDbValue());
        command.Parameters.AddWithValue("$success", entry.Success ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OperationLogEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM OperationLogs ORDER BY TimestampUtc DESC LIMIT $limit;";
        command.Parameters.AddWithValue("$limit", limit);

        var results = new List<OperationLogEntry>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new OperationLogEntry
            {
                Id = reader.GetInt64("Id"),
                TimestampUtc = reader.GetDateTimeUtc("TimestampUtc"),
                Action = reader.GetString(reader.GetOrdinal("Action")),
                Details = reader.GetNullableString("Details"),
                Success = reader.GetBool("Success")
            });
        }

        return results;
    }
}
