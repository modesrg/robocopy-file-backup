using System.Text;

namespace FolderBackup.Core.Models;

public sealed record ProcessRequest(string FileName, IReadOnlyList<string> Arguments, Encoding OutputEncoding);
