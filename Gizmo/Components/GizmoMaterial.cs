using UnityEngine;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoMaterial : MonoBehaviour
{
   private static readonly int ColorShaderId = Shader.PropertyToID("_Color");
   private readonly MaterialPropertyBlock _materialPropertyBlock = new();
   private bool _init;
   private Renderer _renderer;

   private void Awake()
   {
      _renderer = GetComponent<Renderer>();
      _renderer.SetPropertyBlock(_materialPropertyBlock);
      _init = true;
   }

   public void SetColor(Color color)
   {
      _materialPropertyBlock.SetColor(ColorShaderId, color);
      if (_init) _renderer.SetPropertyBlock(_materialPropertyBlock);
   }
}
