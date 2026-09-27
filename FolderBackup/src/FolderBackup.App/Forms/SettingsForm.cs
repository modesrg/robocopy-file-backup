using System.Globalization;
using FolderBackup.App.Constants;
using FolderBackup.App.Presentation;
using FolderBackup.App.Services;
using FolderBackup.Core.Models;
using FolderBackup.Core.Repositories;
using FolderBackup.Core.Services;
using Microsoft.Extensions.Logging;

namespace FolderBackup.App.Forms;

internal sealed class SettingsForm : Form
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IScheduleService _scheduleService;
    private readonly IBackupTargetResolver _targetResolver;
    private readonly IBackupLauncher _backupLauncher;
    private readonly IFormFactory _formFactory;
    private readonly ILogger<SettingsForm> _logger;

    private readonly ListView _jobList = new()
    {
        View = View.Details,
        CheckBoxes = true,
        FullRowSelect = true,
        MultiSelect = false,
        HideSelection = false,
        Dock = DockStyle.Fill,
    };
    private readonly Button _addButton = CreateButton(UiText.Add);
    private readonly Button _editButton = CreateButton(UiText.Edit);
    private readonly Button _removeButton = CreateButton(UiText.Remove);
    private readonly Button _runButton = CreateButton(UiText.RunNow);

    private readonly CheckBox _scheduleEnabled = new() { Text = UiText.RunAutomatically, AutoSize = true };
    private readonly ComboBox _frequency = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly DateTimePicker _time = new() { Format = DateTimePickerFormat.Time, ShowUpDown = true, Width = 110 };
    private readonly FlowLayoutPanel _daysPanel = new() { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
    private readonly Dictionary<DayOfWeek, CheckBox> _dayCheckBoxes = new();
    private readonly Label _nextRunLabel = new() { AutoSize = true, ForeColor = SystemColors.GrayText };

    private readonly Button _saveButton = CreateButton(UiText.Save);
    private readonly Button _cancelButton = CreateButton(UiText.Cancel);

    public SettingsForm(
        ISettingsRepository settingsRepository,
        IScheduleService scheduleService,
        IBackupTargetResolver targetResolver,
        IBackupLauncher backupLauncher,
        IFormFactory formFactory,
        ILogger<SettingsForm> logger)
    {
        _settingsRepository = settingsRepository;
        _scheduleService = scheduleService;
        _targetResolver = targetResolver;
        _backupLauncher = backupLauncher;
        _formFactory = formFactory;
        _logger = logger;

        InitializeLayout();
        WireEvents();
    }

    // ---------------------------------------------------------------- Layout

    private void InitializeLayout()
    {
        Text = UiText.SettingsTitle;
        Icon = SystemIcons.Application;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(860, 560);
        MinimumSize = new Size(680, 460);
        Padding = new Padding(10);
        CancelButton = _cancelButton;

        _jobList.Columns.Add(UiText.ColumnName, 150);
        _jobList.Columns.Add(UiText.ColumnSource, 240);
        _jobList.Columns.Add(UiText.ColumnDestination, 240);
        _jobList.Columns.Add(UiText.ColumnMode, 130);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(CreateJobsGroup(), 0, 0);
        root.Controls.Add(CreateScheduleGroup(), 0, 1);
        root.Controls.Add(CreateDialogButtons(), 0, 2);

        Controls.Add(root);
    }

    private GroupBox CreateJobsGroup()
    {
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill };
        _runButton.Margin = new Padding(3, 15, 3, 3);
        buttons.Controls.AddRange([_addButton, _editButton, _removeButton, _runButton]);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_jobList, 0, 0);
        layout.Controls.Add(buttons, 1, 0);

        var group = new GroupBox { Text = UiText.JobsGroup, Dock = DockStyle.Fill, Padding = new Padding(8) };
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox CreateScheduleGroup()
    {
        foreach (var day in GetDaysInCultureOrder())
        {
            var checkBox = new CheckBox
            {
                Text = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(day),
                AutoSize = true,
            };
            _dayCheckBoxes[day] = checkBox;
            _daysPanel.Controls.Add(checkBox);
        }

        _frequency.Items.AddRange(DisplayChoices.Frequencies.ToArray<object>());

        var whenRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        whenRow.Controls.AddRange([CreateLabel(UiText.FrequencyLabel), _frequency, _daysPanel, CreateLabel(UiText.AtLabel), _time]);

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Dock = DockStyle.Top,
        };
        layout.Controls.AddRange([_scheduleEnabled, whenRow, _nextRunLabel]);

        var group = new GroupBox
        {
            Text = UiText.ScheduleGroup,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(8),
        };
        group.Controls.Add(layout);
        return group;
    }

    private FlowLayoutPanel CreateDialogButtons()
    {
        var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        panel.Controls.AddRange([_cancelButton, _saveButton]);
        return panel;
    }

    private static Button CreateButton(string text) => new() { Text = text, AutoSize = true, MinimumSize = new Size(90, 0) };

    private static Label CreateLabel(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 7, 3, 3) };

    private static IEnumerable<DayOfWeek> GetDaysInCultureOrder()
    {
        var first = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        return Enumerable.Range(0, 7).Select(offset => (DayOfWeek)((first + offset) % 7));
    }

    // ---------------------------------------------------------------- Events

    private void WireEvents()
    {
        Load += async (_, _) => await LoadSettingsAsync();
        _jobList.SelectedIndexChanged += (_, _) => UpdateButtonStates();
        _jobList.DoubleClick += (_, _) => EditSelectedJob();
        _addButton.Click += (_, _) => AddJob();
        _editButton.Click += (_, _) => EditSelectedJob();
        _removeButton.Click += (_, _) => RemoveSelectedJob();
        _runButton.Click += (_, _) => RunSelectedJob();
        _scheduleEnabled.CheckedChanged += (_, _) => UpdateScheduleControls();
        _frequency.SelectedIndexChanged += (_, _) => UpdateScheduleControls();
        _saveButton.Click += async (_, _) => await SaveAsync();
        _cancelButton.Click += (_, _) => Close();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsRepository.LoadAsync();
            PopulateJobs(settings.Jobs);
            PopulateSchedule(settings.Schedule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings.");
            ShowError(UiText.LoadSettingsFailed, ex);
        }

        UpdateButtonStates();
        UpdateScheduleControls();
        UpdateNextRunLabel();
    }

    private async Task SaveAsync()
    {
        var schedule = ReadSchedule();
        if (schedule is { IsEnabled: true, Frequency: ScheduleFrequency.Weekly, DaysOfWeek.Count: 0 })
        {
            MessageBox.Show(this, UiText.PickAtLeastOneDay, UiText.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var settings = new BackupSettings { Jobs = ReadJobs(), Schedule = schedule };

        _saveButton.Enabled = false;
        try
        {
            await _settingsRepository.SaveAsync(settings);
            _scheduleService.Apply(schedule);
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings.");
            ShowError(UiText.SaveSettingsFailed, ex);
        }
        finally
        {
            _saveButton.Enabled = true;
        }
    }

    // ---------------------------------------------------------------- Jobs

    private void PopulateJobs(IEnumerable<BackupJob> jobs)
    {
        _jobList.BeginUpdate();
        _jobList.Items.Clear();
        foreach (var job in jobs)
        {
            _jobList.Items.Add(CreateItem(job));
        }

        _jobList.EndUpdate();
    }

    private ListViewItem CreateItem(BackupJob job)
    {
        var item = new ListViewItem(job.Name) { Checked = job.IsEnabled, Tag = job };
        item.SubItems.Add(job.SourcePath);
        item.SubItems.Add(_targetResolver.TryGetTargetPath(job, out var targetPath) ? targetPath : job.DestinationPath);
        item.SubItems.Add(DisplayChoices.GetMode(job.Mode).Text);
        return item;
    }

    /// <summary>The list's checkboxes are the source of truth for "enabled".</summary>
    private static BackupJob ReadJob(ListViewItem item) => (BackupJob)item.Tag! with { IsEnabled = item.Checked };

    private List<BackupJob> ReadJobs() => _jobList.Items.Cast<ListViewItem>().Select(ReadJob).ToList();

    private void AddJob()
    {
        if (TryEditJob(new BackupJob(), out var created))
        {
            _jobList.Items.Add(CreateItem(created)).Selected = true;
        }
    }

    private void EditSelectedJob()
    {
        if (_jobList.SelectedItems is not [var item])
        {
            return;
        }

        if (TryEditJob(ReadJob(item), out var updated))
        {
            _jobList.Items[item.Index] = CreateItem(updated);
        }
    }

    private bool TryEditJob(BackupJob job, out BackupJob result)
    {
        IReadOnlyList<BackupJob> otherJobs = [.. ReadJobs().Where(other => other.Id != job.Id)];
        using var editor = _formFactory.Create<JobEditorForm>(job, otherJobs);
        var accepted = editor.ShowDialog(this) == DialogResult.OK;
        result = editor.Job;
        return accepted;
    }

    private void RemoveSelectedJob()
    {
        if (_jobList.SelectedItems is not [var item])
        {
            return;
        }

        var answer = MessageBox.Show(this, UiText.ConfirmRemoveJob(item.Text), UiText.AppTitle,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer == DialogResult.Yes)
        {
            _jobList.Items.Remove(item);
        }
    }

    /// <summary>Runs the selected job as it's shown in the list, even if it's disabled or not saved yet.</summary>
    private void RunSelectedJob()
    {
        if (_jobList.SelectedItems is [var item])
        {
            _backupLauncher.Start([ReadJob(item)]);
        }
    }

    private void UpdateButtonStates()
    {
        var hasSelection = _jobList.SelectedItems.Count == 1;
        _editButton.Enabled = hasSelection;
        _removeButton.Enabled = hasSelection;
        _runButton.Enabled = hasSelection;
    }

    // ---------------------------------------------------------------- Schedule

    private void PopulateSchedule(BackupSchedule schedule)
    {
        _scheduleEnabled.Checked = schedule.IsEnabled;
        _frequency.SelectedItem = DisplayChoices.GetFrequency(schedule.Frequency);
        _time.Value = DateTime.Today.Add(schedule.TimeOfDay.ToTimeSpan());

        foreach (var (day, checkBox) in _dayCheckBoxes)
        {
            checkBox.Checked = schedule.DaysOfWeek.Contains(day);
        }
    }

    private BackupSchedule ReadSchedule() => new()
    {
        IsEnabled = _scheduleEnabled.Checked,
        Frequency = SelectedFrequency(),
        TimeOfDay = new TimeOnly(_time.Value.Hour, _time.Value.Minute),
        DaysOfWeek = [.. _dayCheckBoxes.Where(pair => pair.Value.Checked).Select(pair => pair.Key)],
    };

    private ScheduleFrequency SelectedFrequency() =>
        (_frequency.SelectedItem as DisplayItem<ScheduleFrequency>)?.Value ?? ScheduleFrequency.Daily;

    private void UpdateScheduleControls()
    {
        var enabled = _scheduleEnabled.Checked;
        _frequency.Enabled = enabled;
        _time.Enabled = enabled;
        _daysPanel.Enabled = enabled;
        _daysPanel.Visible = SelectedFrequency() == ScheduleFrequency.Weekly;
    }

    private void UpdateNextRunLabel()
    {
        try
        {
            var nextRun = _scheduleService.GetNextRunTime();
            _nextRunLabel.Text = nextRun is { } when ? UiText.NextRun(when) : UiText.NotScheduled;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read the scheduled task.");
            _nextRunLabel.Text = UiText.NextRunUnknown;
        }
    }

    private void ShowError(string message, Exception ex) =>
        MessageBox.Show(this, $"{message}{Environment.NewLine}{Environment.NewLine}{ex.Message}", UiText.AppTitle,
            MessageBoxButtons.OK, MessageBoxIcon.Error);
}
