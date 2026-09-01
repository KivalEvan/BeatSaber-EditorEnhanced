using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

public sealed class MergeEventBoxGroupsView : IInitializable, IDisposable
{
   private readonly BeatmapEventBoxGroupsDataModel _dataModel;
   private readonly EventBoxGroupsSelectionState _selectionState;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;
   private readonly EditorViewLocator _viewLocator;
   private Button _button;
   private readonly List<BeatmapEditorObjectId> _selectionOrder = [];

   public MergeEventBoxGroupsView(
      EventBoxGroupsSelectionState selectionState,
      BeatmapEventBoxGroupsDataModel dataModel,
      SignalBus signalBus,
      UIBuilder uiBuilder,
      EditorViewLocator viewLocator)
   {
      _selectionState = selectionState;
      _dataModel = dataModel;
      _signalBus = signalBus;
      _uiBuilder = uiBuilder;
      _viewLocator = viewLocator;
   }

   public void Initialize()
   {
      if (!_viewLocator.TryGetActiveSelectionView(out var selectionView)) return;

      var sourceRect = (RectTransform)selectionView._mirrorEventBoxGroupsButton.transform;
      var buttonObject = _uiBuilder
         .CreateButton()
         .SetSize(sourceRect.rect.size)
         .SetPadding(new RectOffset(4, 4, 2, 2))
         .SetFontSize(10f)
         .SetText("Merge")
         .SetOnClick(Merge)
         .Create(sourceRect.parent);
      buttonObject.name = "MergeEventBoxGroupsButton";
      _button = buttonObject.GetComponent<Button>();
      _button.transform.SetSiblingIndex(selectionView._deleteEventBoxGroupsButton.transform.GetSiblingIndex());

      _signalBus.Subscribe<EventBoxGroupsSelectionStateUpdatedSignal>(Refresh);
      _signalBus.Subscribe<BeatmapLevelUpdatedSignal>(Refresh);
      Refresh();
   }

   public void Dispose()
   {
      _signalBus.TryUnsubscribe<EventBoxGroupsSelectionStateUpdatedSignal>(Refresh);
      _signalBus.TryUnsubscribe<BeatmapLevelUpdatedSignal>(Refresh);
      if (_button != null) Object.Destroy(_button.gameObject);
   }

   private void Merge()
   {
      if (_selectionOrder.Count >= 2)
         _signalBus.Fire(new MergeSelectedEventBoxGroupsSignal(_selectionOrder[0]));
   }

   private void Refresh()
   {
      if (_button == null) return;

      var selected = _selectionState.eventBoxGroups;
      _selectionOrder.RemoveAll(id => !selected.Contains(id));
      _selectionOrder.AddRange(selected.Where(id => !_selectionOrder.Contains(id)));
      if (selected.Count < 2)
      {
         _button.interactable = false;
         return;
      }

      var first = _dataModel.GetEventBoxGroupById(selected[0]);
      _button.interactable = first != null;
      for (var i = 1; i < selected.Count && _button.interactable; i++)
      {
         var group = _dataModel.GetEventBoxGroupById(selected[i]);
         _button.interactable = group != null && group.groupId == first.groupId && group.type == first.type;
      }
   }
}
