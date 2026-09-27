using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public interface IBackupJobValidator
{
    /// <summary>
    /// Checks that the job is well-formed and doesn't back up into the same place as any of
    /// <paramref name="otherJobs"/>. Doesn't touch the disk, so a disconnected drive is fine.
    /// </summary>
    ValidationResult ValidateConfiguration(BackupJob job, IReadOnlyCollection<BackupJob> otherJobs);

    /// <summary>Checks the job on its own, plus that the source exists and the backup drive is connected.</summary>
    ValidationResult ValidateReadyToRun(BackupJob job);
}
