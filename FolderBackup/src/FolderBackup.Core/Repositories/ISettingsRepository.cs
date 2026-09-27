using FolderBackup.Core.Models;

namespace FolderBackup.Core.Repositories;

public interface ISettingsRepository
{
    Task<BackupSettings> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(BackupSettings settings, CancellationToken ct = default);
}
