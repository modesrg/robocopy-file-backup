using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public interface IBackupService
{
    /// <summary>
    /// Runs the given jobs one after another. Each job is validated first, then scanned with a dry run
    /// (so progress has real totals), then copied.
    /// </summary>
    Task<BackupRunResult> RunAsync(
        IReadOnlyList<BackupJob> jobs,
        IProgress<BackupProgress>? progress = null,
        CancellationToken ct = default);
}
