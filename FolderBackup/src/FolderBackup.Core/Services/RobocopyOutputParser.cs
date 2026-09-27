using System.Globalization;
using System.Text.RegularExpressions;
using FolderBackup.Core.Models;

namespace FolderBackup.Core.Services;

/// <summary>
/// Parses robocopy output produced with /NDL /FP /BYTES /NJH /NJS.
/// Robocopy's text is localized ("New File" is "Neue Datei" on German Windows), so lines are
/// recognized by their structure rather than by words.
/// </summary>
public sealed partial class RobocopyOutputParser : IRobocopyOutputParser
{
    private const string PercentGroup = "percent";
    private const string SizeGroup = "size";
    private const string PathGroup = "path";

    public RobocopyOutputLine Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (ErrorPattern().IsMatch(line))
        {
            return new RobocopyErrorLine(line);
        }

        var percentMatch = PercentPattern().Match(line);
        if (percentMatch.Success)
        {
            return new RobocopyPercentLine(line, ParsePercent(percentMatch.Groups[PercentGroup].Value));
        }

        var fileMatch = FilePattern().Match(line);
        if (fileMatch.Success)
        {
            var size = long.Parse(fileMatch.Groups[SizeGroup].Value, NumberStyles.None, CultureInfo.InvariantCulture);
            return new RobocopyFileLine(line, size, fileMatch.Groups[PathGroup].Value.Trim());
        }

        return new RobocopyInfoLine(line);
    }

    private static double ParsePercent(string value) =>
        double.Parse(value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    /// <summary>"2026/09/24 14:00:00 ERROR 32 (0x00000020) Copying File ...": the hex code is not localized.</summary>
    [GeneratedRegex(@"\(0x[0-9A-Fa-f]{8}\)")]
    private static partial Regex ErrorPattern();

    /// <summary>"  12.5%" (or "12,5%" on some locales).</summary>
    [GeneratedRegex(@"^\s*(?<percent>\d{1,3}(?:[.,]\d+)?)%\s*$")]
    private static partial Regex PercentPattern();

    /// <summary>"\t    New File  \t\t    1234\tC:\Path\File.jpg": class, size in bytes, full path.</summary>
    [GeneratedRegex(@"^\s*[^\t]*\t+\s*(?<size>\d+)\t(?<path>.+)$")]
    private static partial Regex FilePattern();
}
