using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public interface IBackupJobValidator
{
    /// <summary>Checks that the job is well-formed. Doesn't touch the disk, so a disconnected drive is fine.</summary>
    ValidationResult ValidateConfiguration(BackupJob job);

    /// <summary>Checks the configuration, plus that the source exists and the backup drive is connected.</summary>
    ValidationResult ValidateReadyToRun(BackupJob job);
}
