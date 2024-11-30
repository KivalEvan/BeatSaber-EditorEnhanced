using EditorEnhanced.Gizmo;
using EditorEnhanced.UI;
using Zenject;

namespace EditorEnhanced.Installers
{
    public class EEEditorMainInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<GizmoAssets>().AsSingle();
            Container.BindInterfacesTo<GizmoManager>().AsSingle();
        }
    }
}