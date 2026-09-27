namespace FolderBackup.Core.Options;

public sealed class RobocopyOptions
{
    public const string SectionName = "Robocopy";

    /// <summary>Full path to robocopy.exe. Leave empty to use the one in System32.</summary>
    public string ExecutablePath { get; init; } = string.Empty;

    public int RetryCount { get; init; } = 2;

    public int RetryWaitSeconds { get; init; } = 5;

    /// <summary>Use 2-second timestamp precision (/FFT). Needed for FAT32/exFAT backup drives, harmless on NTFS.</summary>
    public bool UseFatFileTimes { get; init; } = true;
}
