namespace FolderBackup.Core.Models;

public enum BackupMode
{
    /// <summary>Copy new files only. Files already in the backup are never touched.</summary>
    AppendOnly,

    /// <summary>Copy new files, and replace a backup copy only when the source file is newer.</summary>
    OverwriteIfNewer,

    /// <summary>Copy new files, and replace any backup copy that differs from the source.</summary>
    Overwrite,
}
