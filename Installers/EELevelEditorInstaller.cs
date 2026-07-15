using EditorEnhanced.Gizmo;
using Zenject;

namespace EditorEnhanced.Installers;

public class EELevelEditorInstaller : Installer
{
   public override void InstallBindings()
   {
      Container.BindInterfacesAndSelfTo<GizmoAssets>().AsSingle();
      Container.Bind<GizmoEffectContextResolver>().AsSingle();
      Container.Bind<GizmoTransformPlanner>().AsSingle();
      Container.BindInterfacesAndSelfTo<GizmoRenderer>().AsSingle();
      Container.BindInterfacesTo<GizmoManager>().AsSingle();
   }
}
