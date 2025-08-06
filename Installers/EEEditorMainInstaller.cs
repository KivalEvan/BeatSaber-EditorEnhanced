using EditorEnhanced.Patches;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorMainInstaller : Installer
{
    public override void InstallBindings()
    {
        // Command Patch
        Container.BindInterfacesTo<MoveEventBoxPatches>().AsSingle();
        Container.BindInterfacesTo<LightEventsPayloadPatches>().AsSingle();
        Container.BindInterfacesTo<ModifyHoveredLightEventDeltaIntensityCommandPatches>().AsSingle();
        Container.BindInterfacesTo<ModifyHoveredLightTranslationDeltaTranslationCommandPatches>().AsSingle();
        // Container.BindInterfacesTo<ModifyHoveredLightRotationDeltaRotationCommandPatches>().AsSingle();
        Container.BindInterfacesTo<PasteEventBoxGroupsCommandPatches>().AsSingle();
        // Container.BindInterfacesTo<ObstacleLoaderSaverPatches>().AsSingle();
        
        // UI Patch
        Container.BindInterfacesTo<EventBoxesViewPatches>().AsSingle();
        Container.BindInterfacesTo<FloatInputFieldValidatorPatches>().AsSingle();
        Container.BindInterfacesTo<IntInputFieldValidatorPatches>().AsSingle();
        Container.BindInterfacesTo<DebugStatePatches>().AsSingle();
    }
}