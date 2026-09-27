namespace FolderBackup.Core.Models;

public sealed record BackupJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public BackupMode Mode { get; init; } = BackupMode.AppendOnly;
    public bool IsEnabled { get; init; } = true;
}
