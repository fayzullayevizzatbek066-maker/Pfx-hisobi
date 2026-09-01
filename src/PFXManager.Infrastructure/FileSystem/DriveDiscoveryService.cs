using PFXManager.Core.Enums;
using PFXManager.Core.Interfaces;

namespace PFXManager.Infrastructure.FileSystem;

public sealed class DriveDiscoveryService : IDriveDiscoveryService
{
    public IReadOnlyList<DiscoveredDrive> DiscoverDrives()
    {
        var result = new List<DiscoveredDrive>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            DriveScanKind kind;
            switch (drive.DriveType)
            {
                case DriveType.Fixed:
                    kind = DriveScanKind.LocalFixed;
                    break;
                case DriveType.Removable:
                    kind = DriveScanKind.Removable;
                    break;
                case DriveType.Network:
                    kind = DriveScanKind.Network;
                    break;
                default:
                    // CD-ROM, RAM disk, Unknown: not useful scan targets, skip.
                    continue;
            }

            result.Add(new DiscoveredDrive(drive.RootDirectory.FullName, kind, drive.IsReady));
        }

        return result;
    }
}
