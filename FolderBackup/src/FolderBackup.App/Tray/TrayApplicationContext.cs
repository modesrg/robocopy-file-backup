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

    private readonly IFormFactory _formFactory;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<TrayApplicationContext> _logger;
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _notifyIcon;

    private ProgressForm? _progressForm;
    private SettingsForm? _settingsForm;
    private bool _exitRequested;

    public TrayApplicationContext(
        IFormFactory formFactory,
        IOptions<StorageOptions> storageOptions,
        ILogger<TrayApplicationContext> logger)
    {
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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Dispose();
            _menu.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(UiText.MenuBackUpNow, null, (_, _) => StartBackup());
        menu.Items.Add(UiText.MenuSettings, null, (_, _) => ShowSettings());
        menu.Items.Add(UiText.MenuOpenLogs, null, (_, _) => OpenLogFolder());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(UiText.MenuExit, null, (_, _) => RequestExit());
        return menu;
    }

    private void StartBackup()
    {
        if (_progressForm is { IsRunning: true })
        {
            // Already running (maybe hidden): just bring it back.
            _progressForm.Show();
            _progressForm.Activate();
            return;
        }

        // A finished run's result window may still be open; replace it with a fresh run.
        _progressForm?.Close();

        _progressForm = _formFactory.Create<ProgressForm>();
        _progressForm.BackupCompleted += OnBackupCompleted;
        _progressForm.FormClosed += OnProgressFormClosed;
        _progressForm.Show();
    }

    private void OnBackupCompleted(object? sender, BackupRunResult result)
    {
        var text = RunResultFormatter.Summarize(result);
        if (text.Length > MaxBalloonTextLength)
        {
            text = string.Concat(text.AsSpan(0, MaxBalloonTextLength - Ellipsis.Length), Ellipsis);
        }

        _notifyIcon.ShowBalloonTip(BalloonTimeoutMilliseconds, RunResultFormatter.GetTitle(result), text, RunResultFormatter.GetIcon(result));
    }

    private void OnProgressFormClosed(object? sender, FormClosedEventArgs e)
    {
        _progressForm = null;
        if (_exitRequested)
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
        if (_progressForm is not { IsRunning: true })
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
        _progressForm.CancelAndClose();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _settingsForm?.Close();
        ExitThread();
    }
}
