using System.ComponentModel;
using FolderBackup.Core.Extensions;
using FolderBackup.Core.Models;
using Microsoft.Extensions.Logging;

namespace FolderBackup.Core.Services;

public sealed class BackupService : IBackupService
{
    private readonly IRobocopyCommandBuilder _commandBuilder;
    private readonly IRobocopyRunner _robocopy;
    private readonly IBackupJobValidator _validator;
    private readonly IBackupRunLock _runLock;
    private readonly IBackupLogFactory _logFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IRobocopyCommandBuilder commandBuilder,
        IRobocopyRunner robocopy,
        IBackupJobValidator validator,
        IBackupRunLock runLock,
        IBackupLogFactory logFactory,
        TimeProvider timeProvider,
        ILogger<BackupService> logger)
    {
        _commandBuilder = commandBuilder;
        _robocopy = robocopy;
        _validator = validator;
        _runLock = runLock;
        _logFactory = logFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<BackupRunResult> RunAsync(
        IReadOnlyList<BackupJob> jobs,
        IProgress<BackupProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        using var runLock = _runLock.TryAcquire();
        if (runLock is null)
        {
            _logger.LogWarning("Another backup is already running; this run was skipped.");
            return BackupRunResult.AlreadyRunning();
        }

        using var log = _logFactory.Create();
        log.WriteLine($"Backup run started with {jobs.Count} job(s).");

        var results = new List<BackupJobResult>(jobs.Count);
        for (var index = 0; index < jobs.Count && !ct.IsCancellationRequested; index++)
        {
            var result = await RunJobAsync(jobs[index], index + 1, jobs.Count, log, progress, ct).ConfigureAwait(false);
            results.Add(result);
            log.WriteLine(Describe(result));
        }

        log.WriteLine("Backup run finished.");
        _logger.LogInformation("Backup run finished: {Succeeded} of {Total} job(s) succeeded.",
            results.Count(r => r.Status == BackupJobStatus.Succeeded), jobs.Count);

        return BackupRunResult.Completed(results, log.FilePath);
    }

    private async Task<BackupJobResult> RunJobAsync(
        BackupJob job,
        int jobNumber,
        int jobCount,
        IBackupLog log,
        IProgress<BackupProgress>? progress,
        CancellationToken ct)
    {
        var startedAt = _timeProvider.GetTimestamp();
        log.WriteLine($"[{job.Name}] {job.SourcePath} -> {job.DestinationPath} ({job.Mode})");

        var validation = _validator.ValidateReadyToRun(job);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                log.WriteLine($"[{job.Name}] Skipped: {error}");
            }

            return BackupJobResult.Skipped(job, validation.Errors);
        }

        var tracker = new BackupProgressTracker(job.Name, jobNumber, jobCount, progress);
        try
        {
            tracker.BeginScanning();
            await ScanAsync(job, tracker, ct).ConfigureAwait(false);
            log.WriteLine($"[{job.Name}] {tracker.TotalFiles:N0} file(s) to copy, {tracker.TotalBytes:N0} bytes.");

            tracker.BeginCopying();
            var errors = new List<string>();
            var exitCode = await _robocopy
                .RunAsync(_commandBuilder.BuildCopyArguments(job), line => HandleCopyOutput(line, tracker, log, errors), ct)
                .ConfigureAwait(false);
            tracker.Complete();

            if (!exitCode.IsSuccess() && errors.Count == 0)
            {
                errors.Add($"Robocopy reported a failure ({exitCode}).");
            }

            var status = exitCode.IsSuccess() ? BackupJobStatus.Succeeded : BackupJobStatus.Failed;
            return new BackupJobResult(job, status, exitCode, tracker.FilesCopied, tracker.BytesCopied, errors, Elapsed(startedAt));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return BackupJobResult.Cancelled(job, tracker.FilesCopied, tracker.BytesCopied, Elapsed(startedAt));
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException)
        {
            _logger.LogError(ex, "Backup job {JobName} could not run.", job.Name);
            return BackupJobResult.Failed(job, ex.Message, Elapsed(startedAt));
        }
    }

    /// <summary>Dry run (/L) so the progress bar knows how many files and bytes are coming.</summary>
    private async Task ScanAsync(BackupJob job, BackupProgressTracker tracker, CancellationToken ct)
    {
        await _robocopy
            .RunAsync(
                _commandBuilder.BuildListArguments(job),
                line =>
                {
                    if (line is RobocopyFileLine file)
                    {
                        tracker.OnFileDiscovered(file);
                    }
                },
                ct)
            .ConfigureAwait(false);
    }

    private static void HandleCopyOutput(RobocopyOutputLine line, BackupProgressTracker tracker, IBackupLog log, List<string> errors)
    {
        switch (line)
        {
            case RobocopyFileLine file:
                tracker.OnFileStarted(file);
                log.WriteLine(file.RawText.Trim());
                break;

            case RobocopyPercentLine percent:
                tracker.OnFilePercent(percent.Percent);
                break;

            case RobocopyErrorLine error:
                errors.Add(error.RawText.Trim());
                log.WriteLine(error.RawText.Trim());
                break;

            case RobocopyInfoLine info when !string.IsNullOrWhiteSpace(info.RawText):
                log.WriteLine(info.RawText.Trim());
                break;
        }
    }

    private TimeSpan Elapsed(long startedAt) => _timeProvider.GetElapsedTime(startedAt);

    private static string Describe(BackupJobResult result) =>
        $"[{result.Job.Name}] {result.Status}: {result.FilesCopied:N0} file(s), {result.BytesCopied:N0} bytes copied " +
        $"in {result.Duration:hh\\:mm\\:ss}" +
        (result.ExitCode is { } exitCode ? $" (robocopy exit code {(int)exitCode}: {exitCode})." : ".");
}
