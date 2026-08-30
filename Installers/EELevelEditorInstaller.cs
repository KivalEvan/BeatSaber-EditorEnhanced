using EditorEnhanced.Gizmo;
using EditorEnhanced.MotionPath;
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
      Container.Bind<MotionPathEventCommitter>().AsSingle();
      Container.Bind<MotionPathEventSourceResolver>().AsSingle();
      Container.BindInterfacesAndSelfTo<MotionPathEventGizmoController>().AsSingle();
      Container.Bind<MotionPathTransformPlanner>().AsSingle();
      Container.BindInterfacesAndSelfTo<MotionPathRenderer>().AsSingle();
      Container.BindInterfacesTo<MotionPathManager>().AsSingle();
   }
}
