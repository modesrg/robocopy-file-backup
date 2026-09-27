using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public interface IProcessRunner
{
    /// <summary>
    /// Runs a process to completion, passing each output line to <paramref name="onOutputLine"/>.
    /// Callbacks are never invoked concurrently. Cancelling kills the process.
    /// </summary>
    Task<int> RunAsync(ProcessRequest request, Action<string> onOutputLine, CancellationToken ct = default);
}
