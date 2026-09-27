namespace FolderBackup.App.Services;

internal interface IHeadlessBackupRunner
{
    /// <summary>Runs all enabled jobs without UI and returns a process exit code.</summary>
    Task<int> RunAsync(CancellationToken ct = default);
}
