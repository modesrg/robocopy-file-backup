using FolderBackup.Core.Models;
using FolderBackup.Core.Repositories;

namespace FolderBackup.Core.Services;

public sealed class ConfiguredBackupService : IConfiguredBackupService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IBackupService _backupService;

    public ConfiguredBackupService(ISettingsRepository settingsRepository, IBackupService backupService)
    {
        _settingsRepository = settingsRepository;
        _backupService = backupService;
    }

    public async Task<BackupRunResult> RunEnabledJobsAsync(IProgress<BackupProgress>? progress = null, CancellationToken ct = default)
    {
        var settings = await _settingsRepository.LoadAsync(ct).ConfigureAwait(false);
        var enabledJobs = settings.Jobs.Where(job => job.IsEnabled).ToList();
        return await _backupService.RunAsync(enabledJobs, progress, ct).ConfigureAwait(false);
    }
}
