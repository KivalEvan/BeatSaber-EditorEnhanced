using EditorEnhanced.Gizmo.Patches;
using EditorEnhanced.Patches;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEMainInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<PrecisionConfigPatches>().AsSingle();
        
        // Fixes
        Container.BindInterfacesTo<EditObjectViewPatches>().AsSingle();
        Container.BindInterfacesTo<ModifyFullLightColorPatches>().AsSingle();

        // Command
        Container.BindInterfacesTo<MoveEventBoxPatches>().AsSingle();
        Container.BindInterfacesTo<LightEventsPayloadPatches>().AsSingle();
        Container.BindInterfacesTo<ModifyHoveredLightEventDeltaIntensityCommandPatches>().AsSingle();
        Container.BindInterfacesTo<ModifyHoveredLightTranslationDeltaTranslationCommandPatches>().AsSingle();
        Container.BindInterfacesTo<PasteEventBoxGroupsCommandPatches>().AsSingle();

        // UI
        Container.BindInterfacesTo<EventBoxesViewPatches>().AsSingle();
        Container.BindInterfacesTo<FloatInputFieldValidatorPatches>().AsSingle();
        Container.BindInterfacesTo<IntInputFieldValidatorPatches>().AsSingle();
        Container.BindInterfacesTo<DebugStatePatches>().AsSingle();
    }
}