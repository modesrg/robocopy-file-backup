using FolderBackup.Core.Constants;

namespace FolderBackup.Core.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    private static readonly string DefaultDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppConstants.AppName);

    /// <summary>Where settings and logs are stored. Defaults to %AppData%\FolderBackup.</summary>
    public string DataDirectory { get; init; } = DefaultDataDirectory;

    public int LogRetentionDays { get; init; } = 30;

    public string SettingsFilePath => Path.Combine(DataDirectory, AppConstants.SettingsFileName);

    public string LogDirectory => Path.Combine(DataDirectory, AppConstants.LogFolderName);
}
