namespace PFXManager.Core.Models;

public enum AppTheme
{
    System,
    Light,
    Dark
}

public enum AppLanguage
{
    UzbekLatin,
    Russian,
    English
}

public sealed class AppSettings
{
    public bool ScanLocalFixedDrives { get; set; } = true;
    public bool ScanNetworkDrives { get; set; }
    public bool ScanRemovableDrives { get; set; }
    public List<string> CustomFolders { get; set; } = new();

    public int ExpiringSoonWarningDays { get; set; } = 30;
    public int ExpiringWarningDays { get; set; } = 90;

    public string QuarantinePath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PFXManager", "Quarantine");

    public AppTheme Theme { get; set; } = AppTheme.System;
    public AppLanguage Language { get; set; } = AppLanguage.UzbekLatin;

    public bool TelemetryEnabled { get; set; } = false;
}
