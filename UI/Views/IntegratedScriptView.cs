using BeatmapEditor3D;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class IntegratedScriptView : IInitializable
{
   private readonly EditBeatmapViewController _ebvc;
   private readonly UIBuilder _uiBuilder;

   private GameObject _view;

   public IntegratedScriptView(
      EditBeatmapViewController ebvc,
      UIBuilder uiBuilder)
   {
      _ebvc = ebvc;
      _uiBuilder = uiBuilder;
   }

   public void Initialize()
   {
      var buttonTag = _uiBuilder.CreateButton();
      var stackTag = _uiBuilder.CreateHorizontalLayout();
      var textTag = _uiBuilder.CreateText();

      _view = stackTag.Create(_ebvc.transform);
      textTag.SetText("Integrated Script").Create(_view.transform);

      buttonTag
         .SetText("Integrated Script")
         .SetOnClick(ToggleView)
         .Create(_ebvc._beatmapEditorExtendedSettingsView.transform);
   }

   private void ToggleView()
   {
      _view.SetActive(!_view.activeSelf);
   }
}
