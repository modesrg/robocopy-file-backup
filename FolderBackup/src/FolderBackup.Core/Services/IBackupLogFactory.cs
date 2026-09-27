namespace FolderBackup.Core.Services;

public interface IBackupLogFactory
{
    /// <summary>Creates a new log file for one backup run.</summary>
    IBackupLog Create();
}
