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

      // Dependencies used by static Harmony patch callbacks
      Container.BindInterfacesAndSelfTo<ModifyHoveredLightEventDeltaIntensityCommandPatches>().AsSingle().NonLazy();
      Container.BindInterfacesAndSelfTo<PasteEventBoxGroupsCommandPatches>().AsSingle().NonLazy();
      Container.BindInterfacesAndSelfTo<EventBoxesViewPatches>().AsSingle().NonLazy();
      Container.Bind<DebugStatePatches>().AsSingle().NonLazy();
   }
}
