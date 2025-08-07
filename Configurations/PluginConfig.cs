namespace EditorEnhanced.Configurations;

public class PluginConfig
{
    public bool GizmoEnabled { get; set; }

    public PluginConfig(PluginConfigModel pluginConfigModel)
    {
        GizmoEnabled = pluginConfigModel.GizmoEnabled;
    }
}
