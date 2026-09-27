namespace FolderBackup.Core.Services;

public interface IBackupRunLock
{
    /// <summary>
    /// Tries to take the machine-wide "backup in progress" lock without waiting.
    /// Returns a handle that releases the lock when disposed, or null if another backup is running.
    /// </summary>
    IDisposable? TryAcquire();
}
