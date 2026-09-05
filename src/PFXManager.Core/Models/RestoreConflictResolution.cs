namespace PFXManager.Core.Models;

public enum RestoreConflictAction
{
    Cancel,
    RenameNew,
    ChooseDestination
}

public sealed record RestoreOptions(RestoreConflictAction ConflictAction, string? ExplicitDestinationPath = null);

public sealed record QuarantineResult(Guid RecordId, bool Success, string? ErrorMessage);
public sealed record RestoreResult(Guid QuarantineItemId, bool Success, string? ErrorMessage, string? RestoredPath, bool HadConflict);
