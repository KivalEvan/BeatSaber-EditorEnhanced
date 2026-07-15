using System;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.Types;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using Zenject;

namespace EditorEnhanced.Gizmo;

internal sealed class GizmoManager : IInitializable, IDisposable
{
   private readonly BeatmapState _beatmapState;
   private readonly PluginConfig _config;
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly GizmoRenderer _renderer;
   private readonly SignalBus _signalBus;
   private readonly GizmoTransformPlanner _transformPlanner;

   public GizmoManager(
      SignalBus signalBus,
      PluginConfig config,
      BeatmapState beatmapState,
      EventBoxGroupsState eventBoxGroupsState,
      GizmoTransformPlanner transformPlanner,
      GizmoRenderer renderer)
   {
      _signalBus = signalBus;
      _config = config;
      _beatmapState = beatmapState;
      _eventBoxGroupsState = eventBoxGroupsState;
      _transformPlanner = transformPlanner;
      _renderer = renderer;
   }

   public void Initialize()
   {
      _signalBus.Subscribe<BeatmapEditingModeSwitchedSignal>(HandleEditingModeChanged);
      _signalBus.Subscribe<EventBoxesUpdatedSignal>(Refresh);
      _signalBus.Subscribe<EventBoxModifiedSignal>(Refresh);
      _signalBus.Subscribe<GizmoRefreshSignal>(Refresh);
   }

   public void Dispose()
   {
      _signalBus.TryUnsubscribe<BeatmapEditingModeSwitchedSignal>(HandleEditingModeChanged);
      _signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(Refresh);
      _signalBus.TryUnsubscribe<EventBoxModifiedSignal>(Refresh);
      _signalBus.TryUnsubscribe<GizmoRefreshSignal>(Refresh);
      _renderer.Clear();
   }

   private void HandleEditingModeChanged(BeatmapEditingModeSwitchedSignal signal)
   {
      if (_config.Gizmo.Enabled && signal.mode == BeatmapEditingMode.EventBoxes)
         RenderCurrentGroup();
      else
         _renderer.Clear();
   }

   private void Refresh()
   {
      if (_config.Gizmo.Enabled && _beatmapState.editingMode == BeatmapEditingMode.EventBoxes)
         RenderCurrentGroup();
      else
         _renderer.Clear();
   }

   private void RenderCurrentGroup()
   {
      var group = _eventBoxGroupsState.eventBoxGroupContext;
      if (group != null && _transformPlanner.TryCreatePlan(group, out var plan))
         _renderer.Render(plan);
      else
         _renderer.Clear();
   }
}
