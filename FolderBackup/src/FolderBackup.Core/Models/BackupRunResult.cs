namespace FolderBackup.Core.Models;

public sealed record BackupRunResult(BackupRunStatus Status, IReadOnlyList<BackupJobResult> Jobs, string? LogFilePath)
{
    public bool AllSucceeded =>
        Status == BackupRunStatus.Completed && Jobs.All(job => job.Status == BackupJobStatus.Succeeded);

    public static BackupRunResult AlreadyRunning() => new(BackupRunStatus.AlreadyRunning, [], null);

    public static BackupRunResult Completed(IReadOnlyList<BackupJobResult> jobs, string logFilePath) =>
        new(BackupRunStatus.Completed, jobs, logFilePath);
}
