using System.Reflection;
using BeatmapEditor3D;
using EditorEnhanced.Configuration;
using EditorEnhanced.Installers;
using HarmonyLib;
using Zenject;

namespace EditorEnhanced.Patches;

internal static class InstallerPatches
{
   private static readonly PropertyInfo ContainerProperty =
      AccessTools.Property(typeof(MonoInstallerBase), "Container");

   private static DiContainer GetContainer(MonoInstallerBase installer)
   {
      return (DiContainer)ContainerProperty.GetValue(installer);
   }

   private static void Install<TInstaller>(DiContainer container) where TInstaller : Installer
   {
      if (!container.HasBinding<PluginConfig>()) container.BindInstance(Plugin.Config).AsSingle();
      container.Install<TInstaller>();
   }

   [HarmonyPatch(typeof(BeatmapEditorMainInstaller), nameof(BeatmapEditorMainInstaller.InstallBindings))]
   private static class BeatmapEditorMainInstallerPatch
   {
      [HarmonyPostfix]
      private static void InstallBindings(BeatmapEditorMainInstaller __instance)
      {
         Install<EEMainInstaller>(GetContainer(__instance));
      }
   }

   [HarmonyPatch(typeof(BeatmapEditorViewControllersInstaller),
      nameof(BeatmapEditorViewControllersInstaller.InstallBindings))]
   private static class BeatmapEditorViewControllersInstallerPatch
   {
      [HarmonyPostfix]
      private static void InstallBindings(BeatmapEditorViewControllersInstaller __instance)
      {
         Install<EEUIInstaller>(GetContainer(__instance));
      }
   }

   [HarmonyPatch(typeof(CommandInstaller), nameof(CommandInstaller.InstallBindings))]
   private static class CommandInstallerPatch
   {
      [HarmonyPostfix]
      private static void InstallBindings(CommandInstaller __instance)
      {
         Install<EECommandInstaller>(GetContainer(__instance));
      }
   }

   [HarmonyPatch(typeof(BeatmapLevelEditorInstaller), nameof(BeatmapLevelEditorInstaller.InstallBindings))]
   private static class BeatmapLevelEditorInstallerPatch
   {
      [HarmonyPostfix]
      private static void InstallBindings(BeatmapLevelEditorInstaller __instance)
      {
         Install<EELevelEditorInstaller>(GetContainer(__instance));
      }
   }
}
