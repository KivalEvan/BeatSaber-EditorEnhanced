using System;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

internal static class GizmoRotationMath
{
   public static Vector3 GetAxisNormal(Transform reference, LightAxis axis)
   {
      return axis switch
      {
         LightAxis.X => reference.right,
         LightAxis.Y => reference.up,
         LightAxis.Z => reference.forward,
         _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
      };
   }

   public static Vector3 GetSnappedLocalEuler(
      Vector3 localEulerAngles,
      LightAxis axis,
      Func<float, float> snap)
   {
      return axis switch
      {
         LightAxis.X => Mathf.Approximately(localEulerAngles.z, 90f)
            ? new Vector3(snap(-localEulerAngles.x + 270f), 0f, 0f)
            : new Vector3(snap(localEulerAngles.x) + 90f, 0f, 0f),
         LightAxis.Y => new Vector3(0f, snap(localEulerAngles.y), 0f),
         LightAxis.Z => Mathf.Approximately(localEulerAngles.z, 90f)
            ? new Vector3(0f, 0f, snap(-localEulerAngles.x))
            : new Vector3(0f, 0f, snap(localEulerAngles.x + 180f)),
         _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
      };
   }

   public static float GetTargetValue(
      Vector3 localRotation,
      Vector3 targetLocalRotation,
      LightAxis axis,
      bool mirror)
   {
      var value = axis switch
      {
         LightAxis.X => localRotation.x + targetLocalRotation.x,
         LightAxis.Y => localRotation.y + targetLocalRotation.y,
         LightAxis.Z => localRotation.z + targetLocalRotation.z,
         _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
      };
      return mirror ? -value : value;
   }
}