namespace FolderBackup.App.Constants;

/// <summary>Exit codes for scheduled (headless) runs, visible in Task Scheduler's "Last Run Result".</summary>
internal static class ProcessExitCodes
{
    public const int Success = 0;
    public const int BackupFailed = 1;
    public const int AlreadyRunning = 2;
}
