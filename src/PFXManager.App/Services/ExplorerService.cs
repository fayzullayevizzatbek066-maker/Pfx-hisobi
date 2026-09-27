using System.Diagnostics;
using System.Windows;

namespace PFXManager.App.Services;

public sealed class ExplorerService : IExplorerService
{
    public void ShowFileInExplorer(string fullPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var startInfo = new ProcessStartInfo("explorer.exe")
        {
            UseShellExecute = true
        };
        // Passed as a single pre-built argument (not via ArgumentList) because Explorer expects
        // "/select,<path>" as one token; ProcessStartInfo.Arguments here is never shell-interpreted
        // (UseShellExecute launches explorer.exe directly, not through cmd.exe), so this is not a
        // command-injection vector even though the path is user/filesystem controlled.
        startInfo.ArgumentList.Add($"/select,{fullPath}");
        Process.Start(startInfo);
    }

    public void CopyToClipboard(string text)
    {
        Clipboard.SetText(text);
    }
}
