using System;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class DifficultySwitchViewController : IInitializable, IDisposable
{
   [Inject] private readonly BeatmapFlowCoordinator _bfc;
   [Inject] private readonly BeatmapLevelFlowCoordinator _blfc;
   [Inject] private readonly BeatmapProjectManager _bpm;
   private readonly EditBeatmapLevelNavigationViewController _eblnvc;
   private readonly EditBeatmapLevelViewController _eblvc;
   private readonly EditBeatmapViewController _ebvc;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;

   public DifficultySwitchViewController(
      SignalBus signalBus,
      EditBeatmapViewController ebvc,
      EditBeatmapLevelViewController eblvc,
      EditBeatmapLevelNavigationViewController eblnvc,
      UIBuilder uiBuilder)
   {
      _signalBus = signalBus;
      _ebvc = ebvc;
      _eblvc = eblvc;
      _eblnvc = eblnvc;
      _uiBuilder = uiBuilder;
   }

   public void Dispose()
   {
   }

   public void Initialize()
   {
      var target = _ebvc.transform;

      var stackTag = _uiBuilder
         .LayoutStack.Instantiate()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
      var verticalTag = _uiBuilder
         .LayoutVertical.Instantiate()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetPadding(new RectOffset(4, 4, 4, 4));
      var horizontalTag = _uiBuilder
         .LayoutHorizontal.Instantiate()
         .SetChildAlignment(TextAnchor.LowerCenter)
         .SetChildControlWidth(true)
         .SetSpacing(8)
         .SetPadding(new RectOffset(4, 4, 2, 4));
      var btnTag = _uiBuilder
         .Button.Instantiate()
         .SetFontSize(16);
      var checkboxTag = _uiBuilder
         .Checkbox.Instantiate()
         .SetSize(28)
         .SetFontSize(16);

      var container = verticalTag.Create(target.transform);
      var layout = horizontalTag.Create(container.transform);

      btnTag
         .SetText("Switch Difficulty")
         .SetOnClick(() =>
         {
            if (_bfc.anyDataModelDirty)
            {
               _bfc._simpleMessageViewController.Init(
                  "Switching Difficulty",
                  "You have unsaved changes, do you want to save them?",
                  "Save and switch",
                  "No and switch",
                  "No and stay",
                  buttonIdx =>
                  {
                     _bfc.SetDialogScreenViewController(null);
                     if (buttonIdx == 2) return;
                     _blfc.HandleBeatmapFlowCoordinatorDidFinish(
                        buttonIdx == 0 ? SaveType.Save : SaveType.DontSave,
                        _bfc._beatmapCharacteristic,
                        _bfc._beatmapDifficulty);
                     _blfc.HandleEditBeatmapViewControllerOpenBeatmapLevel(
                        _bfc._beatmapCharacteristic,
                        BeatmapDifficulty.Easy);
                  });
               _bfc.SetDialogScreenViewController(_bfc._simpleMessageViewController, true);
            }
            else
            {
               _blfc.HandleBeatmapFlowCoordinatorDidFinish(
                  SaveType.DontSave,
                  _bfc._beatmapCharacteristic,
                  _bfc._beatmapDifficulty);
               _blfc.HandleEditBeatmapViewControllerOpenBeatmapLevel(
                  _bfc._beatmapCharacteristic,
                  BeatmapDifficulty.Easy);
            }
         })
         .Create(layout.transform);
   }
}