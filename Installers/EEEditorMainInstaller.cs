using EditorEnhanced.Gizmo;
using EditorEnhanced.Managers;
using EditorEnhanced.Patches;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorMainInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GizmoAssets>().AsSingle();
        Container.BindInterfacesTo<GizmoManager>().AsSingle();
        
        Container.BindInterfacesAndSelfTo<LightEventsPayloadPatches>().AsSingle();
    }
}