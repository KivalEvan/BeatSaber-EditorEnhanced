using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.UI.Components;
using HMUI;
using Zenject;

namespace EditorEnhanced.UI;

public class DraggableEventBoxCell : IInitializable
{
   private readonly DiContainer _container;
   private readonly EditBeatmapViewController _ebvc;

   public DraggableEventBoxCell(DiContainer container, SignalBus signalBus, EditBeatmapViewController ebvc)
   {
      _container = container;
      _ebvc = ebvc;
   }

   public void Initialize()
   {
      var ebv = _ebvc
         ._editBeatmapRightPanelView._panels.First(p => p.panelType == BeatmapPanelType.EventBox)
         .elements[0]
         .GetComponent<EventBoxesView>();

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