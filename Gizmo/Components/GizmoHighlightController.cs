using System.Collections.Generic;
using EditorEnhanced.Configuration;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoHighlightController : MonoBehaviour, IGizmoInput
{
   [Inject] private readonly PluginConfig _config = null!;
   private List<GizmoHighlight> _highlights;
   public bool IsDragging { get; set; }

   public void OnPointerEnter()
   {
      if (!IsDragging) Highlight();
   }

   public void OnPointerExit()
   {
      if (!IsDragging) Unhighlight();
   }

   public void OnDrag()
   {
   }

   public void OnMouseClick()
   {
      IsDragging = true;
      Highlight();
   }

   public void OnMouseRelease()
   {
      Unhighlight();
      IsDragging = false;
   }

   public void Highlight()
   {
      if (!_config.Gizmo.Highlight) return;
      foreach (var highlight in _highlights) highlight.AddOutline();
   }

   public void Unhighlight()
   {
      if (!_config.Gizmo.Highlight && !IsDragging) return;
      foreach (var highlight in _highlights) highlight.RemoveOutline();
   }

   public void Add(GameObject gizmo)
   {
      var gizmoHighlight = gizmo.GetComponent<GizmoHighlight>();
      if (gizmoHighlight == null) return;
      _highlights.Add(gizmoHighlight);
   }

   public void SharedWith(GizmoHighlightController gizmoHighlightController)
   {
      _highlights = gizmoHighlightController._highlights;
   }

   public void Init()
   {
      _highlights = [];
   }
}