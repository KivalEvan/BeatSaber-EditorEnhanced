using System.Reflection;
using BeatmapEditor3D;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using SiraUtil.Zenject;
using EditorEnhanced.Installers;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace EditorEnhanced;

[Plugin(RuntimeOptions.SingleStartInit), NoEnableDisable]
internal class Plugin
{
    internal static IPALogger Log { get; private set; }

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
        zenjector.Install<EECommandInstaller, CommandInstaller>();
            
        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");

        var asm = Assembly.GetExecutingAssembly();
        foreach (var manifestResourceName in asm.GetManifestResourceNames())
        {
            Log.Info($"{manifestResourceName}");
        }
    }
}