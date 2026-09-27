using Microsoft.Data.Sqlite;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Persistence;

public sealed class SqliteQuarantineRepository : IQuarantineRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteQuarantineRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(QuarantineItem item, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO QuarantineItems
                (Id, CertificateRecordId, OriginalPath, QuarantinePath, FileName, Thumbprint, SerialNumber,
                 NotAfter, QuarantinedAtUtc, OriginalLastModifiedUtc, FileSizeBytes, Reason, IsResolved, ResolutionKind, ResolvedAtUtc)
            VALUES
                ($id, $recordId, $original, $quarantine, $fileName, $thumb, $serial,
                 $notAfter, $quarantinedAt, $originalModified, $size, $reason, $resolved, $resolutionKind, $resolvedAt);
            """;
        BindParameters(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(QuarantineItem item, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE QuarantineItems SET IsResolved=$resolved, ResolutionKind=$resolutionKind, ResolvedAtUtc=$resolvedAt
            WHERE Id=$id;
            """;
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$resolved", item.IsResolved ? 1 : 0);
        command.Parameters.AddWithValue("$resolutionKind", item.ResolutionKind.AsDbValue());
        command.Parameters.AddWithValue("$resolvedAt", ((DateTime?)item.ResolvedAtUtc).AsDbValue());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void BindParameters(SqliteCommand command, QuarantineItem item)
    {
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$recordId", item.CertificateRecordId.ToString());
        command.Parameters.AddWithValue("$original", item.OriginalPath);
        command.Parameters.AddWithValue("$quarantine", item.QuarantinePath);
        command.Parameters.AddWithValue("$fileName", item.FileName);
        command.Parameters.AddWithValue("$thumb", item.Thumbprint.AsDbValue());
        command.Parameters.AddWithValue("$serial", item.SerialNumber.AsDbValue());
        command.Parameters.AddWithValue("$notAfter", item.NotAfter.AsDbValue());
        command.Parameters.AddWithValue("$quarantinedAt", item.QuarantinedAtUtc.ToString("o"));
        command.Parameters.AddWithValue("$originalModified", ((DateTime?)item.OriginalLastModifiedUtc).AsDbValue());
        command.Parameters.AddWithValue("$size", item.FileSizeBytes);
        command.Parameters.AddWithValue("$reason", item.Reason.AsDbValue());
        command.Parameters.AddWithValue("$resolved", item.IsResolved ? 1 : 0);
        command.Parameters.AddWithValue("$resolutionKind", item.ResolutionKind.AsDbValue());
        command.Parameters.AddWithValue("$resolvedAt", ((DateTime?)item.ResolvedAtUtc).AsDbValue());
    }

    public async Task<IReadOnlyList<QuarantineItem>> GetActiveAsync(CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM QuarantineItems WHERE IsResolved = 0 ORDER BY QuarantinedAtUtc DESC;";

        var results = new List<QuarantineItem>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public async Task<QuarantineItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM QuarantineItems WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private static QuarantineItem Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetGuid("Id"),
        CertificateRecordId = reader.GetGuid("CertificateRecordId"),
        OriginalPath = reader.GetString(reader.GetOrdinal("OriginalPath")),
        QuarantinePath = reader.GetString(reader.GetOrdinal("QuarantinePath")),
        FileName = reader.GetString(reader.GetOrdinal("FileName")),
        Thumbprint = reader.GetNullableString("Thumbprint"),
        SerialNumber = reader.GetNullableString("SerialNumber"),
        NotAfter = reader.GetNullableDateTimeUtc("NotAfter"),
        QuarantinedAtUtc = reader.GetDateTimeUtc("QuarantinedAtUtc"),
        OriginalLastModifiedUtc = reader.GetDateTimeUtc("OriginalLastModifiedUtc"),
        FileSizeBytes = reader.GetInt64("FileSizeBytes"),
        Reason = reader.GetNullableString("Reason") ?? string.Empty,
        IsResolved = reader.GetBool("IsResolved"),
        ResolutionKind = reader.GetNullableString("ResolutionKind"),
        ResolvedAtUtc = reader.GetNullableDateTimeUtc("ResolvedAtUtc")
    };
}
