using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

public interface IRobocopyOutputParser
{
    RobocopyOutputLine Parse(string line);
}
