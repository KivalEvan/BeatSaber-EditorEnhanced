using System;
using BeatmapEditor3D.Views;
using EditorEnhanced.Gizmo.Commands;
using HarmonyLib;
using Zenject;

namespace EditorEnhanced.Gizmo.Patches;

[HarmonyPatch(typeof(EventBoxesView), nameof(EventBoxesView.SetEventBoxData))]
public class EventBoxesViewPatches : IDisposable
{
   private static SignalBus _signalBus;
   private readonly SignalBus _injectedSignalBus;

   public EventBoxesViewPatches(SignalBus signalBus)
   {
      _injectedSignalBus = signalBus;
      _signalBus = signalBus;
   }

   public void Dispose()
   {
      if (ReferenceEquals(_signalBus, _injectedSignalBus)) _signalBus = null;
   }

   [HarmonyPostfix]
   private static void SignalSelectedEventBox()
   {
      _signalBus?.Fire<EventBoxSelectedSignal>();
   }
}
