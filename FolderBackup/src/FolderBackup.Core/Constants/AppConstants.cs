namespace FolderBackup.Core.Constants;

public static class AppConstants
{
    public const string AppName = "FolderBackup";

    // Storage
    public const string SettingsFileName = "settings.json";
    public const string TempFileExtension = ".tmp";
    public const string LogFolderName = "Logs";
    public const string LogFilePrefix = "backup_";
    public const string LogFileExtension = ".log";
    public const string LogFileTimestampFormat = "yyyy-MM-dd_HHmmss";
    public const string LogLineTimestampFormat = "yyyy-MM-dd HH:mm:ss";

    // Robocopy
    public const string RobocopyExecutableName = "robocopy.exe";

    // Windows Task Scheduler
    public const string ScheduledTaskName = "FolderBackup - Scheduled backup";
    public const string ScheduledTaskDescription = "Runs the backup jobs configured in the FolderBackup tray app.";

    // Cross-process synchronization
    public const string RunLockName = @"Local\FolderBackup.RunLock";
    public const string TrayInstanceMutexName = @"Local\FolderBackup.Tray";
}
