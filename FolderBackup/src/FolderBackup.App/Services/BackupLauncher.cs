using FolderBackup.App.Forms;
using FolderBackup.Core.Models;
using FolderBackup.Core.Repositories;

namespace FolderBackup.App.Services;

internal sealed class BackupLauncher : IBackupLauncher
{
    private readonly IFormFactory _formFactory;
    private readonly ISettingsRepository _settingsRepository;
    private ProgressForm? _progressForm;

    public BackupLauncher(IFormFactory formFactory, ISettingsRepository settingsRepository)
    {
        _formFactory = formFactory;
        _settingsRepository = settingsRepository;
    }

    public event EventHandler<BackupRunResult>? RunCompleted;

    public event EventHandler? ProgressWindowClosed;

    public bool IsRunning => _progressForm is { IsRunning: true };

    public void Start(IReadOnlyList<BackupJob> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        if (TryShowRunningBackup())
        {
            return;
        }

        // A finished run's result window may still be open; replace it with the new run.
        _progressForm?.Close();

        var form = _formFactory.Create<ProgressForm>(jobs);
        form.BackupCompleted += (_, result) => RunCompleted?.Invoke(this, result);
        form.FormClosed += OnProgressFormClosed;
        _progressForm = form;
        form.Show();
    }

    public async Task StartEnabledJobsAsync(CancellationToken ct = default)
    {
        if (TryShowRunningBackup())
        {
            return;
        }

        var settings = await _settingsRepository.LoadAsync(ct);
        Start([.. settings.Jobs.Where(job => job.IsEnabled)]);
    }

    public void CancelAndClose() => _progressForm?.CancelAndClose();

    private bool TryShowRunningBackup()
    {
        if (_progressForm is not { IsRunning: true } running)
        {
            return false;
        }

        running.Show();
        running.Activate();
        return true;
    }

    private void OnProgressFormClosed(object? sender, FormClosedEventArgs e)
    {
        if (ReferenceEquals(sender, _progressForm))
        {
            _progressForm = null;
        }

        ProgressWindowClosed?.Invoke(this, EventArgs.Empty);
    }
}
