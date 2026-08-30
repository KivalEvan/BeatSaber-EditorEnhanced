using System.Collections.Generic;
using System.Reflection;
using BeatmapEditor3D.InputSystem;
using BeatmapEditor3D.Views;
using HarmonyLib;
using UnityEngine;

namespace EditorEnhanced.MotionPath;

internal static class MotionPathEventGizmoInputGate
{
   private static MotionPathEventGizmoController _controller;
   private static bool _consumeEscapeRelease;
   private static int _directSelectionBlockThroughFrame = -1;
   private static bool _ownsLeftButton;

   internal static bool BlocksDirectEventBoxesSelection =>
      _ownsLeftButton || Time.frameCount <= _directSelectionBlockThroughFrame;

   internal static void Register(MotionPathEventGizmoController controller)
   {
      _controller = controller;
      _consumeEscapeRelease = false;
      _directSelectionBlockThroughFrame = -1;
      _ownsLeftButton = false;
   }

   internal static void Unregister(MotionPathEventGizmoController controller)
   {
      if (!ReferenceEquals(_controller, controller)) return;
      if (_ownsLeftButton) _directSelectionBlockThroughFrame = Time.frameCount;
      _controller = null;
      _consumeEscapeRelease = false;
      _ownsLeftButton = false;
   }

   internal static bool TryConsumeKeyDown(
      InputKey inputKey,
      bool wasOverUi,
      CurrentKeysBuffer currentKeys)
   {
      if (_controller == null) return false;
      if (inputKey == InputKey.escape && _ownsLeftButton)
      {
         _controller.CancelPointerGesture();
         _consumeEscapeRelease = true;
         currentKeys.SetKeyUp(InputKey.escape);
         return true;
      }
      if (inputKey != InputKey.leftButton) return false;
      if (!_ownsLeftButton && (wasOverUi || !_controller.TryBeginPointerGesture())) return false;

      _ownsLeftButton = true;
      currentKeys.SetKeyUp(InputKey.leftButton);
      return true;
   }

   internal static bool TryConsumeKeyUp(InputKey inputKey)
   {
      if (inputKey == InputKey.none)
      {
         if (_ownsLeftButton) _directSelectionBlockThroughFrame = Time.frameCount;
         _controller?.CancelPointerGesture();
         _consumeEscapeRelease = false;
         _ownsLeftButton = false;
         return false;
      }
      if (inputKey == InputKey.escape && _consumeEscapeRelease)
      {
         _consumeEscapeRelease = false;
         return true;
      }
      if (inputKey != InputKey.leftButton || !_ownsLeftButton) return false;

      _controller?.EndPointerGesture();
      _ownsLeftButton = false;
      _directSelectionBlockThroughFrame = Time.frameCount;
      return true;
   }
}

[HarmonyPatch(typeof(KeyBindInputActionsTransmitter), "HandleKeyDown")]
internal static class MotionPathEventGizmoKeyDownPatch
{
   private static readonly AccessTools.FieldRef<KeyBindInputActionsTransmitter, IInputReceiver> InputReceiver =
      AccessTools.FieldRefAccess<KeyBindInputActionsTransmitter, IInputReceiver>("_inputReceiver");

   [HarmonyPrefix]
   private static bool SuppressOwnedPointer(KeyBindInputActionsTransmitter __instance, InputKey __0)
   {
      var receiver = InputReceiver(__instance);
      return !MotionPathEventGizmoInputGate.TryConsumeKeyDown(
         __0,
         receiver.lastEventWasOverUI,
         receiver.currentKeysBuffer);
   }
}

[HarmonyPatch(typeof(KeyBindInputActionsTransmitter), "HandleKeyUp")]
internal static class MotionPathEventGizmoKeyUpPatch
{
   [HarmonyPrefix]
   private static bool SuppressOwnedPointer(InputKey __0) =>
      !MotionPathEventGizmoInputGate.TryConsumeKeyUp(__0);
}

[HarmonyPatch]
internal static class MotionPathEventGizmoSelectionPatch
{
   private static IEnumerable<MethodBase> TargetMethods()
   {
      yield return AccessTools.Method(
         typeof(EventBoxesSelectionView),
         "HandleBeatmapEditorGroundViewMouseStartDrag");
      yield return AccessTools.Method(
         typeof(EventBoxesSelectionView),
         "HandleBeatmapEditorGroundViewMouseDrag");
      yield return AccessTools.Method(
         typeof(EventBoxesSelectionView),
         "HandleBeatmapEditorGroundViewMouseEndDrag");
      yield return AccessTools.Method(
         typeof(EventBoxesSelectionView),
         "HandleBeatmapEditorGroundViewMouseMove");
   }

   [HarmonyPrefix]
   private static bool SuppressOwnedPointer() =>
      !MotionPathEventGizmoInputGate.BlocksDirectEventBoxesSelection;
}
