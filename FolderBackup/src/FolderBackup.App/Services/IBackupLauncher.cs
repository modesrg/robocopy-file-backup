using FolderBackup.Core.Models;

namespace FolderBackup.App.Services;

/// <summary>
/// Starts backups from anywhere in the UI (tray menu, Settings window) and makes sure there is
/// only ever one progress window.
/// </summary>
internal interface IBackupLauncher
{
    event EventHandler<BackupRunResult>? RunCompleted;

    event EventHandler? ProgressWindowClosed;

    bool IsRunning { get; }

    /// <summary>Runs the given jobs, whether or not they're enabled. If a backup is already running, shows it instead.</summary>
    void Start(IReadOnlyList<BackupJob> jobs);

    /// <summary>Loads the saved settings and runs every enabled job.</summary>
    Task StartEnabledJobsAsync(CancellationToken ct = default);

    /// <summary>Cancels a running backup and closes the progress window once robocopy has stopped.</summary>
    void CancelAndClose();
}
