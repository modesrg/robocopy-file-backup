namespace FolderBackup.Core.Constants;

public static class CommandLineArguments
{
    /// <summary>Runs all enabled jobs without any UI, then exits. Used by the scheduled task.</summary>
    public const string RunBackup = "--run";
}
