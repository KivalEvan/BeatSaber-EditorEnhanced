using EditorEnhanced.Managers;
using EditorEnhanced.UI.Tags;
using EditorEnhanced.UI.Views;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorUIInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<EditorButtonBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorButtonWithIconBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorLayoutHorizontalBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorLayoutStackBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorTextBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorToggleGroupBuilder>().AsSingle();

        Container.BindInterfacesTo<CameraPresetViewController>().AsSingle();
        Container.BindInterfacesAndSelfTo<CameraPresetManager>().AsSingle();

        Container.BindInterfacesTo<CopyEventBoxViewController>().AsSingle();
        Container.BindInterfacesAndSelfTo<EventBoxClipboardManager>().AsSingle();

        Container.BindInterfacesTo<LolighterViewController>().AsSingle();
        Container.BindInterfacesTo<ReorderEventBoxViewController>().AsSingle();
        // Container.BindInterfacesTo<MassValueShiftViewController>().AsSingle();

        Container.BindInterfacesTo<ScrollableYourInput>().AsSingle();
    }
}