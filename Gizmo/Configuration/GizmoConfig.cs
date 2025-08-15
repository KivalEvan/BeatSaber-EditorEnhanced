using UnityEngine;

namespace EditorEnhanced.Gizmo.Configuration;

public class GizmoConfig
{
   public virtual bool Enabled { get; set; } = true;

   // Functionality
   public virtual bool Draggable { get; set; } = true;
   public virtual bool Swappable { get; set; } = true;
   public virtual bool RaycastGizmo { get; set; } = true;
   public virtual bool RaycastLane { get; set; } = true;

   // Visual
   public virtual bool Highlight { get; set; } = true;
   public virtual bool MulticolorId { get; set; } = true;
   public virtual bool DistributeShape { get; set; } = true;

   public virtual bool ShowBase { get; set; } = true;
   public virtual bool ShowModifier { get; set; } = true;
   public virtual bool ShowLane { get; set; } = true;
   public virtual bool ShowInfo { get; set; } = true;

   public virtual float GlobalScale { get; set; } = 1f;
   public virtual float SizeBase { get; set; } = 0.5f;
   public virtual float SizeTranslation { get; set; } = 2.5f;
   public virtual float SizeRotation { get; set; } = 2.5f;

   public virtual Color DefaultColor { get; set; } = new(1f, 1f, 1f);
   public virtual Color HighlightColor { get; set; } = new(0f, 1f, 1f);
}