namespace FolderBackup.App.Constants;

internal static class UiText
{
    public const string AppTitle = "Folder Backup";
    public const string AlreadyRunning = "Folder Backup is already running. Look for its icon in the system tray.";

    // Common buttons
    public const string Ok = "OK";
    public const string Cancel = "Cancel";
    public const string Close = "Close";
    public const string Save = "Save";
    public const string Browse = "Browse…";

    // Tray menu
    public const string MenuBackUpNow = "Back up now";
    public const string MenuSettings = "Settings…";
    public const string MenuOpenLogs = "Open log folder";
    public const string MenuExit = "Exit";
    public const string ConfirmExitDuringBackup = "A backup is running. Cancel it and exit?";
    public const string OpenLogsFailed = "Couldn't open the log folder.";

    // Settings form
    public const string SettingsTitle = "Folder Backup – Settings";
    public const string JobsGroup = "Backup jobs (tick to enable)";
    public const string ScheduleGroup = "Schedule";
    public const string ColumnName = "Name";
    public const string ColumnSource = "Source";
    public const string ColumnDestination = "Backup folder";
    public const string ColumnMode = "Mode";
    public const string Add = "Add…";
    public const string Edit = "Edit…";
    public const string Remove = "Remove";
    public const string RunAutomatically = "Run backups automatically";
    public const string FrequencyLabel = "Frequency:";
    public const string AtLabel = "at";
    public const string FrequencyDaily = "Daily";
    public const string FrequencyWeekly = "Weekly on";
    public const string NotScheduled = "Automatic backups are off.";
    public const string NextRunUnknown = "Couldn't read the schedule from Task Scheduler.";
    public const string PickAtLeastOneDay = "Pick at least one day for a weekly schedule.";
    public const string LoadSettingsFailed = "Couldn't load your settings.";
    public const string SaveSettingsFailed = "Couldn't save your settings.";

    // Job editor
    public const string NewJobTitle = "New backup job";
    public const string EditJobTitle = "Edit backup job";
    public const string JobNameLabel = "Name:";
    public const string SourceLabel = "Source folder:";
    public const string DestinationLabel = "Backup folder:";
    public const string ModeLabel = "Mode:";
    public const string PickSource = "Choose the folder to back up";
    public const string PickDestination = "Choose where the backup goes";
    public const string FixProblemsTitle = "Please fix the following";

    // Backup modes
    public const string ModeAppendOnly = "Append only";
    public const string ModeOverwriteIfNewer = "Update if newer";
    public const string ModeOverwrite = "Overwrite";
    public const string ModeAppendOnlyDescription = "Copies new files only. Files already in the backup are never changed.";
    public const string ModeOverwriteIfNewerDescription = "Copies new files, and replaces a backup copy when the source file is newer.";
    public const string ModeOverwriteDescription = "Copies new files, and replaces any backup copy that differs from the source.";

    // Progress form
    public const string ProgressTitle = "Folder Backup";
    public const string CurrentFileCaption = "Current file";
    public const string JobProgressCaption = "This job";
    public const string Starting = "Starting…";
    public const string Cancelling = "Cancelling…";
    public const string RunCancelled = "Backup cancelled.";

    // Results
    public const string ResultTitleSuccess = "Backup complete";
    public const string ResultTitleProblems = "Backup finished with problems";
    public const string ResultTitleAlreadyRunning = "Backup already running";
    public const string ResultAlreadyRunning = "Another backup is in progress, so this one was skipped.";
    public const string ResultNoJobs = "There are no enabled backup jobs. Add one in Settings.";

    public static string NextRun(DateTime when) => $"Next backup: {when:f}";

    public static string ConfirmRemoveJob(string name) => $"Remove the backup job \"{name}\"? Files already backed up are not deleted.";

    public static string JobHeader(int number, int count, string name) => $"Job {number} of {count}: {name}";

    public static string Scanning(int filesFound) => $"Looking for changes… {filesFound:N0} file(s) to copy so far";

    public static string Copying(int done, int total, string bytesDone, string bytesTotal) =>
        $"Copied {done:N0} of {total:N0} file(s) · {bytesDone} of {bytesTotal}";

    public static string RunFailed(string reason) => $"The backup couldn't run: {reason}";

    public static string JobSucceeded(string name, int files, string size) => $"✔ {name}: {files:N0} file(s) copied ({size})";

    public static string JobFailed(string name, int errorCount, string firstError) =>
        errorCount > 1 ? $"✖ {name}: {errorCount} errors, see the log" : $"✖ {name}: {firstError}";

    public static string JobSkipped(string name, string reason) => $"⚠ {name}: skipped. {reason}";

    public static string JobCancelled(string name) => $"■ {name}: cancelled";
}
