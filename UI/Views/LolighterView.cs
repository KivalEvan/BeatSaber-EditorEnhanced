using System;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class LolighterView : IInitializable, IDisposable
{
   private readonly EditBeatmapNavigationViewController _ebnvc;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;

   public LolighterView(
      SignalBus signalBus,
      EditBeatmapNavigationViewController ebnvc,
      UIBuilder uiBuilder)
   {
      _signalBus = signalBus;
      _ebnvc = ebnvc;
      _uiBuilder = uiBuilder;
   }

   public void Dispose()
   {
   }

   public void Initialize()
   {
      var target = _ebnvc._eventsToolbarView;

      _uiBuilder
         .Button.Instantiate()
         .SetFontSize(10)
         .SetText("Commit Crime")
         .SetOnClick(() => _signalBus.Fire(new LolighterSignal()))
         .Create(target.transform);
   }
}