using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Patches;
using EditorEnhanced.Patches;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEMainInstaller : Installer
{
   public override void InstallBindings()
   {
      Container.Bind<PrecisionDefaults>().AsSingle();
      Container.BindInterfacesTo<PrecisionConfigurationInitializer>().AsSingle();

      // Event data compatibility
      Container.BindInterfacesTo<LightColorDataViewPatches>().AsSingle();
      Container.BindInterfacesTo<FxEventBoxEditorDataPatches>().AsSingle();

      // Command behavior and precision
      Container.BindInterfacesTo<MoveEventBoxPatches>().AsSingle();
      Container.BindInterfacesTo<LightEventsPayloadPatches>().AsSingle();
      Container.BindInterfacesTo<ModifyHoveredLightEventDeltaIntensityCommandPatches>().AsSingle();
      Container.BindInterfacesTo<ModifyHoveredLightTranslationDeltaTranslationCommandPatches>().AsSingle();
      Container.BindInterfacesTo<PasteEventBoxGroupsCommandPatches>().AsSingle();

      // Editor UI integration
      Container.BindInterfacesTo<EventBoxesViewPatches>().AsSingle();
      Container.BindInterfacesTo<FloatInputFieldValidatorPatches>().AsSingle();
      Container.BindInterfacesTo<IntInputFieldValidatorPatches>().AsSingle();
      Container.BindInterfacesTo<DebugStatePatches>().AsSingle();
   }
}