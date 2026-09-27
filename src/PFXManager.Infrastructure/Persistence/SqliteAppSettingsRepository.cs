using System.Text.Json;
using PFXManager.Core.Interfaces;
using PFXManager.Core.Models;

namespace PFXManager.Infrastructure.Persistence;

public sealed class SqliteAppSettingsRepository : IAppSettingsRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteAppSettingsRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT JsonData FROM AppSettings WHERE Id = 1;";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not string json)
        {
            return new AppSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(settings);

        using var connection = _connectionFactory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AppSettings (Id, JsonData) VALUES (1, $json)
            ON CONFLICT(Id) DO UPDATE SET JsonData = excluded.JsonData;
            """;
        command.Parameters.AddWithValue("$json", json);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
