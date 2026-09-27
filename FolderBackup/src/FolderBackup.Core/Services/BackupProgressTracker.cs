using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

/// <summary>
/// Turns the stream of robocopy output for one job into <see cref="BackupProgress"/> reports.
/// Not thread-safe; <see cref="IProcessRunner"/> guarantees callbacks arrive one at a time.
/// </summary>
internal sealed class BackupProgressTracker
{
    private const int DiscoveryReportInterval = 250;
    private const double FullPercent = 100;

    private readonly string _jobName;
    private readonly int _jobNumber;
    private readonly int _jobCount;
    private readonly IProgress<BackupProgress>? _progress;

    private BackupPhase _phase = BackupPhase.Scanning;
    private int _filesProcessed;
    private long _bytesProcessed;
    private string? _currentFile;
    private long _currentFileSize;
    private double _currentFilePercent;
    private bool _currentFileCounted;
    private int _lastReportedWholePercent = -1;

    public BackupProgressTracker(string jobName, int jobNumber, int jobCount, IProgress<BackupProgress>? progress)
    {
        _jobName = jobName;
        _jobNumber = jobNumber;
        _jobCount = jobCount;
        _progress = progress;
    }

    public int TotalFiles { get; private set; }

    public long TotalBytes { get; private set; }

    /// <summary>Files robocopy reported as 100% copied.</summary>
    public int FilesCopied { get; private set; }

    public long BytesCopied { get; private set; }

    public void BeginScanning()
    {
        _phase = BackupPhase.Scanning;
        Report();
    }

    public void OnFileDiscovered(RobocopyFileLine file)
    {
        TotalFiles++;
        TotalBytes += file.SizeBytes;

        if (TotalFiles % DiscoveryReportInterval == 0)
        {
            Report();
        }
    }

    public void BeginCopying()
    {
        _phase = BackupPhase.Copying;
        Report();
    }

    public void OnFileStarted(RobocopyFileLine file)
    {
        FinishCurrentFile();

        _currentFile = file.Path;
        _currentFileSize = file.SizeBytes;
        _currentFilePercent = 0;
        _currentFileCounted = false;
        _lastReportedWholePercent = -1;
        Report();
    }

    public void OnFilePercent(double percent)
    {
        if (_currentFile is null)
        {
            return;
        }

        _currentFilePercent = Math.Clamp(percent, 0, FullPercent);

        if (_currentFilePercent >= FullPercent && !_currentFileCounted)
        {
            _currentFileCounted = true;
            FilesCopied++;
            BytesCopied += _currentFileSize;
        }

        // Robocopy prints many percent lines for large files; only report whole-percent changes.
        var wholePercent = (int)_currentFilePercent;
        if (wholePercent != _lastReportedWholePercent)
        {
            _lastReportedWholePercent = wholePercent;
            Report();
        }
    }

    public void Complete()
    {
        FinishCurrentFile();
        _phase = BackupPhase.Finished;
        Report();
    }

    /// <summary>Counts the current file as processed, whether it succeeded or failed, so the bar keeps moving.</summary>
    private void FinishCurrentFile()
    {
        if (_currentFile is null)
        {
            return;
        }

        _filesProcessed++;
        _bytesProcessed += _currentFileSize;
        _currentFile = null;
        _currentFileSize = 0;
        _currentFilePercent = 0;
    }

    private void Report()
    {
        if (_progress is null)
        {
            return;
        }

        var bytesProcessed = _bytesProcessed + (long)(_currentFileSize * _currentFilePercent / FullPercent);

        // Files can appear between the scan and the copy, so never let "done" exceed "total".
        _progress.Report(new BackupProgress(
            _jobName,
            _jobNumber,
            _jobCount,
            _phase,
            _currentFile,
            _currentFilePercent,
            _filesProcessed,
            Math.Max(TotalFiles, _filesProcessed),
            bytesProcessed,
            Math.Max(TotalBytes, bytesProcessed)));
    }
}
