using System.ComponentModel;
using System.Diagnostics;
using FolderBackup.Core.Models;
using Microsoft.Extensions.Logging;

namespace FolderBackup.Core.Services;

public sealed class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    public async Task<int> RunAsync(ProcessRequest request, Action<string> onOutputLine, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(onOutputLine);
        ct.ThrowIfCancellationRequested();

        using var process = new Process { StartInfo = CreateStartInfo(request) };
        var callbackGate = new Lock();

        void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            // stdout and stderr events can arrive on different threads; serialize them for the caller.
            lock (callbackGate)
            {
                try
                {
                    onOutputLine(e.Data);
                }
                catch (Exception ex)
                {
                    // An exception here would be unhandled on a thread-pool thread and crash the app.
                    _logger.LogError(ex, "Output handler failed for line: {Line}", e.Data);
                }
            }
        }

        process.OutputDataReceived += OnDataReceived;
        process.ErrorDataReceived += OnDataReceived;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Stop(process);
            throw;
        }

        return process.ExitCode;
    }

    private static ProcessStartInfo CreateStartInfo(ProcessRequest request)
    {
        var startInfo = new ProcessStartInfo(request.FileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = request.OutputEncoding,
            StandardErrorEncoding = request.OutputEncoding,
        };

        // ArgumentList handles quoting, so paths with spaces are passed safely.
        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private void Stop(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        catch (InvalidOperationException)
        {
            // The process already exited.
        }
        catch (Win32Exception ex)
        {
            _logger.LogWarning(ex, "Could not stop the process after cancellation.");
        }
    }
}
