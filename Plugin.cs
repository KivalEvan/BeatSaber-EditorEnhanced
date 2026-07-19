using System.Reflection;
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
public class Plugin
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

   public static PluginConfig Config { get; private set; }
   public static IPALogger Log { get; private set; }
}
