using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public interface IRobocopyRunner
{
    Task<RobocopyExitCode> RunAsync(
        IReadOnlyList<string> arguments,
        Action<RobocopyOutputLine> onOutput,
        CancellationToken ct = default);
}
