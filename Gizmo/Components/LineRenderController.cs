using UnityEngine;

namespace EditorEnhanced.Gizmo.Components;

public class LineRenderController : MonoBehaviour
{
   private LineRenderer _lineRenderer;
   private Material _material;
   private Transform[] _transforms = [];

   public void Start()
   {
      _lineRenderer = GetComponent<LineRenderer>();
      _lineRenderer.material = _material;
      _lineRenderer.positionCount = _transforms.Length;
   }

   public void Update()
   {
      for (var i = 0; i < _transforms.Length; i++) _lineRenderer.SetPosition(i, _transforms[i].position);
   }

   public void SetMaterial(Material material)
   {
      _material = material;
      if (_lineRenderer != null) _lineRenderer.material = material;
   }

   public void SetTransforms(Transform[] transforms)
   {
      _transforms = transforms;
      if (_lineRenderer != null) _lineRenderer.positionCount = _transforms.Length;
   }
}