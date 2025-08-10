using System.Runtime.CompilerServices;
using EditorEnhanced.Gizmo.Configuration;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace EditorEnhanced.Configuration;

public class PluginConfig
{
    public virtual bool Enabled { get; set; } = true;

    public virtual PrecisionConfig Precision { get; set; } = new();
    public virtual GizmoConfig Gizmo { get; set; } = new();

    public virtual void Changed()
    {
    }
}