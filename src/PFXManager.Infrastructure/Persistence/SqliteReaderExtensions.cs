using System.Globalization;
using Microsoft.Data.Sqlite;

namespace PFXManager.Infrastructure.Persistence;

internal static class SqliteReaderExtensions
{
    public static string? GetNullableString(this SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static long GetInt64(this SqliteDataReader reader, string column, long fallback = 0)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? fallback : reader.GetInt64(ordinal);
    }

    public static int? GetNullableInt32(this SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    public static bool GetBool(this SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return !reader.IsDBNull(ordinal) && reader.GetInt64(ordinal) != 0;
    }

    public static DateTime? GetNullableDateTimeUtc(this SqliteDataReader reader, string column)
    {
        var text = reader.GetNullableString(column);
        if (text is null)
        {
            return null;
        }

        return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
    }

    public static DateTime GetDateTimeUtc(this SqliteDataReader reader, string column) =>
        GetNullableDateTimeUtc(reader, column) ?? default;

    public static Guid? GetNullableGuid(this SqliteDataReader reader, string column)
    {
        var text = reader.GetNullableString(column);
        return text is null ? null : Guid.Parse(text);
    }

    public static Guid GetGuid(this SqliteDataReader reader, string column) =>
        Guid.Parse(reader.GetString(reader.GetOrdinal(column)));

    public static object AsDbValue(this string? value) => (object?)value ?? DBNull.Value;

    public static object AsDbValue(this DateTime? value) =>
        value is null ? DBNull.Value : value.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    public static object AsDbValue(this Guid? value) => value is null ? DBNull.Value : value.Value.ToString();

    public static object AsDbValue(this int? value) => (object?)value ?? DBNull.Value;
}
