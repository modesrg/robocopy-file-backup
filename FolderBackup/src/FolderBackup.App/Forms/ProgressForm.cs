using FolderBackup.App.Constants;
using FolderBackup.App.Presentation;
using FolderBackup.Core.Models;
using FolderBackup.Core.Services;
using Microsoft.Extensions.Logging;

namespace FolderBackup.App.Forms;

/// <summary>
/// Runs the given jobs and shows progress. Closing the window while a backup runs just hides it;
/// the tray icon reports the result. The Cancel button stops the backup.
/// </summary>
internal sealed class ProgressForm : Form
{
    private const int ProgressBarMaximum = 1000;
    private const double FullPercent = 100;

    private readonly IBackupService _backupService;
    private readonly IReadOnlyList<BackupJob> _jobs;
    private readonly ILogger<ProgressForm> _logger;
    private readonly CancellationTokenSource _cancellation = new();

    private readonly Label _jobLabel = new() { AutoSize = true, Text = UiText.Starting };
    private readonly Label _statusLabel = new() { AutoSize = true, MaximumSize = new Size(480, 0) };
    private readonly Label _fileLabel = new() { AutoEllipsis = true, Dock = DockStyle.Fill, Height = 20 };
    private readonly ProgressBar _fileProgress = CreateProgressBar();
    private readonly ProgressBar _jobProgress = CreateProgressBar();
    private readonly Button _actionButton = new() { Text = UiText.Cancel, AutoSize = true, MinimumSize = new Size(90, 0) };

    private bool _closeWhenFinished;

    public ProgressForm(IBackupService backupService, ILogger<ProgressForm> logger, IReadOnlyList<BackupJob> jobs)
    {
        _backupService = backupService;
        _logger = logger;
        _jobs = jobs;

        InitializeLayout();
        _actionButton.Click += (_, _) => OnActionClicked();
    }

    public event EventHandler<BackupRunResult>? BackupCompleted;

    public bool IsRunning { get; private set; }

    /// <summary>Cancels a running backup and closes the window once it has stopped.</summary>
    public void CancelAndClose()
    {
        _closeWhenFinished = true;
        if (IsRunning)
        {
            RequestCancellation();
        }
        else
        {
            Close();
        }
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await RunBackupAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (IsRunning)
        {
            // Keep the form alive until the backup has stopped; hide it if the user just closed the window.
            e.Cancel = true;
            if (e.CloseReason == CloseReason.UserClosing && !_closeWhenFinished)
            {
                Hide();
            }
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellation.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeLayout()
    {
        Text = UiText.ProgressTitle;
        Icon = SystemIcons.Application;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 250);
        Padding = new Padding(12);

        _jobLabel.Font = new Font(Font, FontStyle.Bold);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Control[] rows =
        [
            _jobLabel,
            _statusLabel,
            CreateCaption(UiText.CurrentFileCaption),
            _fileLabel,
            _fileProgress,
            CreateCaption(UiText.JobProgressCaption),
            _jobProgress,
        ];

        foreach (var control in rows)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(control);
        }

        // Last row takes the remaining space, with the button pinned to its bottom-right.
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _actionButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        layout.Controls.Add(_actionButton);
        layout.RowCount = layout.RowStyles.Count;

        Controls.Add(layout);
    }

    private static ProgressBar CreateProgressBar() => new()
    {
        Dock = DockStyle.Fill,
        Height = 18,
        Maximum = ProgressBarMaximum,
        MarqueeAnimationSpeed = 30,
    };

    private static Label CreateCaption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(3, 10, 3, 0),
    };

    private async Task RunBackupAsync()
    {
        IsRunning = true;
        try
        {
            var result = await _backupService.RunAsync(_jobs, new Progress<BackupProgress>(Render), _cancellation.Token);
            IsRunning = false;
            ShowResult(result);
            BackupCompleted?.Invoke(this, result);
        }
        catch (OperationCanceledException)
        {
            IsRunning = false;
            _statusLabel.Text = UiText.RunCancelled;
        }
        catch (Exception ex)
        {
            // Top-level UI boundary: anything unexpected is logged and shown instead of crashing the tray app.
            IsRunning = false;
            _logger.LogError(ex, "Backup run failed.");
            _statusLabel.Text = UiText.RunFailed(ex.Message);
        }

        _actionButton.Text = UiText.Close;
        _actionButton.Enabled = true;

        if (_closeWhenFinished || !Visible)
        {
            Close();
        }
    }

    private void Render(BackupProgress progress)
    {
        // Late reports can arrive after completion or after the user asked to cancel.
        if (!IsRunning || _cancellation.IsCancellationRequested || IsDisposed)
        {
            return;
        }

        _jobLabel.Text = UiText.JobHeader(progress.JobNumber, progress.JobCount, progress.JobName);

        switch (progress.Phase)
        {
            case BackupPhase.Scanning:
                SetIndeterminate(true);
                _statusLabel.Text = UiText.Scanning(progress.TotalFiles);
                _fileLabel.Text = string.Empty;
                break;

            case BackupPhase.Copying:
                SetIndeterminate(false);
                _statusLabel.Text = UiText.Copying(
                    progress.FilesProcessed,
                    progress.TotalFiles,
                    ByteSizeFormatter.Format(progress.BytesProcessed),
                    ByteSizeFormatter.Format(progress.TotalBytes));
                _fileLabel.Text = progress.CurrentFile ?? string.Empty;
                SetPercent(_fileProgress, progress.CurrentFilePercent);
                SetPercent(_jobProgress, progress.JobPercent);
                break;

            case BackupPhase.Finished:
                SetIndeterminate(false);
                SetPercent(_fileProgress, FullPercent);
                SetPercent(_jobProgress, FullPercent);
                break;
        }
    }

    private void ShowResult(BackupRunResult result)
    {
        _jobLabel.Text = RunResultFormatter.GetTitle(result);
        _statusLabel.Text = RunResultFormatter.Summarize(result);
        _fileLabel.Text = string.Empty;
        SetIndeterminate(false);
        SetPercent(_fileProgress, FullPercent);
        SetPercent(_jobProgress, FullPercent);
    }

    private void SetIndeterminate(bool indeterminate)
    {
        var style = indeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
        if (_fileProgress.Style != style)
        {
            _fileProgress.Style = style;
            _jobProgress.Style = style;
        }
    }

    private static void SetPercent(ProgressBar bar, double percent) =>
        bar.Value = (int)Math.Round(Math.Clamp(percent, 0, FullPercent) / FullPercent * ProgressBarMaximum);

    private void OnActionClicked()
    {
        if (IsRunning)
        {
            RequestCancellation();
        }
        else
        {
            Close();
        }
    }

    private void RequestCancellation()
    {
        _actionButton.Enabled = false;
        _statusLabel.Text = UiText.Cancelling;
        _cancellation.Cancel();
    }
}
