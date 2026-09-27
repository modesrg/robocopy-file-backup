namespace FolderBackup.Core.Models;

/// <summary>
/// Robocopy's exit code is a bit field. Values below 8 are all successes.
/// </summary>
[Flags]
public enum RobocopyExitCode
{
    NoChange = 0,
    FilesCopied = 1,
    ExtraFilesOrDirectories = 2,
    MismatchedFiles = 4,
    CopyErrors = 8,
    FatalError = 16,
}
