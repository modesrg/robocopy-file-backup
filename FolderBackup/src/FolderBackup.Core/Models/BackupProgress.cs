namespace FolderBackup.Core.Models;

public sealed record BackupProgress(
    string JobName,
    int JobNumber,
    int JobCount,
    BackupPhase Phase,
    string? CurrentFile,
    double CurrentFilePercent,
    int FilesProcessed,
    int TotalFiles,
    long BytesProcessed,
    long TotalBytes)
{
    /// <summary>Progress of the current job, 0–100, weighted by bytes.</summary>
    public double JobPercent => Phase switch
    {
        BackupPhase.Finished => 100,
        _ when TotalBytes > 0 => BytesProcessed * 100.0 / TotalBytes,
        _ when TotalFiles > 0 => FilesProcessed * 100.0 / TotalFiles,
        _ => 0,
    };
}
