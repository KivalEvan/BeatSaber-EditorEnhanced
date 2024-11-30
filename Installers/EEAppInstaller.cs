using Zenject;

namespace EditorEnhanced.Installers
{
    internal class EEAppInstaller : Installer
    {
        private readonly PluginConfig pluginConfig;

        public EEAppInstaller(PluginConfig pluginConfig)
        {
            this.pluginConfig = pluginConfig;
        }
        
        public override void InstallBindings()
        {
            Container.BindInstance(pluginConfig).AsSingle();
        }
    }
}