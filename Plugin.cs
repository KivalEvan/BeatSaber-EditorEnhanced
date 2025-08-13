using BeatmapEditor3D;
using EditorEnhanced.Configuration;
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

      var pluginConfig = ipaConfig.Generated<PluginConfig>();

      zenjector.Install<AppInstaller>(Location.App, pluginConfig);

      // Runs in order, when editor launched
      zenjector.Install<EEMainInstaller, BeatmapEditorMainInstaller>();
      zenjector.Install<EEUIInstaller, BeatmapEditorViewControllersInstaller>();
      zenjector.Install<EECommandInstaller, CommandInstaller>();

      // Runs whenever enters beatmap level edit
      zenjector.Install<EELevelEditorInstaller, BeatmapLevelEditorInstaller>();

      Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
   }

   internal static IPALogger Log { get; private set; }
}