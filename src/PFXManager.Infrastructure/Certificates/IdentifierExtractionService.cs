using System.Text.RegularExpressions;
using PFXManager.Core.Interfaces;

namespace PFXManager.Infrastructure.Certificates;

/// <summary>
/// Best-effort extraction of Uzbekistan STIR/INN (9 digits) and JSHSHIR/PINFL (14 digits) from a
/// certificate Subject. Uzbek PKI (E-IMZO) certificates commonly encode these as explicit RDN
/// keys (UID, INN, STIR, PINFL, SERIALNUMBER) or embed them in a free-text component; both shapes
/// are handled. This is display-only metadata — it is never used to decide which certificates are
/// eligible for deletion.
/// </summary>
public sealed partial class IdentifierExtractionService : IIdentifierExtractionService
{
    private static readonly string[] StirKeys = { "STIR", "INN", "TIN" };
    private static readonly string[] PinflKeys = { "PINFL", "JSHSHIR", "PERSONALID", "UID" };
    private static readonly string[] NameKeys = { "CN", "GIVENNAME", "SURNAME", "G", "SN" };

    [GeneratedRegex(@"\b\d{9}\b")]
    private static partial Regex NineDigitRegex();

    [GeneratedRegex(@"\b\d{14}\b")]
    private static partial Regex FourteenDigitRegex();

    public ExtractedIdentifiers Extract(string? rawSubject)
    {
        if (string.IsNullOrWhiteSpace(rawSubject))
        {
            return new ExtractedIdentifiers(null, null, null);
        }

        var rdns = SplitRdns(rawSubject);

        string? stir = FindByKeys(rdns, StirKeys);
        string? pinfl = FindByKeys(rdns, PinflKeys);
        string? owner = FindByKeys(rdns, NameKeys);

        // Fallback: scan the raw subject text for bare digit runs matching the expected lengths.
        stir ??= FirstMatch(NineDigitRegex(), rawSubject);
        pinfl ??= FirstMatch(FourteenDigitRegex(), rawSubject);

        return new ExtractedIdentifiers(stir, pinfl, owner);
    }

    private static string? FirstMatch(Regex regex, string input)
    {
        var match = regex.Match(input);
        return match.Success ? match.Value : null;
    }

    private static string? FindByKeys(IReadOnlyList<(string Key, string Value)> rdns, string[] keys)
    {
        foreach (var key in keys)
        {
            var hit = rdns.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));
            if (hit.Value is { Length: > 0 })
            {
                return hit.Value;
            }
        }

        return null;
    }

    private static List<(string Key, string Value)> SplitRdns(string dn)
    {
        var result = new List<(string, string)>();
        var current = new System.Text.StringBuilder();

        void Flush()
        {
            var part = current.ToString();
            current.Clear();
            var separatorIndex = part.IndexOf('=');
            if (separatorIndex > 0)
            {
                result.Add((part[..separatorIndex].Trim(), part[(separatorIndex + 1)..].Trim()));
            }
        }

        for (var i = 0; i < dn.Length; i++)
        {
            var c = dn[i];
            if (c == '\\' && i + 1 < dn.Length)
            {
                current.Append(dn[i + 1]);
                i++;
                continue;
            }

            if (c == ',' || c == '+')
            {
                Flush();
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            Flush();
        }

        return result;
    }
}
