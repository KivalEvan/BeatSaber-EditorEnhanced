using EditorEnhanced.Managers;
using EditorEnhanced.UI;
using EditorEnhanced.UI.Tags;
using EditorEnhanced.UI.Views;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorUIInstaller : Installer
{
    public override void InstallBindings()
    {
        // UI Builder
        Container.BindInterfacesAndSelfTo<EditorButtonBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorButtonWithIconBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorCheckboxBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorLayoutHorizontalBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorLayoutVerticalBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorLayoutStackBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorTextBuilder>().AsSingle();
        Container.BindInterfacesAndSelfTo<EditorToggleGroupBuilder>().AsSingle();

        // Event Boxes
        Container.BindInterfacesTo<OffsetDurationDistributionViewController>().AsSingle();
        Container.BindInterfacesTo<SortEventBoxViewController>().AsSingle();
        Container.BindInterfacesTo<CopyEventBoxViewController>().AsSingle();
        Container.BindInterfacesAndSelfTo<EventBoxClipboardManager>().AsSingle();
        // Container.BindInterfacesTo<ReorderEventBoxViewController>().AsSingle();

        // Mixed
        Container.BindInterfacesTo<RandomSeedClipboardViewController>().AsSingle();
        Container.BindInterfacesAndSelfTo<RandomSeedClipboardManager>().AsSingle();

        // Others
        Container.BindInterfacesTo<ConfigurationViewController>().AsSingle();
        // Container.BindInterfacesTo<DifficultySwitchViewController>().AsSingle();
        // Container.BindInterfacesTo<LolighterViewController>().AsSingle();
        // Container.BindInterfacesTo<MassValueShiftViewController>().AsSingle();
        // Container.BindInterfacesTo<IntegratedScriptViewController>().AsSingle();

        // UI Patch
        Container.BindInterfacesTo<ScrollableYourInput>().AsSingle();
        Container.BindInterfacesTo<DraggableEventBoxCell>().AsSingle();
    }
}