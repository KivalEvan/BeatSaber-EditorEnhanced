using EditorEnhanced.Gizmo;
using EditorEnhanced.UI;
using Zenject;

namespace EditorEnhanced.Installers
{
    public class EEEditorUIInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<CameraPresetViewController>().AsSingle();
            Container.BindInterfacesTo<ReorderEventBoxViewController>().AsSingle();
        }
    }
}