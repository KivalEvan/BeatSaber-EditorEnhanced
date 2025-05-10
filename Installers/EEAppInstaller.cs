using Zenject;

namespace EditorEnhanced.Installers;

internal class EEAppInstaller : Installer
{
    private readonly EEConfig _eeConfig;

    public EEAppInstaller(EEConfig eeConfig)
    {
        _eeConfig = eeConfig;
    }

    public override void InstallBindings()
    {
        Container.BindInstance(_eeConfig).AsSingle();
    }
}