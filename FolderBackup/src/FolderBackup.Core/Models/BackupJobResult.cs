namespace FolderBackup.Core.Models;

public sealed record BackupJobResult(
    BackupJob Job,
    BackupJobStatus Status,
    RobocopyExitCode? ExitCode,
    int FilesCopied,
    long BytesCopied,
    IReadOnlyList<string> Messages,
    TimeSpan Duration)
{
    public static BackupJobResult Skipped(BackupJob job, IReadOnlyList<string> reasons) =>
        new(job, BackupJobStatus.Skipped, null, 0, 0, reasons, TimeSpan.Zero);

    public static BackupJobResult Cancelled(BackupJob job, int filesCopied, long bytesCopied, TimeSpan duration) =>
        new(job, BackupJobStatus.Cancelled, null, filesCopied, bytesCopied, [], duration);

    public static BackupJobResult Failed(BackupJob job, string reason, TimeSpan duration) =>
        new(job, BackupJobStatus.Failed, null, 0, 0, [reason], duration);
}
