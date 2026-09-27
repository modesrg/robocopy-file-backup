namespace FolderBackup.Core.Models;

/// <summary>One parsed line of robocopy output.</summary>
public abstract record RobocopyOutputLine(string RawText);

/// <summary>A file robocopy is about to copy (or, with /L, would copy).</summary>
public sealed record RobocopyFileLine(string RawText, long SizeBytes, string Path) : RobocopyOutputLine(RawText);

/// <summary>Copy progress of the current file.</summary>
public sealed record RobocopyPercentLine(string RawText, double Percent) : RobocopyOutputLine(RawText);

/// <summary>An error reported for a file or folder.</summary>
public sealed record RobocopyErrorLine(string RawText) : RobocopyOutputLine(RawText);

/// <summary>Anything else: retry notices, error descriptions, blank lines.</summary>
public sealed record RobocopyInfoLine(string RawText) : RobocopyOutputLine(RawText);
