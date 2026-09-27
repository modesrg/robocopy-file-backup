using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public interface IConfiguredBackupService
{
    /// <summary>Loads the saved settings and runs every enabled job.</summary>
    Task<BackupRunResult> RunEnabledJobsAsync(IProgress<BackupProgress>? progress = null, CancellationToken ct = default);
}
