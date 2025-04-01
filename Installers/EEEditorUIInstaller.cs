using EditorEnhanced.Managers;
using EditorEnhanced.UI.Views;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorUIInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<CameraPresetViewController>().AsSingle();
        Container.BindInterfacesAndSelfTo<CameraPresetManager>().AsSingle();

        Container.BindInterfacesTo<CopyEventBoxViewController>().AsSingle();
        Container.BindInterfacesAndSelfTo<EventBoxClipboardManager>().AsSingle();
        
        Container.BindInterfacesTo<LolighterViewController>().AsSingle();
        Container.BindInterfacesTo<ReorderEventBoxViewController>().AsSingle();
    }
}