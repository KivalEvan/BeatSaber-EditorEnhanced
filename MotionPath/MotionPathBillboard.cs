using UnityEngine;

namespace EditorEnhanced.MotionPath;

internal sealed class MotionPathBillboard : MonoBehaviour
{
   private Camera _camera;

   private void LateUpdate()
   {
      if (_camera == null || !_camera.isActiveAndEnabled) _camera = Camera.main;
      if (_camera == null) return;

      var direction = transform.position - _camera.transform.position;
      if (direction.sqrMagnitude > 0f)
         transform.rotation = Quaternion.LookRotation(direction, _camera.transform.up);
   }
}
