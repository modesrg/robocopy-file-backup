using FolderBackup.Core.Models;

namespace FolderBackup.Core.Extensions;

public static class RobocopyExitCodeExtensions
{
    private const RobocopyExitCode FailureFlags = RobocopyExitCode.CopyErrors | RobocopyExitCode.FatalError;

    /// <summary>Robocopy returns 0–7 for success; 8 and above mean at least one failure.</summary>
    public static bool IsSuccess(this RobocopyExitCode exitCode) => (exitCode & FailureFlags) == 0;
}
