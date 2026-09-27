namespace FolderBackup.Core.Services;

public interface IBackupLog : IDisposable
{
    string FilePath { get; }

    void WriteLine(string message);
}
