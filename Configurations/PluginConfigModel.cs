using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace EditorEnhanced.Configurations;

public class PluginConfigModel
{
    public bool GizmoEnabled { get; set; } = true;

    public virtual void Changed() { }
}
