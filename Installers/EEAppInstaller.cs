using EditorEnhanced.Configurations;
using Zenject;

namespace EditorEnhanced.Installers;

internal class EEAppInstaller : Installer
{
    private readonly PluginConfigModel _pluginConfigModel;

    public EEAppInstaller(PluginConfigModel pluginConfigModel)
    {
        _pluginConfigModel = pluginConfigModel;
    }

    public override void InstallBindings()
    {
        Container.BindInstance(_pluginConfigModel).AsSingle();
        Container.Bind<PluginConfig>().FromInstance(new PluginConfig(_pluginConfigModel)).AsSingle();
    }
}