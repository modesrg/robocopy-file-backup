using FolderBackup.App.Constants;
using FolderBackup.Core.Models;

namespace FolderBackup.App.Presentation;

internal static class RunResultFormatter
{
    public static string GetTitle(BackupRunResult result) => result switch
    {
        { Status: BackupRunStatus.AlreadyRunning } => UiText.ResultTitleAlreadyRunning,
        { AllSucceeded: true } => UiText.ResultTitleSuccess,
        _ => UiText.ResultTitleProblems,
    };

    public static ToolTipIcon GetIcon(BackupRunResult result) =>
        result.AllSucceeded ? ToolTipIcon.Info : ToolTipIcon.Warning;

    public static string Summarize(BackupRunResult result)
    {
        if (result.Status == BackupRunStatus.AlreadyRunning)
        {
            return UiText.ResultAlreadyRunning;
        }

        return result.Jobs.Count == 0
            ? UiText.ResultNoJobs
            : string.Join(Environment.NewLine, result.Jobs.Select(Describe));
    }

    private static string Describe(BackupJobResult job) => job.Status switch
    {
        BackupJobStatus.Succeeded => UiText.JobSucceeded(job.Job.Name, job.FilesCopied, ByteSizeFormatter.Format(job.BytesCopied)),
        BackupJobStatus.Failed => UiText.JobFailed(job.Job.Name, job.Messages.Count, job.Messages.FirstOrDefault() ?? string.Empty),
        BackupJobStatus.Skipped => UiText.JobSkipped(job.Job.Name, job.Messages.FirstOrDefault() ?? string.Empty),
        BackupJobStatus.Cancelled => UiText.JobCancelled(job.Job.Name),
        _ => job.Job.Name,
    };
}
