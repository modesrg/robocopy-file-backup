using FolderBackup.App.Constants;
using FolderBackup.Core.Models;
using FolderBackup.Core.Services;
using Microsoft.Extensions.Logging;

namespace FolderBackup.App.Services;

internal sealed class HeadlessBackupRunner : IHeadlessBackupRunner
{
    private readonly IConfiguredBackupService _backupService;
    private readonly ILogger<HeadlessBackupRunner> _logger;

    public HeadlessBackupRunner(IConfiguredBackupService backupService, ILogger<HeadlessBackupRunner> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _backupService.RunEnabledJobsAsync(progress: null, ct);

            return result switch
            {
                { Status: BackupRunStatus.AlreadyRunning } => ProcessExitCodes.AlreadyRunning,
                { AllSucceeded: true } => ProcessExitCodes.Success,
                _ => ProcessExitCodes.BackupFailed,
            };
        }
        catch (Exception ex)
        {
            // Top-level boundary of a scheduled run: log it (the Windows Event Log is included) and fail.
            _logger.LogError(ex, "Scheduled backup failed.");
            return ProcessExitCodes.BackupFailed;
        }
    }
}
