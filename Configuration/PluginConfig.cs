using System.Runtime.CompilerServices;
using EditorEnhanced.Gizmo.Configuration;
using EditorEnhanced.MotionPath.Configuration;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace EditorEnhanced.Configuration;

public class PluginConfig
{
   public virtual PrecisionConfig Precision { get; set; } = new();
   public virtual GizmoConfig Gizmo { get; set; } = new();
   /// <summary>Gets or sets the experimental selected event-box motion-path configuration.</summary>
   public virtual MotionPathConfig MotionPath { get; set; } = new();
   public virtual string EventBoxPresetsJson { get; set; } = "[]";
}
