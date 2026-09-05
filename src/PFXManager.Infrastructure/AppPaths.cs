namespace PFXManager.Infrastructure;

/// <summary>Well-known on-disk locations used by PFX Manager. Centralized so every service agrees.</summary>
public static class AppPaths
{
    private static string BaseDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PFXManager");

    public static string DatabasePath => Path.Combine(BaseDataDirectory, "pfxmanager.db");

    public static string DefaultQuarantineRoot => Path.Combine(BaseDataDirectory, "Quarantine");

    public static string LogsDirectory => Path.Combine(BaseDataDirectory, "Logs");

    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(BaseDataDirectory);
        Directory.CreateDirectory(DefaultQuarantineRoot);
        Directory.CreateDirectory(LogsDirectory);
    }
}
