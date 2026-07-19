using System.Reflection;
using BeatmapEditor3D;
using EditorEnhanced.Configuration;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace EditorEnhanced;

[Plugin(RuntimeOptions.SingleStartInit)]
[NoEnableDisable]
internal class Plugin
{
   [Init]
   public Plugin(IPALogger ipaLogger, IPAConfig ipaConfig, PluginMetadata pluginMetadata)
   {
      Log = ipaLogger;
      Config = ipaConfig.Generated<PluginConfig>();

      var harmony = new Harmony(pluginMetadata.Id);
      harmony.PatchAll(Assembly.GetExecutingAssembly());

      Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
   }

   internal static PluginConfig Config { get; private set; }
   internal static IPALogger Log { get; private set; }
}
