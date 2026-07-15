using BeatmapEditor3D.Views;
using EditorEnhanced.UI.Components;
using HMUI;
using Zenject;

namespace EditorEnhanced.UI;

internal sealed class DraggableEventBoxCell : IInitializable
{
   private readonly DiContainer _container;
   private readonly EditorViewLocator _viewLocator;

   public DraggableEventBoxCell(DiContainer container, EditorViewLocator viewLocator)
   {
      _container = container;
      _viewLocator = viewLocator;
   }

   public void Initialize()
   {
      if (!_viewLocator.TryGetEventBoxesView(out var ebv)) return;

      SegmentedControlCell[] prefabs =
      [
         ebv._eventBoxButtonsTextSegmentedControl._firstCellPrefab,
         ebv._eventBoxButtonsTextSegmentedControl._middleCellPrefab,
         ebv._eventBoxButtonsTextSegmentedControl._lastCellPrefab,
         ebv._eventBoxButtonsTextSegmentedControl._singleCellPrefab
      ];

      foreach (var prefab in prefabs) _container.InstantiateComponent<DragSwapSegmentCell>(prefab.gameObject);
   }
}
