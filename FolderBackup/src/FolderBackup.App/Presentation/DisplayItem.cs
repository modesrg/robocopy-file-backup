namespace FolderBackup.App.Presentation;

/// <summary>A value with a friendly label, for combo boxes and lists.</summary>
internal sealed record DisplayItem<T>(T Value, string Text)
{
    public override string ToString() => Text;
}
