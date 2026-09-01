using Microsoft.Data.Sqlite;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Persistence;

public sealed class SqliteScanSessionRepository : IScanSessionRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteScanSessionRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(ScanSession session, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ScanSessions (Id, StartedAtUtc, FinishedAtUtc, WasCancelled, FilesChecked, PfxFound, ExpiredCount, ErrorCount, RootsScanned)
            VALUES ($id, $started, $finished, $cancelled, $checked, $found, $expired, $errors, $roots);
            """;
        BindParameters(command, session);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScanSession session, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE ScanSessions SET FinishedAtUtc=$finished, WasCancelled=$cancelled,
                FilesChecked=$checked, PfxFound=$found, ExpiredCount=$expired, ErrorCount=$errors, RootsScanned=$roots
            WHERE Id=$id;
            """;
        BindParameters(command, session);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void BindParameters(SqliteCommand command, ScanSession session)
    {
        command.Parameters.AddWithValue("$id", session.Id.ToString());
        command.Parameters.AddWithValue("$started", session.StartedAtUtc.ToString("o"));
        command.Parameters.AddWithValue("$finished", ((DateTime?)session.FinishedAtUtc).AsDbValue());
        command.Parameters.AddWithValue("$cancelled", session.WasCancelled ? 1 : 0);
        command.Parameters.AddWithValue("$checked", session.FilesChecked);
        command.Parameters.AddWithValue("$found", session.PfxFound);
        command.Parameters.AddWithValue("$expired", session.ExpiredCount);
        command.Parameters.AddWithValue("$errors", session.ErrorCount);
        command.Parameters.AddWithValue("$roots", string.Join('|', session.RootsScanned));
    }

    public async Task<IReadOnlyList<ScanSession>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM ScanSessions ORDER BY StartedAtUtc DESC;";

        var results = new List<ScanSession>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ScanSession
            {
                Id = reader.GetGuid("Id"),
                StartedAtUtc = reader.GetDateTimeUtc("StartedAtUtc"),
                FinishedAtUtc = reader.GetNullableDateTimeUtc("FinishedAtUtc"),
                WasCancelled = reader.GetBool("WasCancelled"),
                FilesChecked = reader.GetInt64("FilesChecked"),
                PfxFound = reader.GetInt64("PfxFound"),
                ExpiredCount = reader.GetInt64("ExpiredCount"),
                ErrorCount = reader.GetInt64("ErrorCount"),
                RootsScanned = (reader.GetNullableString("RootsScanned") ?? string.Empty)
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
            });
        }

        return results;
    }
}
