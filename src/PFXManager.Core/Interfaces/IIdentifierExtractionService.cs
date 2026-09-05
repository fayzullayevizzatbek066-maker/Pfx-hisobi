namespace PFXManager.Core.Interfaces;

public sealed record ExtractedIdentifiers(string? Stir, string? Pinfl, string? OwnerDisplayName);

/// <summary>
/// Best-effort extraction of Uzbekistan tax/personal identifiers (STIR/INN, JSHSHIR/PINFL) and a
/// human-friendly owner name from a certificate's raw Subject / extension data. Never used as an
/// input to deletion or status decisions — parsing failures here must never block or corrupt
/// certificate management operations.
/// </summary>
public interface IIdentifierExtractionService
{
    ExtractedIdentifiers Extract(string? rawSubject);
}
