using EditorEnhanced.Menu;
using Zenject;

namespace EditorEnhanced.Installers;

internal class EEMenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<SettingsMenuManager>().AsSingle();
        Container.Bind<ExampleSettingsMenu>().AsSingle();
    }
}