using System.Diagnostics.CodeAnalysis;
using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

/// <summary>
/// Decides where a job's files actually go: the backup folder plus the source folder's name.
/// For example, C:\Users\me\Pictures backed up to E:\Backup goes into E:\Backup\Pictures.
/// This is the only place that rule lives.
/// </summary>
public interface IBackupTargetResolver
{
    /// <exception cref="ArgumentException">The source or backup folder isn't a valid full path.</exception>
    string GetTargetPath(BackupJob job);

    bool TryGetTargetPath(BackupJob job, [NotNullWhen(true)] out string? targetPath);
}
