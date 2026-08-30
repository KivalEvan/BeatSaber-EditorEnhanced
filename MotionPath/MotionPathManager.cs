using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Controller;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.Types;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.MotionPath.Configuration;
using EditorEnhanced.UI;
using Zenject;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.MotionPath;

internal sealed class MotionPathManager : IInitializable, ILateTickable, IDisposable
{
   private const double BakeBudgetMilliseconds = 1.5d;
   private const int CachedPlanLimit = 4;
   private const int PreviewSamplesPerBeat = 8;
   private const int ServiceRetryFrames = 30;

   private readonly AudioDataModel _audioDataModel;
   private readonly BeatmapData _livePreviewBeatmapData;
   private readonly BeatmapState _beatmapState;
   private readonly PluginConfig _config;
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly MotionPathRenderer _renderer;
   private readonly MotionPathTransformPlanner _transformPlanner;
   private readonly EditorViewLocator _viewLocator;
   private readonly SignalBus _signalBus;
   private readonly ISongPreviewController _songPreviewController;

     private bool _clearPending;
     private bool _disposed;
      private bool _dirty = true;
      private MotionPathTransformPlanner.IPreparationOperation _preparation;
      private long _preparationRequestVersion;
      private MotionPathTransformPlanner.IBakeOperation _pendingOperation;
      private PendingOperationPurpose _pendingPurpose;
      private float _pendingRequestedBeat;
      private float _pendingBackwardRangeInBeats;
      private float _pendingForwardRangeInBeats;
      private long _pendingRequestVersion;
      private MotionPathPlan _plan;
      private bool _planIsFullQuality;
      private readonly LinkedList<MotionPathPlan> _planCache = [];
     private MotionPathPlanConfiguration _planConfiguration;
     private bool _presentationUpdatePending;
     private MotionPathTransformPlanner.IBakeSession _session;
     private long _sessionRequestVersion;
    private int _rebuildAfterFrame;
    private bool _rendererCleared = true;
    private long _requestVersion;
   private float _renderedBeat = float.NaN;

   public MotionPathManager(
      SignalBus signalBus,
      PluginConfig config,
      BeatmapState beatmapState,
      AudioDataModel audioDataModel,
      EventBoxGroupsState eventBoxGroupsState,
      BeatmapData livePreviewBeatmapData,
      ISongPreviewController songPreviewController,
      EditorViewLocator viewLocator,
      MotionPathTransformPlanner transformPlanner,
      MotionPathRenderer renderer)
   {
      _signalBus = signalBus;
      _config = config;
      _beatmapState = beatmapState;
      _audioDataModel = audioDataModel;
      _eventBoxGroupsState = eventBoxGroupsState;
      _livePreviewBeatmapData = livePreviewBeatmapData;
      _songPreviewController = songPreviewController;
      _viewLocator = viewLocator;
      _transformPlanner = transformPlanner;
      _renderer = renderer;
   }

    public void Initialize()
    {
       _planConfiguration = MotionPathPlanConfiguration.Capture(_config.MotionPath);
      _signalBus.Subscribe<BeatmapEditingModeSwitchedSignal>(HandleEditingModeChanged);
      _signalBus.Subscribe<EventBoxSelectedSignal>(HandleEventBoxSelected);
      _signalBus.Subscribe<EventBoxesUpdatedSignal>(HandleEventBoxesUpdated);
      _signalBus.Subscribe<EventBoxModifiedSignal>(HandleEventBoxModified);
      _signalBus.Subscribe<BeatmapLevelUpdatedSignal>(HandleBeatmapLevelUpdated);
      _signalBus.Subscribe<BeatmapLevelStateTimeUpdated>(HandleBeatmapLevelStateTimeUpdated);
      _signalBus.Subscribe<MotionPathRefreshSignal>(HandleRefresh);
      _songPreviewController.playheadPositionChangedEvent += HandlePlaybackPositionChanged;
      _livePreviewBeatmapData.beatmapEventDataWasInsertedEvent += HandleEventInserted;
      _livePreviewBeatmapData.beatmapEventDataWasRemovedEvent += HandleEventRemoved;
   }

      public void LateTick()
      {
         if (_disposed) return;

         try
        {
            if (_clearPending)
            {
               Clear();
               _clearPending = false;
            }

            if (!CanRender())
            {
               DropPreparedState();
               Clear();
               return;
           }

           if (_dirty && UnityEngine.Time.frameCount > _rebuildAfterFrame)
           {
              StartBake();
              return;
           }

            var currentBeat = GetCurrentBeat();
            if (_preparation != null)
            {
               if (_preparationRequestVersion != _requestVersion)
               {
                  DropPreparation();
                  return;
               }

               _preparation.Advance(BakeBudgetMilliseconds);
               if (_preparationRequestVersion != _requestVersion)
               {
                  DropPreparation();
                  return;
               }
               if (!_preparation.CompletedOutcome.HasValue) return;

               var preparationOutcome = _preparation.CompletedOutcome.Value;
               var session = _preparation.CompletedSession;
               DropPreparation();
               if (preparationOutcome == MotionPathBakePreparationOutcome.Ready && session != null)
               {
                  SetSession(session);
                  return;
               }

               Clear();
               if (preparationOutcome == MotionPathBakePreparationOutcome.ServicesUnavailable)
                  RetryAfterServicesInitialize();
               return;
            }

             if (_session == null || _sessionRequestVersion != _requestVersion)
            {
               return;
            }

             var activePlanContainsWindow = ContainsVisibleWindow(_plan, currentBeat);
             if (!activePlanContainsWindow)
             {
                if (TryRenderCachedPlan(currentBeat))
                {
                   DropPendingOperation();
                   ScheduleForwardPrefetch(currentBeat);
                   return;
                }

                if (_pendingOperation == null
                    || _pendingPurpose != PendingOperationPurpose.Preview
                    || !_pendingOperation.ContainsVisibleWindow(
                       currentBeat,
                       GetBackwardRange(),
                       GetForwardRange()))
                {
                   StartPreview(currentBeat, true);
                   return;
                }
             }

             if (_pendingOperation != null)
             {
                if (_pendingRequestVersion != _requestVersion)
               {
                  DropPendingOperation();
                  return;
               }

                if (activePlanContainsWindow
                    && _planIsFullQuality
                    && _pendingPurpose == PendingOperationPurpose.Prefetch)
                {
                   EnsureForwardPrefetchMatchesCurrentRange(currentBeat);
                  if (_pendingOperation == null)
                  {
                     if (_presentationUpdatePending || _renderedBeat != currentBeat) UpdateForBeat(currentBeat);
                     return;
                  }
               }
                _pendingOperation.Advance(BakeBudgetMilliseconds);
                if (_pendingRequestVersion != _requestVersion
                    || _sessionRequestVersion != _requestVersion)
                {
                   DropPendingOperation();
                   return;
                }
                if (_pendingOperation.CompletedPlan != null)
                {
                   CompletePendingOperation(currentBeat);
                }

               if (_plan == null) return;
               if (!ContainsVisibleWindow(_plan, currentBeat))
               {
                  Clear();
                  _clearPending = false;
                  return;
               }

               if (_presentationUpdatePending || _renderedBeat != currentBeat) UpdateForBeat(currentBeat);
               return;
            }

             if (!activePlanContainsWindow)
             {
                StartPreview(currentBeat, true);
                return;
             }

            if (_presentationUpdatePending || _renderedBeat != currentBeat) UpdateForBeat(currentBeat);
             ScheduleForwardPrefetch(currentBeat);
        }
        catch (Exception exception)
        {
           Plugin.Log.Error($"Motion-path bake failed: {exception}");
            _dirty = false;
            _clearPending = false;
            DropPreparedState();
            Clear();
        }
        finally
        {
           _renderer.SubmitMarkerBatches();
       }
   }

    public void Dispose()
    {
       if (_disposed) return;
       _disposed = true;

       _signalBus.TryUnsubscribe<BeatmapEditingModeSwitchedSignal>(HandleEditingModeChanged);
      _signalBus.TryUnsubscribe<EventBoxSelectedSignal>(HandleEventBoxSelected);
      _signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(HandleEventBoxesUpdated);
      _signalBus.TryUnsubscribe<EventBoxModifiedSignal>(HandleEventBoxModified);
      _signalBus.TryUnsubscribe<BeatmapLevelUpdatedSignal>(HandleBeatmapLevelUpdated);
      _signalBus.TryUnsubscribe<BeatmapLevelStateTimeUpdated>(HandleBeatmapLevelStateTimeUpdated);
      _signalBus.TryUnsubscribe<MotionPathRefreshSignal>(HandleRefresh);
       _songPreviewController.playheadPositionChangedEvent -= HandlePlaybackPositionChanged;
       _livePreviewBeatmapData.beatmapEventDataWasInsertedEvent -= HandleEventInserted;
        _livePreviewBeatmapData.beatmapEventDataWasRemovedEvent -= HandleEventRemoved;
       _requestVersion++;
       DropPreparedState();
        Clear();
        _renderer.Dispose();
   }

   private bool CanRender()
   {
       return _config.MotionPath.Enabled
              && _beatmapState.editingMode == BeatmapEditingMode.EventBoxes;
   }

     private void InvalidateGeometry()
     {
        _requestVersion++;
        DropPreparedState();
        _dirty = true;
        _clearPending = true;
       _rebuildAfterFrame = UnityEngine.Time.frameCount + 1;
    }

        private void HandleEditingModeChanged(BeatmapEditingModeSwitchedSignal _)
         {
            InvalidateGeometry();
        }

     private void HandleEventBoxSelected(EventBoxSelectedSignal _) => RefreshPlanContext();
     private void HandleEventBoxesUpdated(EventBoxesUpdatedSignal _) => RefreshPlanContext();
     private void HandleEventBoxModified(EventBoxModifiedSignal _) => RefreshPlanContext();
     private void HandleBeatmapLevelUpdated(BeatmapLevelUpdatedSignal _) => RefreshPlanContext();
    private void HandleBeatmapLevelStateTimeUpdated()
    {
       QueuePresentationUpdate();
    }
    private void HandlePlaybackPositionChanged(int _) => QueuePresentationUpdate();
     private void HandleRefresh(MotionPathRefreshSignal _)
     {
        var currentConfiguration = MotionPathPlanConfiguration.Capture(_config.MotionPath);
         if (_planConfiguration.RequiresRebuild(currentConfiguration))
         {
            _planConfiguration = currentConfiguration;
            InvalidateGeometry();
            return;
         }
         _planConfiguration = currentConfiguration;
          QueuePresentationUpdate();
      }
     private void HandleEventInserted(BeatmapEventData _, System.Collections.Generic.LinkedListNode<BeatmapDataItem> __) => InvalidateGeometry();
     private void HandleEventRemoved(BeatmapEventData _) => InvalidateGeometry();

     private void StartBake()
     {
       _dirty = false;
       _planConfiguration = MotionPathPlanConfiguration.Capture(_config.MotionPath);
        if (!_viewLocator.TryGetEventBoxView(out var eventBoxView)
          || eventBoxView._eventBox == null)
      {
         Clear();
         return;
      }

       var group = _eventBoxGroupsState.eventBoxGroupContext;
       if (group == null
           || (group.type != EventBoxGroupType.Rotation && group.type != EventBoxGroupType.Translation))
       {
          Clear();
          return;
       }

         var selectedPreparationOutcome = _transformPlanner.CreatePreparation(
            group,
            eventBoxView._eventBox,
            out var preparation);
         if (selectedPreparationOutcome == MotionPathBakePreparationOutcome.Ready)
         {
            _preparation = preparation;
            _preparationRequestVersion = _requestVersion;
            return;
         }

       Clear();
       if (selectedPreparationOutcome == MotionPathBakePreparationOutcome.ServicesUnavailable)
          RetryAfterServicesInitialize();
    }

      private void SetSession(MotionPathTransformPlanner.IBakeSession session)
      {
         ClearPlanCache();
         _session = session;
         _sessionRequestVersion = _requestVersion;
      }

      private void StartPreview(float requestedBeat, bool clearVisuals)
      {
         if (TryRenderCachedPlan(requestedBeat))
         {
            DropPendingOperation();
            ScheduleForwardPrefetch(requestedBeat);
            return;
         }

         StartOperation(
            requestedBeat,
            GetBackwardRange(),
            GetForwardRange(),
            Math.Min(_planConfiguration.SamplesPerBeat, PreviewSamplesPerBeat),
            PendingOperationPurpose.Preview);
         if (_pendingOperation == null || !clearVisuals) return;

         Clear();
         _clearPending = false;
      }

      private void StartRefinement(
         float requestedBeat,
         float backwardRangeInBeats,
         float forwardRangeInBeats,
         float currentBeat)
      {
         var cachedPlan = FindCachedPlan(requestedBeat, backwardRangeInBeats, forwardRangeInBeats);
         if (cachedPlan != null
             && cachedPlan.ContainsVisibleWindow(currentBeat, GetBackwardRange(), GetForwardRange()))
         {
            DropPendingOperation();
            Render(cachedPlan, currentBeat, true);
            ScheduleForwardPrefetch(currentBeat);
            return;
         }

         StartOperation(
            requestedBeat,
            backwardRangeInBeats,
            forwardRangeInBeats,
            _planConfiguration.SamplesPerBeat,
            PendingOperationPurpose.Refinement);
      }

      private void StartPrefetch(float requestedBeat, float currentBeat)
      {
         var backwardRangeInBeats = GetBackwardRange();
         var forwardRangeInBeats = GetForwardRange();
         var cachedPlan = FindCachedForwardPlan(
            requestedBeat,
            backwardRangeInBeats,
            forwardRangeInBeats);
         if (cachedPlan != null)
         {
            if (!ReferenceEquals(cachedPlan, _plan)
                && cachedPlan.ContainsVisibleWindow(currentBeat, backwardRangeInBeats, forwardRangeInBeats))
               Render(cachedPlan, currentBeat, true);
            return;
         }

         StartOperation(
            requestedBeat,
            backwardRangeInBeats,
            forwardRangeInBeats,
            _planConfiguration.SamplesPerBeat,
            PendingOperationPurpose.Prefetch);
      }

      private void StartOperation(
         float requestedBeat,
         float backwardRangeInBeats,
         float forwardRangeInBeats,
         int samplesPerBeat,
         PendingOperationPurpose purpose)
      {
         DropPendingOperation();
         if (_session == null || _sessionRequestVersion != _requestVersion) return;

         _pendingOperation = _session.CreateOperation(
            requestedBeat,
            backwardRangeInBeats,
            forwardRangeInBeats,
            samplesPerBeat);
         _pendingPurpose = purpose;
         _pendingRequestedBeat = requestedBeat;
         _pendingBackwardRangeInBeats = backwardRangeInBeats;
         _pendingForwardRangeInBeats = forwardRangeInBeats;
         _pendingRequestVersion = _requestVersion;
      }

     private void DropPendingOperation()
     {
         _pendingOperation = null;
         _pendingPurpose = PendingOperationPurpose.None;
         _pendingRequestedBeat = 0f;
         _pendingBackwardRangeInBeats = 0f;
         _pendingForwardRangeInBeats = 0f;
         _pendingRequestVersion = 0;
     }

      private void DropPreparedState()
      {
         DropPreparation();
         DropPendingOperation();
         _session = null;
         _sessionRequestVersion = 0;
         ClearPlanCache();
      }

      private void DropPreparation()
      {
         _preparation = null;
         _preparationRequestVersion = 0;
      }

      private void EnsureForwardPrefetchMatchesCurrentRange(float currentBeat)
     {
        if (_plan == null || _plan.BakedEndBeat >= _plan.SongEndBeat)
        {
           DropPendingOperation();
           return;
        }

         var nextCenterBeat = _plan.BakedEndBeat - GetForwardRange();
         if (_pendingOperation != null
             && _pendingPurpose == PendingOperationPurpose.Prefetch
             && _pendingOperation.ContainsVisibleWindow(
                nextCenterBeat,
                GetBackwardRange(),
                GetForwardRange())) return;

         DropPendingOperation();
         StartPrefetch(nextCenterBeat, currentBeat);
      }

      private void ScheduleForwardPrefetch(float currentBeat)
     {
         if (_pendingOperation != null
             || _plan == null
             || !_planIsFullQuality
             || _plan.BakedEndBeat >= _plan.SongEndBeat) return;

         StartPrefetch(_plan.BakedEndBeat - GetForwardRange(), currentBeat);
      }

      private void CompletePendingOperation(float currentBeat)
      {
         var completedPlan = _pendingOperation?.CompletedPlan;
         if (completedPlan == null
             || _pendingRequestVersion != _requestVersion
             || _sessionRequestVersion != _requestVersion) return;

         var purpose = _pendingPurpose;
         var requestedBeat = _pendingRequestedBeat;
         var backwardRangeInBeats = _pendingBackwardRangeInBeats;
         var forwardRangeInBeats = _pendingForwardRangeInBeats;
         DropPendingOperation();
         switch (purpose)
         {
            case PendingOperationPurpose.Preview:
               if (!completedPlan.ContainsVisibleWindow(currentBeat, GetBackwardRange(), GetForwardRange()))
                  return;
               if (_planConfiguration.SamplesPerBeat <= PreviewSamplesPerBeat)
               {
                  Render(CacheFullQualityPlan(completedPlan), currentBeat, true);
                  ScheduleForwardPrefetch(currentBeat);
                  return;
               }

               Render(completedPlan, currentBeat, false);
               StartRefinement(
                  requestedBeat,
                  backwardRangeInBeats,
                  forwardRangeInBeats,
                  currentBeat);
               return;
            case PendingOperationPurpose.Refinement:
               if (!completedPlan.ContainsVisibleWindow(currentBeat, GetBackwardRange(), GetForwardRange()))
                  return;
               Render(CacheFullQualityPlan(completedPlan), currentBeat, true);
               ScheduleForwardPrefetch(currentBeat);
               return;
            case PendingOperationPurpose.Prefetch:
               var cachedPlan = CacheFullQualityPlan(completedPlan);
               if (cachedPlan.ContainsVisibleWindow(currentBeat, GetBackwardRange(), GetForwardRange()))
               {
                  Render(cachedPlan, currentBeat, true);
                  ScheduleForwardPrefetch(currentBeat);
               }
               return;
         }
      }

         private void Render(MotionPathPlan plan, float currentBeat, bool isFullQuality)
        {
           _plan = plan;
           _planIsFullQuality = isFullQuality;
          _renderedBeat = currentBeat;
          _rendererCleared = false;
         _renderer.Render(
           plan,
           currentBeat,
            true,
           GetSelectedEventBoxGroupType());
        _presentationUpdatePending = false;
     }

        private void RefreshPlanContext()
        {
           InvalidateGeometry();
       }

      private void QueuePresentationUpdate()
      {
         _presentationUpdatePending = true;
      }

    private EventBoxGroupType? GetSelectedEventBoxGroupType()
    {
       if (_beatmapState.editingMode != BeatmapEditingMode.EventBoxes)
          return null;

       return _eventBoxGroupsState.eventBoxGroupContext?.type switch
       {
          EventBoxGroupType.Rotation => EventBoxGroupType.Rotation,
          EventBoxGroupType.Translation => EventBoxGroupType.Translation,
          _ => null
       };
    }

   private void RetryAfterServicesInitialize()
   {
       _dirty = true;
       _rebuildAfterFrame = UnityEngine.Time.frameCount + ServiceRetryFrames;
   }

    private void Clear()
    {
       if (!_rendererCleared)
       {
          _renderer.Clear();
          _rendererCleared = true;
       }
        _plan = null;
        _planIsFullQuality = false;
       _presentationUpdatePending = false;
       _renderedBeat = float.NaN;
   }

   private float GetCurrentBeat()
   {
      return _songPreviewController.isPlaying
         ? _audioDataModel.bpmData.SampleToBeat(_songPreviewController.currentSample)
         : _beatmapState.beat;
   }

    private void UpdateForBeat(float beat)
    {
       if (!CanRender() || _plan == null) return;
       if (!ContainsVisibleWindow(_plan, beat)) return;

       _renderer.RefreshPresentation(
          beat,
           true,
          GetSelectedEventBoxGroupType());
       _renderedBeat = beat;
       _presentationUpdatePending = false;
   }

     private float GetBackwardRange() => _config.MotionPath.GetBackwardRangeInBeats();
     private float GetForwardRange() => _config.MotionPath.GetForwardRangeInBeats();
      private bool ContainsVisibleWindow(MotionPathPlan plan, float beat) =>
         plan != null && plan.ContainsVisibleWindow(beat, GetBackwardRange(), GetForwardRange());

      private bool TryRenderCachedPlan(float requestedBeat)
      {
         var cachedPlan = FindCachedPlan(requestedBeat, GetBackwardRange(), GetForwardRange());
         if (cachedPlan == null) return false;

         Render(cachedPlan, requestedBeat, true);
         return true;
      }

      private MotionPathPlan FindCachedPlan(
         float requestedBeat,
         float backwardRangeInBeats,
         float forwardRangeInBeats)
      {
         for (var node = _planCache.Last; node != null; node = node.Previous)
         {
            if (!node.Value.ContainsVisibleWindow(
                   requestedBeat,
                   backwardRangeInBeats,
                   forwardRangeInBeats)) continue;

            _planCache.Remove(node);
            _planCache.AddLast(node);
            return node.Value;
         }

         return null;
      }

      private MotionPathPlan CacheFullQualityPlan(MotionPathPlan plan)
      {
         for (var node = _planCache.Last; node != null; node = node.Previous)
         {
            if (!node.Value.BakedStartBeat.Equals(plan.BakedStartBeat)
                || !node.Value.BakedEndBeat.Equals(plan.BakedEndBeat)) continue;

            _planCache.Remove(node);
            _planCache.AddLast(node);
            return node.Value;
         }

         _planCache.AddLast(plan);
         while (_planCache.Count > CachedPlanLimit) _planCache.RemoveFirst();
         return plan;
      }

      private MotionPathPlan FindCachedForwardPlan(
         float requestedBeat,
         float backwardRangeInBeats,
         float forwardRangeInBeats)
      {
         if (_plan == null) return null;

         for (var node = _planCache.Last; node != null; node = node.Previous)
         {
            if (ReferenceEquals(node.Value, _plan)
                || node.Value.BakedEndBeat <= _plan.BakedEndBeat
                || !node.Value.ContainsVisibleWindow(
                   requestedBeat,
                   backwardRangeInBeats,
                   forwardRangeInBeats)) continue;

            _planCache.Remove(node);
            _planCache.AddLast(node);
            return node.Value;
         }

         return null;
      }

      private void ClearPlanCache() => _planCache.Clear();

      private enum PendingOperationPurpose
      {
         None,
         Preview,
         Refinement,
         Prefetch
      }

    private readonly struct MotionPathPlanConfiguration
    {
       private MotionPathPlanConfiguration(
           bool enabled,
           int samplesPerBeat,
           float bakeBufferInBeats,
           int maximumSelectedTargets,
           int maximumSelectedTracks)
       {
           Enabled = enabled;
           SamplesPerBeat = samplesPerBeat;
           BakeBufferInBeats = bakeBufferInBeats;
           MaximumSelectedTargets = maximumSelectedTargets;
           MaximumSelectedTracks = maximumSelectedTracks;
       }

       private bool Enabled { get; }
        public int SamplesPerBeat { get; }
        private float BakeBufferInBeats { get; }
       private int MaximumSelectedTargets { get; }
       private int MaximumSelectedTracks { get; }

       public static MotionPathPlanConfiguration Capture(MotionPathConfig config) => new(
           config.Enabled,
           config.GetSamplesPerBeat(),
           config.GetBakeBufferInBeats(),
           config.GetMaximumSelectedTargets(),
           config.GetMaximumSelectedTracks());

       public bool RequiresRebuild(MotionPathPlanConfiguration other)
       {
           return Enabled != other.Enabled
                  || SamplesPerBeat != other.SamplesPerBeat
                 || !UnityEngine.Mathf.Approximately(BakeBufferInBeats, other.BakeBufferInBeats)
                  || MaximumSelectedTargets != other.MaximumSelectedTargets
                  || MaximumSelectedTracks != other.MaximumSelectedTracks;
       }
    }
}
