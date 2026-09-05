using System.Globalization;
using Microsoft.Data.Sqlite;
using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Persistence;

public sealed class SqliteCertificateRecordRepository : ICertificateRecordRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteCertificateRecordRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task UpsertManyAsync(IEnumerable<CertificateRecord> records, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();

        // Batch in reasonably sized transactions to keep memory pressure and lock duration low
        // even for 10,000+ discovered files, per the performance requirements.
        const int batchSize = 500;
        var batch = 0;
        var transaction = connection.BeginTransaction();

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO CertificateRecords
                    (Id, FullPath, Drive, FileSizeBytes, CreatedTimeUtc, LastModifiedTimeUtc,
                     Subject, CommonName, Organization, OrganizationalUnit, Issuer, SerialNumber, Thumbprint,
                     NotBefore, NotAfter, HasPrivateKey, SignatureAlgorithm, FriendlyName, CertificateVersion, KeyAlgorithm,
                     RawSubject, Stir, Pinfl, OwnerDisplayName,
                     Status, RemainingDays, ReadErrorMessage, IsPasswordProtected, DuplicateGroupId,
                     DiscoveredAtUtc, ScanSessionId)
                VALUES
                    ($id, $fullPath, $drive, $size, $created, $modified,
                     $subject, $cn, $org, $ou, $issuer, $serial, $thumb,
                     $notBefore, $notAfter, $hasKey, $sigAlg, $friendly, $certVer, $keyAlg,
                     $rawSubject, $stir, $pinfl, $owner,
                     $status, $remaining, $readError, $pwdProtected, $dupGroup,
                     $discovered, $scanSession)
                ON CONFLICT(FullPath) DO UPDATE SET
                    Drive=excluded.Drive, FileSizeBytes=excluded.FileSizeBytes,
                    CreatedTimeUtc=excluded.CreatedTimeUtc, LastModifiedTimeUtc=excluded.LastModifiedTimeUtc,
                    Subject=excluded.Subject, CommonName=excluded.CommonName, Organization=excluded.Organization,
                    OrganizationalUnit=excluded.OrganizationalUnit, Issuer=excluded.Issuer, SerialNumber=excluded.SerialNumber,
                    Thumbprint=excluded.Thumbprint, NotBefore=excluded.NotBefore, NotAfter=excluded.NotAfter,
                    HasPrivateKey=excluded.HasPrivateKey, SignatureAlgorithm=excluded.SignatureAlgorithm,
                    FriendlyName=excluded.FriendlyName, CertificateVersion=excluded.CertificateVersion, KeyAlgorithm=excluded.KeyAlgorithm,
                    RawSubject=excluded.RawSubject, Stir=excluded.Stir, Pinfl=excluded.Pinfl, OwnerDisplayName=excluded.OwnerDisplayName,
                    Status=excluded.Status, RemainingDays=excluded.RemainingDays, ReadErrorMessage=excluded.ReadErrorMessage,
                    IsPasswordProtected=excluded.IsPasswordProtected, DuplicateGroupId=excluded.DuplicateGroupId,
                    DiscoveredAtUtc=excluded.DiscoveredAtUtc, ScanSessionId=excluded.ScanSessionId;
                """;

            BindParameters(command, record);
            await command.ExecuteNonQueryAsync(cancellationToken);

            batch++;
            if (batch % batchSize == 0)
            {
                transaction.Commit();
                transaction.Dispose();
                transaction = connection.BeginTransaction();
            }
        }

        transaction.Commit();
        transaction.Dispose();
    }

    private static void BindParameters(SqliteCommand command, CertificateRecord record)
    {
        command.Parameters.AddWithValue("$id", record.Id.ToString());
        command.Parameters.AddWithValue("$fullPath", record.FullPath);
        command.Parameters.AddWithValue("$drive", record.Drive.AsDbValue());
        command.Parameters.AddWithValue("$size", record.FileSizeBytes);
        command.Parameters.AddWithValue("$created", ((DateTime?)record.CreatedTimeUtc).AsDbValue());
        command.Parameters.AddWithValue("$modified", ((DateTime?)record.LastModifiedTimeUtc).AsDbValue());
        command.Parameters.AddWithValue("$subject", record.Subject.AsDbValue());
        command.Parameters.AddWithValue("$cn", record.CommonName.AsDbValue());
        command.Parameters.AddWithValue("$org", record.Organization.AsDbValue());
        command.Parameters.AddWithValue("$ou", record.OrganizationalUnit.AsDbValue());
        command.Parameters.AddWithValue("$issuer", record.Issuer.AsDbValue());
        command.Parameters.AddWithValue("$serial", record.SerialNumber.AsDbValue());
        command.Parameters.AddWithValue("$thumb", record.Thumbprint.AsDbValue());
        command.Parameters.AddWithValue("$notBefore", record.NotBefore.AsDbValue());
        command.Parameters.AddWithValue("$notAfter", record.NotAfter.AsDbValue());
        command.Parameters.AddWithValue("$hasKey", record.HasPrivateKey ? 1 : 0);
        command.Parameters.AddWithValue("$sigAlg", record.SignatureAlgorithm.AsDbValue());
        command.Parameters.AddWithValue("$friendly", record.FriendlyName.AsDbValue());
        command.Parameters.AddWithValue("$certVer", record.CertificateVersion.AsDbValue());
        command.Parameters.AddWithValue("$keyAlg", record.KeyAlgorithm.AsDbValue());
        command.Parameters.AddWithValue("$rawSubject", record.RawSubject.AsDbValue());
        command.Parameters.AddWithValue("$stir", record.Stir.AsDbValue());
        command.Parameters.AddWithValue("$pinfl", record.Pinfl.AsDbValue());
        command.Parameters.AddWithValue("$owner", record.OwnerDisplayName.AsDbValue());
        command.Parameters.AddWithValue("$status", record.Status.ToString());
        command.Parameters.AddWithValue("$remaining", record.RemainingDays.AsDbValue());
        command.Parameters.AddWithValue("$readError", record.ReadErrorMessage.AsDbValue());
        command.Parameters.AddWithValue("$pwdProtected", record.IsPasswordProtected ? 1 : 0);
        command.Parameters.AddWithValue("$dupGroup", record.DuplicateGroupId.AsDbValue());
        command.Parameters.AddWithValue("$discovered", ((DateTime?)record.DiscoveredAtUtc).AsDbValue());
        command.Parameters.AddWithValue("$scanSession", (object?)record.ScanSessionId.ToString() ?? DBNull.Value);
    }

    public async Task<IReadOnlyList<CertificateRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM CertificateRecords;";

        var results = new List<CertificateRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public async Task<CertificateRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM CertificateRecords WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM CertificateRecords WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteByPathAsync(string fullPath, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM CertificateRecords WHERE FullPath = $path;";
        command.Parameters.AddWithValue("$path", fullPath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateDuplicateGroupsAsync(IReadOnlyDictionary<Guid, Guid?> recordIdToGroupId, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var transaction = connection.BeginTransaction();

        foreach (var (recordId, groupId) in recordIdToGroupId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE CertificateRecords SET DuplicateGroupId = $group WHERE Id = $id;";
            command.Parameters.AddWithValue("$group", groupId.AsDbValue());
            command.Parameters.AddWithValue("$id", recordId.ToString());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM CertificateRecords;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static CertificateRecord Map(SqliteDataReader reader)
    {
        return new CertificateRecord
        {
            Id = reader.GetGuid("Id"),
            FullPath = reader.GetString(reader.GetOrdinal("FullPath")),
            Drive = reader.GetNullableString("Drive") ?? string.Empty,
            FileSizeBytes = reader.GetInt64("FileSizeBytes"),
            CreatedTimeUtc = reader.GetDateTimeUtc("CreatedTimeUtc"),
            LastModifiedTimeUtc = reader.GetDateTimeUtc("LastModifiedTimeUtc"),
            Subject = reader.GetNullableString("Subject"),
            CommonName = reader.GetNullableString("CommonName"),
            Organization = reader.GetNullableString("Organization"),
            OrganizationalUnit = reader.GetNullableString("OrganizationalUnit"),
            Issuer = reader.GetNullableString("Issuer"),
            SerialNumber = reader.GetNullableString("SerialNumber"),
            Thumbprint = reader.GetNullableString("Thumbprint"),
            NotBefore = reader.GetNullableDateTimeUtc("NotBefore"),
            NotAfter = reader.GetNullableDateTimeUtc("NotAfter"),
            HasPrivateKey = reader.GetBool("HasPrivateKey"),
            SignatureAlgorithm = reader.GetNullableString("SignatureAlgorithm"),
            FriendlyName = reader.GetNullableString("FriendlyName"),
            CertificateVersion = reader.GetNullableInt32("CertificateVersion"),
            KeyAlgorithm = reader.GetNullableString("KeyAlgorithm"),
            RawSubject = reader.GetNullableString("RawSubject"),
            Stir = reader.GetNullableString("Stir"),
            Pinfl = reader.GetNullableString("Pinfl"),
            OwnerDisplayName = reader.GetNullableString("OwnerDisplayName"),
            Status = Enum.Parse<CertificateStatus>(reader.GetString(reader.GetOrdinal("Status"))),
            RemainingDays = reader.GetNullableInt32("RemainingDays"),
            ReadErrorMessage = reader.GetNullableString("ReadErrorMessage"),
            IsPasswordProtected = reader.GetBool("IsPasswordProtected"),
            DuplicateGroupId = reader.GetNullableGuid("DuplicateGroupId"),
            DiscoveredAtUtc = reader.GetDateTimeUtc("DiscoveredAtUtc"),
            ScanSessionId = reader.GetNullableGuid("ScanSessionId") ?? Guid.Empty
        };
    }
}
