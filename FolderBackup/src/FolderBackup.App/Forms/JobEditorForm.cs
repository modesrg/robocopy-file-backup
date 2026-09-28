using FolderBackup.App.Constants;
using FolderBackup.App.Presentation;
using FolderBackup.Core.Models;
using FolderBackup.Core.Services;

namespace FolderBackup.App.Forms;

internal sealed class JobEditorForm : Form
{
    private const int FieldWidth = 420;

    private readonly IBackupJobValidator _validator;
    private readonly IBackupTargetResolver _targetResolver;
    private readonly BackupJob _original;
    private readonly IReadOnlyList<BackupJob> _otherJobs;

    private readonly TextBox _nameBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _sourceBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _destinationBox = new() { Dock = DockStyle.Fill };
    private readonly Label _targetPreview = new()
    {
        AutoSize = true,
        MaximumSize = new Size(FieldWidth, 0),
        ForeColor = SystemColors.GrayText,
    };
    private readonly ComboBox _modeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly Label _modeDescription = new()
    {
        AutoSize = true,
        MaximumSize = new Size(FieldWidth, 0),
        ForeColor = SystemColors.GrayText,
    };
    private readonly Button _okButton = new() { Text = UiText.Ok, AutoSize = true };
    private readonly Button _cancelButton = new() { Text = UiText.Cancel, AutoSize = true, DialogResult = DialogResult.Cancel };

    /// <param name="otherJobs">The other configured jobs, so two jobs can't back up into the same folder.</param>
    public JobEditorForm(
        IBackupJobValidator validator,
        IBackupTargetResolver targetResolver,
        BackupJob job,
        IReadOnlyList<BackupJob> otherJobs)
    {
        _validator = validator;
        _targetResolver = targetResolver;
        _original = job;
        _otherJobs = otherJobs;
        Job = job;

        InitializeLayout();
        PopulateFields(job);

        _sourceBox.TextChanged += (_, _) => UpdateTargetPreview();
        _destinationBox.TextChanged += (_, _) => UpdateTargetPreview();
        _modeBox.SelectedIndexChanged += (_, _) => UpdateModeDescription();
        _okButton.Click += (_, _) => Accept();
    }

    /// <summary>The edited job. Only meaningful when the dialog returns <see cref="DialogResult.OK"/>.</summary>
    public BackupJob Job { get; private set; }

    private void InitializeLayout()
    {
        Text = string.IsNullOrEmpty(_original.Name) ? UiText.NewJobTitle : UiText.EditJobTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);
        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        _modeBox.Items.AddRange(DisplayChoices.Modes.ToArray<object>());

        var layout = new TableLayoutPanel
        {
            ColumnCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, FieldWidth));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddRow(layout, UiText.JobNameLabel, _nameBox);
        AddRow(layout, UiText.SourceLabel, _sourceBox, CreateBrowseButton(_sourceBox, UiText.PickSource));
        AddRow(layout, UiText.DestinationLabel, _destinationBox, CreateBrowseButton(_destinationBox, UiText.PickDestination));
        AddRow(layout, string.Empty, _targetPreview);
        AddRow(layout, UiText.ModeLabel, _modeBox);
        AddRow(layout, string.Empty, _modeDescription);
        AddButtonRow(layout);

        Controls.Add(layout);
    }

    private static void AddRow(TableLayoutPanel layout, string labelText, Control field, Control? trailing = null)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(field, 1, row);

        if (trailing is not null)
        {
            layout.Controls.Add(trailing, 2, row);
        }
    }

    private void AddButtonRow(TableLayoutPanel layout)
    {
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
        };
        buttons.Controls.AddRange([_cancelButton, _okButton]);

        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(buttons, 0, row);
        layout.SetColumnSpan(buttons, layout.ColumnCount);
    }

    private Button CreateBrowseButton(TextBox target, string description)
    {
        var button = new Button { Text = UiText.Browse, AutoSize = true };
        button.Click += (_, _) => BrowseInto(target, description);
        return button;
    }

    private void BrowseInto(TextBox target, string description)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
        };

        if (Directory.Exists(target.Text))
        {
            dialog.SelectedPath = target.Text;
        }

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        target.Text = dialog.SelectedPath;

        // Suggest a name from the source folder, e.g. "Pictures".
        if (target == _sourceBox && string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            _nameBox.Text = Path.GetFileName(Path.TrimEndingDirectorySeparator(dialog.SelectedPath));
        }
    }

    private void PopulateFields(BackupJob job)
    {
        _nameBox.Text = job.Name;
        _sourceBox.Text = job.SourcePath;
        _destinationBox.Text = job.DestinationPath;
        _modeBox.SelectedItem = DisplayChoices.GetMode(job.Mode);
        UpdateModeDescription();
        UpdateTargetPreview();
    }

    /// <summary>Shows where the files will really go, e.g. "E:\Backup" + "Pictures" = "E:\Backup\Pictures".</summary>
    private void UpdateTargetPreview() =>
        _targetPreview.Text = _targetResolver.TryGetTargetPath(BuildCandidate(), out var targetPath)
            ? UiText.TargetPreview(targetPath)
            : string.Empty;

    private void UpdateModeDescription() => _modeDescription.Text = DisplayChoices.GetModeDescription(SelectedMode());

    private BackupMode SelectedMode() =>
        (_modeBox.SelectedItem as DisplayItem<BackupMode>)?.Value ?? BackupMode.AppendOnly;

    private BackupJob BuildCandidate() => _original with
    {
        Name = _nameBox.Text.Trim(),
        SourcePath = _sourceBox.Text.Trim(),
        DestinationPath = _destinationBox.Text.Trim(),
        Mode = SelectedMode(),
    };

    private void Accept()
    {
        var candidate = BuildCandidate();
        var validation = _validator.ValidateConfiguration(candidate, _otherJobs);
        if (!validation.IsValid)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, validation.Errors), UiText.FixProblemsTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Job = candidate;
        DialogResult = DialogResult.OK;
    }
}
