using BeatmapEditor3D;
using EditorEnhanced.Gizmo;
using EditorEnhanced.Managers;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorMainInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GizmoAssets>().AsSingle();
        Container.BindInterfacesTo<GizmoManager>().AsSingle();
    }
}