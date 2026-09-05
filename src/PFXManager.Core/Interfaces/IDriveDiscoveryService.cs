using PFXManager.Core.Enums;

namespace PFXManager.Core.Interfaces;

public sealed record DiscoveredDrive(string RootPath, DriveScanKind Kind, bool IsReady);

public interface IDriveDiscoveryService
{
    IReadOnlyList<DiscoveredDrive> DiscoverDrives();
}
