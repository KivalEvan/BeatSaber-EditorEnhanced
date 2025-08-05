using EditorEnhanced.Patches;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorMainInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<EventBoxesViewPatches>().AsSingle();
        Container.BindInterfacesAndSelfTo<LightEventsPayloadPatches>().AsSingle();
        Container.BindInterfacesAndSelfTo<ModifyHoveredLightEventDeltaIntensityCommandPatches>().AsSingle();
        Container.BindInterfacesAndSelfTo<ModifyHoveredLightTranslationDeltaTranslationCommandPatches>().AsSingle();
        // Container.BindInterfacesAndSelfTo<ModifyHoveredLightRotationDeltaRotationCommandPatches>().AsSingle();
        Container.BindInterfacesAndSelfTo<PasteEventBoxGroupsCommandPatches>().AsSingle();
        // Container.BindInterfacesAndSelfTo<ObstacleLoaderSaverPatches>().AsSingle();
        Container.BindInterfacesAndSelfTo<FloatInputFieldValidatorPatches>().AsSingle();
        Container.BindInterfacesAndSelfTo<IntInputFieldValidatorPatches>().AsSingle();
        Container.BindInterfacesAndSelfTo<DebugStatePatches>().AsSingle();
    }
}