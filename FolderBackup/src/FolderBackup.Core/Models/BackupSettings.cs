namespace FolderBackup.Core.Models;

public sealed record BackupSettings
{
    public IReadOnlyList<BackupJob> Jobs { get; init; } = [];
    public BackupSchedule Schedule { get; init; } = new();
}
