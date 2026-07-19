using System;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

public static class GizmoRotationMath
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

   public static float GetEventValue(Quaternion localRotation, LightAxis axis, bool mirror)
   {
      var axisComponent = axis switch
      {
         LightAxis.X => localRotation.x,
         LightAxis.Y => localRotation.y,
         LightAxis.Z => localRotation.z,
         _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
      };
      var value = 2f * Mathf.Atan2(axisComponent, localRotation.w) * Mathf.Rad2Deg;
      return mirror ? -value : value;
   }
}
