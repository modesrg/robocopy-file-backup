using System.Globalization;
using System.Text;
using FolderBackup.Core.Constants;

namespace FolderBackup.Core.Services;

internal sealed class FileBackupLog : IBackupLog
{
    private readonly StreamWriter _writer;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _gate = new();

    public FileBackupLog(string filePath, TimeProvider timeProvider)
    {
        FilePath = filePath;
        _timeProvider = timeProvider;
        _writer = new StreamWriter(filePath, append: true, Encoding.UTF8) { AutoFlush = true };
    }

    public string FilePath { get; }

    public void WriteLine(string message)
    {
        var timestamp = _timeProvider.GetLocalNow().ToString(AppConstants.LogLineTimestampFormat, CultureInfo.InvariantCulture);
        lock (_gate)
        {
            _writer.WriteLine($"{timestamp}  {message}");
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _writer.Dispose();
        }
    }
}
