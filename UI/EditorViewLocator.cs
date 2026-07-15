using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI;

internal sealed class EditorViewLocator
{
   private readonly EditBeatmapNavigationViewController _navigationViewController;
   private readonly HashSet<string> _reportedFailures = new();
   private readonly EditBeatmapViewController _viewController;

   public EditorViewLocator(
      EditBeatmapViewController viewController,
      EditBeatmapNavigationViewController navigationViewController)
   {
      _viewController = viewController;
      _navigationViewController = navigationViewController;
   }

   public bool TryGetRightPanel(out EditBeatmapRightPanelView rightPanel)
   {
      rightPanel = _viewController._editBeatmapRightPanelView;
      return rightPanel != null || ReportMissing("right-panel", "the editor right panel");
   }

   public bool TryGetEditObjectView(out EditObjectView editObjectView)
   {
      editObjectView = null;
      if (!TryGetRightPanel(out var rightPanel)) return false;

      editObjectView = rightPanel._editObjectView;
      return editObjectView != null || ReportMissing("edit-object-view", "the edit object view");
   }

   public bool TryGetRightPanelContent(out Transform content)
   {
      content = null;
      if (!TryGetRightPanel(out var rightPanel)) return false;
      if (rightPanel._scrollView == null)
         return ReportMissing("right-panel-scroll", "the editor right panel scroll view");

      content = rightPanel._scrollView.contentTransform;
      return content != null || ReportMissing("right-panel-content", "the editor right panel content");
   }

   public bool TryRegisterPanel(string name, GameObject content)
   {
      if (!TryGetRightPanel(out var rightPanel) || rightPanel._dropdown == null)
         return ReportMissing("right-panel-dropdown", "the editor right panel dropdown");

      var panels = rightPanel._panels ?? [];
      if (panels.Any(panel => panel.name == name)) return false;

      var panel = new EditBeatmapRightPanelView.PanelElement
      {
         name = name,
         panelType = (BeatmapPanelType)(Enum.GetValues(typeof(BeatmapPanelType)).Length + 1),
         elements = [content]
      };
      rightPanel._panels = panels.Append(panel).ToArray();
      rightPanel._dropdown.SetTexts(rightPanel._panels.Select(item => item.name).ToArray());
      rightPanel._dropdown._numberOfVisibleCell = rightPanel._panels.Length;
      return true;
   }

   public bool TryGetEventBoxesView(out EventBoxesView eventBoxesView)
   {
      eventBoxesView = null;
      if (!TryGetRightPanel(out var rightPanel)) return false;

      if (rightPanel._panels != null)
         foreach (var panel in rightPanel._panels)
            if (panel.panelType == BeatmapPanelType.EventBox && panel.elements != null)
               foreach (var element in panel.elements)
               {
                  if (element == null) continue;
                  eventBoxesView = element.GetComponent<EventBoxesView>();
                  if (eventBoxesView != null) return true;
               }

      return ReportMissing("event-boxes-view", "the event boxes panel or its EventBoxesView component");
   }

   public bool TryGetEventBoxView(out EventBoxView eventBoxView)
   {
      eventBoxView = null;
      if (!TryGetEventBoxesView(out var eventBoxesView)) return false;

      eventBoxView = eventBoxesView._eventBoxView;
      return eventBoxView != null || ReportMissing("event-box-view", "the event box editor view");
   }

   public bool TryGetEventBoxToolbarInsertion(out Transform parent, out int siblingIndex)
   {
      parent = null;
      siblingIndex = 0;
      var toolbar = _navigationViewController._eventBoxGroupsToolbarView;
      if (toolbar == null || toolbar._extensionToggle == null || toolbar._extensionToggle.transform.parent == null)
         return ReportMissing("event-box-toolbar", "the event box groups toolbar insertion point");

      parent = toolbar.transform;
      siblingIndex = toolbar._extensionToggle.transform.parent.GetSiblingIndex();
      return true;
   }

   public bool TryGetNoteBackground(out Transform background)
   {
      background = null;
      if (!TryGetEditObjectView(out var editObjectView)) return false;
      if (editObjectView._noteDataView == null) return ReportMissing("note-data-view", "the note editor data view");

      background = editObjectView._noteDataView.transform.Find("Background4px");
      return background != null || ReportMissing("note-background", "the note editor background template");
   }

   public bool TryFind(Transform root, string path, out Transform target)
   {
      target = root == null ? null : root.Find(path);
      return target != null || ReportMissing($"path:{path}", $"the editor transform '{path}'");
   }

   public bool TryFindRect(Transform root, string path, out RectTransform target)
   {
      target = null;
      if (!TryFind(root, path, out var transform)) return false;

      target = transform as RectTransform;
      return target != null || ReportMissing($"rect:{path}", $"the editor RectTransform '{path}'");
   }

   public Button GetButtonPrefab()
   {
      return Require(
         _viewController._beatmapEditorExtendedSettingsView?._copyDifficultyButton,
         "button-prefab",
         "the editor button template");
   }

   public Toggle GetTogglePrefab()
   {
      return Require(
         _navigationViewController._eventBoxGroupsToolbarView?._extensionToggle,
         "toggle-prefab",
         "the editor toggle template");
   }

   public GameObject GetInputPrefab()
   {
      var input = _viewController._editBeatmapRightPanelView?._editObjectView?._noteDataView?
         ._beatInputFieldValidator;
      return Require(input, "input-prefab", "the editor input template").gameObject;
   }

   public GameObject GetTextPrefab()
   {
      var text = _viewController._activeSelectionView?._arcsCountText;
      return Require(text, "text-prefab", "the editor text template").gameObject;
   }

   public GameObject GetSliderPrefab()
   {
      var statusBar = _viewController.GetComponentInChildren<StatusBarView>();
      return Require(statusBar?._musicVolumeSlider, "slider-prefab", "the editor slider template").gameObject;
   }

   public Transform GetEditorRoot()
   {
      return Require(_viewController, "editor-root", "the editor view controller").transform;
   }

   private T Require<T>(T value, string key, string description) where T : Object
   {
      if (value != null) return value;

      ReportMissing(key, description);
      throw new InvalidOperationException($"Editor UI compatibility error: could not locate {description}.");
   }

   private bool ReportMissing(string key, string description)
   {
      if (_reportedFailures.Add(key))
         Plugin.Log.Error($"Editor UI compatibility error: could not locate {description}.");
      return false;
   }
}