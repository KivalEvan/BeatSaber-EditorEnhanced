using BeatmapEditor3D;
using EditorEnhanced.Configurations;
using EditorEnhanced.Installers;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace EditorEnhanced;

[Plugin(RuntimeOptions.SingleStartInit)]
[NoEnableDisable]
internal class Plugin
{
    [Init]
    public Plugin(IPALogger ipaLogger, IPAConfig ipaConfig, Zenjector zenjector, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        zenjector.UseLogger(Log);

        var pluginConfig = ipaConfig.Generated<EEConfig>();

        zenjector.Install<EEAppInstaller>(Location.App, pluginConfig);
        // zenjector.Install<EEMenuInstaller>(Location.Menu);

        zenjector.Install<EEEditorMainInstaller, BeatmapLevelEditorInstaller>();
        zenjector.Install<EEEditorUIInstaller, BeatmapEditorViewControllersInstaller>();
        zenjector.Install<EEEditorCommandInstaller, CommandInstaller>();

        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }

    internal static IPALogger Log { get; private set; }
}