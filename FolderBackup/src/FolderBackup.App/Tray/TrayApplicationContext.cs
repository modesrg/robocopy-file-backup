using System.ComponentModel;
using System.Diagnostics;
using FolderBackup.App.Constants;
using FolderBackup.App.Forms;
using FolderBackup.App.Presentation;
using FolderBackup.App.Services;
using FolderBackup.Core.Models;
using FolderBackup.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FolderBackup.App.Tray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private const int BalloonTimeoutMilliseconds = 5000;
    private const int MaxBalloonTextLength = 250;
    private const string Ellipsis = "…";

    private readonly IBackupLauncher _backupLauncher;
    private readonly IFormFactory _formFactory;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<TrayApplicationContext> _logger;
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _notifyIcon;

    private SettingsForm? _settingsForm;
    private bool _exitRequested;

    public TrayApplicationContext(
        IBackupLauncher backupLauncher,
        IFormFactory formFactory,
        IOptions<StorageOptions> storageOptions,
        ILogger<TrayApplicationContext> logger)
    {
        _backupLauncher = backupLauncher;
        _formFactory = formFactory;
        _storageOptions = storageOptions.Value;
        _logger = logger;

        _menu = BuildMenu();
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = UiText.AppTitle,
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();

        _backupLauncher.RunCompleted += OnRunCompleted;
        _backupLauncher.ProgressWindowClosed += OnProgressWindowClosed;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _backupLauncher.RunCompleted -= OnRunCompleted;
            _backupLauncher.ProgressWindowClosed -= OnProgressWindowClosed;
            _notifyIcon.Dispose();
            _menu.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(UiText.MenuBackUpNow, null, async (_, _) => await BackUpEnabledJobsAsync());
        menu.Items.Add(UiText.MenuSettings, null, (_, _) => ShowSettings());
        menu.Items.Add(UiText.MenuOpenLogs, null, (_, _) => OpenLogFolder());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(UiText.MenuExit, null, (_, _) => RequestExit());
        return menu;
    }

    private async Task BackUpEnabledJobsAsync()
    {
        try
        {
            await _backupLauncher.StartEnabledJobsAsync();
        }
        catch (Exception ex)
        {
            // Top-level UI boundary, e.g. an unreadable settings file.
            _logger.LogError(ex, "Could not start the backup.");
            MessageBox.Show($"{UiText.LoadSettingsFailed}{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                UiText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnRunCompleted(object? sender, BackupRunResult result)
    {
        var text = RunResultFormatter.Summarize(result);
        if (text.Length > MaxBalloonTextLength)
        {
            text = string.Concat(text.AsSpan(0, MaxBalloonTextLength - Ellipsis.Length), Ellipsis);
        }

        _notifyIcon.ShowBalloonTip(BalloonTimeoutMilliseconds, RunResultFormatter.GetTitle(result), text, RunResultFormatter.GetIcon(result));
    }

    private void OnProgressWindowClosed(object? sender, EventArgs e)
    {
        if (_exitRequested && !_backupLauncher.IsRunning)
        {
            ExitApplication();
        }
    }

    private void ShowSettings()
    {
        if (_settingsForm is null)
        {
            _settingsForm = _formFactory.Create<SettingsForm>();
            _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        }

        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(_storageOptions.LogDirectory);
            Process.Start(new ProcessStartInfo { FileName = _storageOptions.LogDirectory, UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not open the log folder.");
            MessageBox.Show(UiText.OpenLogsFailed, UiText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RequestExit()
    {
        if (!_backupLauncher.IsRunning)
        {
            ExitApplication();
            return;
        }

        var answer = MessageBox.Show(UiText.ConfirmExitDuringBackup, UiText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes)
        {
            return;
        }

        // Wait for robocopy to be stopped before exiting, so it isn't left running on its own.
        _exitRequested = true;
        _backupLauncher.CancelAndClose();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _settingsForm?.Close();
        ExitThread();
    }
}
