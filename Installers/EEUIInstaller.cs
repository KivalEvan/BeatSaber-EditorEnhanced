using EditorEnhanced.Managers;
using EditorEnhanced.UI;
using EditorEnhanced.UI.Views;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEUIInstaller : Installer
{
   public override void InstallBindings()
   {
      // UI Builder
      Container.Bind<UIBuilder>().AsSingle();

      // Event Boxes
      Container.BindInterfacesTo<OffsetDurationDistributionView>().AsSingle();
      Container.BindInterfacesTo<SortEventBoxView>().AsSingle();
      Container.BindInterfacesTo<CopyEventBoxView>().AsSingle();
      Container.BindInterfacesTo<EventBoxIDVisualView>().AsSingle();
      Container.Bind<EventBoxClipboardManager>().AsSingle();

      // Mixed
      Container.BindInterfacesTo<RandomSeedClipboardView>().AsSingle();
      Container.BindInterfacesAndSelfTo<RandomSeedClipboardManager>().AsSingle();

      // Others
      Container.BindInterfacesTo<ConfigurationView>().AsSingle();

      // UI Patch
      Container.BindInterfacesTo<ScrollableYourInput>().AsSingle();
      Container.BindInterfacesTo<DraggableEventBoxCell>().AsSingle();
   }
}
