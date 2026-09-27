using System.Globalization;
using FolderBackup.Core.Constants;
using FolderBackup.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FolderBackup.Core.Services;

public sealed class FileBackupLogFactory : IBackupLogFactory
{
    private readonly StorageOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FileBackupLogFactory> _logger;

    public FileBackupLogFactory(IOptions<StorageOptions> options, TimeProvider timeProvider, ILogger<FileBackupLogFactory> logger)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public IBackupLog Create()
    {
        Directory.CreateDirectory(_options.LogDirectory);
        DeleteExpiredLogs();

        var timestamp = _timeProvider.GetLocalNow().ToString(AppConstants.LogFileTimestampFormat, CultureInfo.InvariantCulture);
        var fileName = $"{AppConstants.LogFilePrefix}{timestamp}{AppConstants.LogFileExtension}";
        return new FileBackupLog(Path.Combine(_options.LogDirectory, fileName), _timeProvider);
    }

    private void DeleteExpiredLogs()
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-_options.LogRetentionDays);
        var pattern = $"{AppConstants.LogFilePrefix}*{AppConstants.LogFileExtension}";

        foreach (var file in Directory.EnumerateFiles(_options.LogDirectory, pattern))
        {
            if (File.GetLastWriteTimeUtc(file) >= cutoff)
            {
                continue;
            }

            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not delete old log {File}.", file);
            }
        }
    }
}
