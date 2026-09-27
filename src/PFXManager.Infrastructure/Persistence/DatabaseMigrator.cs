using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace PFXManager.Infrastructure.Persistence;

/// <summary>
/// Minimal, ordered schema-version migration runner. Each entry in <see cref="Migrations"/> is
/// applied at most once, tracked in a SchemaVersion table, so upgrading PFX Manager on a machine
/// with existing data never loses history.
/// </summary>
public sealed class DatabaseMigrator
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(SqliteConnectionFactory connectionFactory, ILogger<DatabaseMigrator> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    private static readonly (int Version, string Sql)[] Migrations =
    {
        (1, """
            CREATE TABLE IF NOT EXISTS CertificateRecords (
                Id TEXT PRIMARY KEY,
                FullPath TEXT NOT NULL UNIQUE,
                Drive TEXT,
                FileSizeBytes INTEGER NOT NULL DEFAULT 0,
                CreatedTimeUtc TEXT,
                LastModifiedTimeUtc TEXT,
                Subject TEXT,
                CommonName TEXT,
                Organization TEXT,
                OrganizationalUnit TEXT,
                Issuer TEXT,
                SerialNumber TEXT,
                Thumbprint TEXT,
                NotBefore TEXT,
                NotAfter TEXT,
                HasPrivateKey INTEGER NOT NULL DEFAULT 0,
                SignatureAlgorithm TEXT,
                FriendlyName TEXT,
                CertificateVersion INTEGER,
                KeyAlgorithm TEXT,
                RawSubject TEXT,
                Stir TEXT,
                Pinfl TEXT,
                OwnerDisplayName TEXT,
                Status TEXT NOT NULL,
                RemainingDays INTEGER,
                ReadErrorMessage TEXT,
                IsPasswordProtected INTEGER NOT NULL DEFAULT 0,
                DuplicateGroupId TEXT,
                DiscoveredAtUtc TEXT NOT NULL,
                ScanSessionId TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_CertificateRecords_Status ON CertificateRecords(Status);
            CREATE INDEX IF NOT EXISTS IX_CertificateRecords_Thumbprint ON CertificateRecords(Thumbprint);
            CREATE INDEX IF NOT EXISTS IX_CertificateRecords_Drive ON CertificateRecords(Drive);

            CREATE TABLE IF NOT EXISTS ScanSessions (
                Id TEXT PRIMARY KEY,
                StartedAtUtc TEXT NOT NULL,
                FinishedAtUtc TEXT,
                WasCancelled INTEGER NOT NULL DEFAULT 0,
                FilesChecked INTEGER NOT NULL DEFAULT 0,
                PfxFound INTEGER NOT NULL DEFAULT 0,
                ExpiredCount INTEGER NOT NULL DEFAULT 0,
                ErrorCount INTEGER NOT NULL DEFAULT 0,
                RootsScanned TEXT
            );

            CREATE TABLE IF NOT EXISTS QuarantineItems (
                Id TEXT PRIMARY KEY,
                CertificateRecordId TEXT,
                OriginalPath TEXT NOT NULL,
                QuarantinePath TEXT NOT NULL,
                FileName TEXT NOT NULL,
                Thumbprint TEXT,
                SerialNumber TEXT,
                NotAfter TEXT,
                QuarantinedAtUtc TEXT NOT NULL,
                OriginalLastModifiedUtc TEXT,
                FileSizeBytes INTEGER NOT NULL DEFAULT 0,
                Reason TEXT,
                IsResolved INTEGER NOT NULL DEFAULT 0,
                ResolutionKind TEXT,
                ResolvedAtUtc TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_QuarantineItems_IsResolved ON QuarantineItems(IsResolved);

            CREATE TABLE IF NOT EXISTS OperationLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TimestampUtc TEXT NOT NULL,
                Action TEXT NOT NULL,
                Details TEXT,
                Success INTEGER NOT NULL DEFAULT 1
            );
            CREATE INDEX IF NOT EXISTS IX_OperationLogs_TimestampUtc ON OperationLogs(TimestampUtc);

            CREATE TABLE IF NOT EXISTS AppSettings (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                JsonData TEXT NOT NULL
            );
            """)
    };

    public void Migrate()
    {
        using var connection = _connectionFactory.Create();

        using (var createVersionTable = connection.CreateCommand())
        {
            createVersionTable.CommandText =
                "CREATE TABLE IF NOT EXISTS SchemaVersion (Version INTEGER PRIMARY KEY);";
            createVersionTable.ExecuteNonQuery();
        }

        var currentVersion = GetCurrentVersion(connection);

        foreach (var (version, sql) in Migrations.OrderBy(m => m.Version))
        {
            if (version <= currentVersion)
            {
                continue;
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }

                using (var recordVersion = connection.CreateCommand())
                {
                    recordVersion.Transaction = transaction;
                    recordVersion.CommandText = "INSERT INTO SchemaVersion (Version) VALUES ($v);";
                    recordVersion.Parameters.AddWithValue("$v", version);
                    recordVersion.ExecuteNonQuery();
                }

                transaction.Commit();
                _logger.LogInformation("Applied database migration {Version}", version);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    private static int GetCurrentVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion;";
        var result = command.ExecuteScalar();
        return result is long l ? (int)l : 0;
    }
}
