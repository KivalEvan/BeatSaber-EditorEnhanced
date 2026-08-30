using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.Managers;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

public sealed class EventBoxGroupPresetView : IInitializable
{
   private readonly BeatmapFlowCoordinator _flowCoordinator;
   private readonly EventBoxGroupPresetManager _presetManager;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;
   private readonly EditorViewLocator _viewLocator;

   public EventBoxGroupPresetView(
      BeatmapFlowCoordinator flowCoordinator,
      EventBoxGroupPresetManager presetManager,
      SignalBus signalBus,
      UIBuilder uiBuilder,
      EditorViewLocator viewLocator)
   {
      _flowCoordinator = flowCoordinator;
      _presetManager = presetManager;
      _signalBus = signalBus;
      _uiBuilder = uiBuilder;
      _viewLocator = viewLocator;
   }

   public void Initialize()
   {
      if (!_viewLocator.TryGetEventBoxesView(out var eventBoxesView)) return;
      var controls = eventBoxesView._eventBoxButtonsScrollView == null
         ? null
         : eventBoxesView._eventBoxButtonsScrollView.transform.parent?.parent;
      if (!_viewLocator.TryFind(controls, "ControlButtons/RemoveButtonsWrapper", out var target)) return;

      var rect = (RectTransform)eventBoxesView._eventBoxButtonsScrollView.transform.parent;
      rect.sizeDelta = new Vector2(40f, -170f);
      rect.localPosition = new Vector3(-20f, -85f, 0f);

      var instance = Object.Instantiate(target.gameObject, target.parent);
      instance.name = "EventBoxGroupPresetButtonsWrapper";
      instance.transform.localPosition = new Vector3(40f, -120f, 0f);
      var hoverExpandView = instance.GetComponent<BeatmapEditorHoverExpandView>();
      for (var i = hoverExpandView._content.childCount - 1; i >= 0; i--)
         Object.Destroy(hoverExpandView._content.GetChild(i).gameObject);

      var button = _uiBuilder
         .CreateButton()
         .SetSize(new Vector2(40f, 40f))
         .SetPadding(new RectOffset(0, 0, 0, 0))
         .SetChildForceExpandWidth(true)
         .SetChildForceExpandHeight(true)
         .SetFontSize(12f);

      button.SetText("Copy\nPreset").SetOnClick(Copy).Create(hoverExpandView._content);
      button.SetText("Paste\nPreset").SetOnClick(Paste).Create(hoverExpandView._content);
      button.SetText("Save\nPreset").SetOnClick(ShowSaveDialog).Create(hoverExpandView._content);
      button.SetText("Load\nPreset").SetOnClick(ShowLoadDialog).Create(hoverExpandView._content);
   }

   private void Copy()
   {
      _presetManager.CopyCurrent();
   }

   private void Paste()
   {
      Apply(_presetManager.GetClipboard());
   }

   private void ShowSaveDialog()
   {
      var dialog = _flowCoordinator._createBookmarkController;
      dialog._labelInputField.text = string.Empty;
      dialog.Init(
         "Save Event Box Preset",
         "Save",
         "Cancel",
         (_, _, buttonIndex, name, _) =>
         {
            if (buttonIndex == 0 && !_presetManager.SaveCurrent(name)) return;
            CloseDialog();
            dialog._textInputField.gameObject.SetActive(true);
         });
      dialog._textInputField.gameObject.SetActive(false);
      _flowCoordinator.SetDialogScreenViewController(dialog, true);
   }

   private void ShowLoadDialog()
   {
      var names = _presetManager.GetPresetNames();
      if (names.Count == 0)
      {
         var dialog = _flowCoordinator._simpleMessageViewController;
         dialog.Init("Load Event Box Preset", "No presets are saved for this environment and group ID.", "OK", _ => CloseDialog());
         _flowCoordinator.SetDialogScreenViewController(dialog, true);
         return;
      }

      ShowLoadPage(names, 0);
   }

   private void ShowLoadPage(IReadOnlyList<string> names, int page)
   {
      const int pageSize = 2;
      var pageCount = (names.Count + pageSize - 1) / pageSize;
      var firstIndex = page * pageSize;
      var first = names[firstIndex];
      var second = firstIndex + 1 < names.Count ? names[firstIndex + 1] : null;
      var next = pageCount > 1 ? $"Next ({page + 1}/{pageCount})" : null;
      var dialog = _flowCoordinator._simpleMessageViewController;
      dialog.Init(
         "Load Event Box Preset",
         "Select a saved preset.",
         first,
         second,
         next,
         buttonIndex =>
         {
            CloseDialog();
            if (buttonIndex == 0)
               Apply(_presetManager.LoadPreset(first));
            else if (buttonIndex == 1 && second != null)
               Apply(_presetManager.LoadPreset(second));
            else if (buttonIndex == 2 && pageCount > 1)
               ShowLoadPage(names, (page + 1) % pageCount);
         });
      _flowCoordinator.SetDialogScreenViewController(dialog, true);
   }

   private void Apply(IReadOnlyList<EventBoxEditorData> eventBoxes)
   {
      if (eventBoxes != null)
         _signalBus.Fire(new ApplyEventBoxGroupPresetSignal(eventBoxes));
   }

   private void CloseDialog()
   {
      _flowCoordinator.SetDialogScreenViewController(null, false);
   }
}
