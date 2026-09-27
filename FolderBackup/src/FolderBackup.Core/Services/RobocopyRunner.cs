using System.Globalization;
using System.Text;
using FolderBackup.Core.Constants;
using FolderBackup.Core.Models;
using FolderBackup.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FolderBackup.Core.Services;

public sealed class RobocopyRunner : IRobocopyRunner
{
    private static readonly Encoding ConsoleEncoding = CreateConsoleEncoding();

    private readonly IProcessRunner _processRunner;
    private readonly IRobocopyOutputParser _parser;
    private readonly RobocopyOptions _options;
    private readonly ILogger<RobocopyRunner> _logger;

    public RobocopyRunner(
        IProcessRunner processRunner,
        IRobocopyOutputParser parser,
        IOptions<RobocopyOptions> options,
        ILogger<RobocopyRunner> logger)
    {
        _processRunner = processRunner;
        _parser = parser;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RobocopyExitCode> RunAsync(
        IReadOnlyList<string> arguments,
        Action<RobocopyOutputLine> onOutput,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(onOutput);

        var request = new ProcessRequest(ResolveExecutablePath(), arguments, ConsoleEncoding);
        _logger.LogDebug("Running {Executable} {Arguments}", request.FileName, string.Join(' ', arguments));

        var exitCode = await _processRunner
            .RunAsync(request, line => onOutput(_parser.Parse(line)), ct)
            .ConfigureAwait(false);

        return (RobocopyExitCode)exitCode;
    }

    private string ResolveExecutablePath() =>
        string.IsNullOrWhiteSpace(_options.ExecutablePath)
            ? Path.Combine(Environment.SystemDirectory, AppConstants.RobocopyExecutableName)
            : _options.ExecutablePath;

    /// <summary>Redirected robocopy output uses the console's OEM code page, not UTF-8.</summary>
    private static Encoding CreateConsoleEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
    }
}
