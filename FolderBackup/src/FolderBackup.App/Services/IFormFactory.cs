namespace FolderBackup.App.Services;

internal interface IFormFactory
{
    /// <summary>
    /// Creates a form, resolving its constructor dependencies from DI.
    /// Extra <paramref name="arguments"/> fill constructor parameters that DI can't (like the job to edit).
    /// </summary>
    TForm Create<TForm>(params object[] arguments) where TForm : Form;
}
