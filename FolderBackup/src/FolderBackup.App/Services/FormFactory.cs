using Microsoft.Extensions.DependencyInjection;

namespace FolderBackup.App.Services;

internal sealed class FormFactory : IFormFactory
{
    private readonly IServiceProvider _services;

    public FormFactory(IServiceProvider services)
    {
        _services = services;
    }

    public TForm Create<TForm>(params object[] arguments) where TForm : Form =>
        ActivatorUtilities.CreateInstance<TForm>(_services, arguments);
}
