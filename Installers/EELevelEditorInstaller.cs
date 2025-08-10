using EditorEnhanced.Gizmo;
using Zenject;

namespace EditorEnhanced.Installers;

public class EELevelEditorInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GizmoAssets>().AsSingle();
        Container.BindInterfacesTo<GizmoManager>().AsSingle();
    }
}