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
using UnityEngine;
using Zenject;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.MotionPath;

internal sealed class MotionPathManager : IInitializable, ILateTickable, IDisposable
{
   private const double BakeBudgetMilliseconds = 1.5d;
   private const int CachedChunkLimit = 10;
   private const int ServiceRetryFrames = 30;

   private readonly AudioDataModel _audioDataModel;
   private readonly List<int> _aheadChunkOrder = [];
   private readonly BeatmapData _livePreviewBeatmapData;
   private readonly List<int> _behindChunkOrder = [];
   private readonly BeatmapState _beatmapState;
   private readonly Dictionary<MotionPathChunkKey, ChunkBuildState> _chunkBuilds = [];
   private readonly Dictionary<MotionPathChunkKey, LinkedListNode<MotionPathChunk>> _chunkCache = [];
   private readonly LinkedList<MotionPathChunk> _chunkLru = [];
   private readonly PluginConfig _config;
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly List<ChunkPriorityGroup> _priorityGroups = [];
   private readonly List<MotionPathChunkKey> _removedBuildKeys = [];
   private readonly List<RenderedSegmentKey> _removedRenderedSegmentKeys = [];
   private readonly HashSet<int> _renderedChunks = [];
   private readonly Dictionary<RenderedSegmentKey, long> _renderedSegments = [];
   private readonly MotionPathRenderer _renderer;
   private readonly HashSet<int> _retainedChunks = [];
   private readonly HashSet<int> _visibleChunks = [];
   private readonly List<int> _visibleChunkOrder = [];
   private readonly MotionPathTransformPlanner _transformPlanner;
   private readonly EditorViewLocator _viewLocator;
   private readonly List<ChunkWorkItem> _workQueue = [];
   private readonly SignalBus _signalBus;
   private readonly ISongPreviewController _songPreviewController;

   private ChunkWorkItem _activeWork;
   private bool _clearPending;
   private bool _dirty = true;
   private bool _disposed;
   private MotionPathTransformPlanner.IBakeOperation _operation;
   private MotionPathTransformPlanner.IBakeOperation _drainingOperation;
   private long _operationRequestVersion;
   private MotionPathGeometryConfiguration _geometryConfiguration;
   private MotionPathTransformPlanner.IPreparationOperation _preparation;
   private long _preparationRequestVersion;
   private bool _presentationUpdatePending;
   private int _rebuildAfterFrame;
   private float _renderedBeat = float.NaN;
   private long _requestVersion;
   private MotionPathTransformPlanner.IBakeSession _session;
   private long _sessionRequestVersion;

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
      _geometryConfiguration = MotionPathGeometryConfiguration.Capture(_config.MotionPath);
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
            ClearRenderer();
            _clearPending = false;
         }

         if (!DrainStaleOperation()) return;

         if (!CanRender())
         {
            DropPlanningState();
            ClearChunkData();
            ClearRenderer();
            return;
         }

         var currentBeat = GetCurrentBeat();
         if (_dirty && Time.frameCount > _rebuildAfterFrame)
         {
            RefreshPresentationIfNeeded(currentBeat);
            StartBake();
            return;
         }

         if (_preparation != null)
         {
            RefreshPresentationIfNeeded(currentBeat);
            AdvancePreparation();
            return;
         }

         if (_session == null || _sessionRequestVersion != _requestVersion)
         {
            RefreshPresentationIfNeeded(currentBeat);
            return;
         }

         BuildPriorities(currentBeat);
         RemoveUnneededPartialBuilds();
         RemoveOneUnneededRenderedChunk(currentBeat);
         BuildWorkQueue();
         EnsureActiveOperationMatchesPriority();
         if (!DrainStaleOperation()) return;
         if (TryPublishAvailableSegment(currentBeat, 1)) return;

         var visibleWorkPending = _workQueue.Count > 0 && _workQueue[0].PriorityClass <= 1;
         if (_operation != null && (_activeWork.PriorityClass <= 1 || !visibleWorkPending))
         {
            AdvanceOperation(currentBeat);
            return;
         }

         if (_operation == null && visibleWorkPending)
         {
            StartOperation(_workQueue[0]);
            AdvanceOperation(currentBeat);
            return;
         }

         if (TryPublishAvailableSegment(currentBeat, int.MaxValue)) return;

         if (_operation == null && _workQueue.Count > 0) StartOperation(_workQueue[0]);
         if (_operation != null)
         {
            AdvanceOperation(currentBeat);
            return;
         }

         RefreshPresentationIfNeeded(currentBeat);
         EvictChunks();
      }
      catch (Exception exception)
      {
         Plugin.Log.Error($"Motion-path bake failed: {exception}");
         _dirty = false;
         _clearPending = false;
         DropPlanningState();
         ClearChunkData();
         ClearRenderer();
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
      DropPlanningState(true);
      ClearChunkData();
      ClearRenderer();
      _renderer.Dispose();
   }

   private bool CanRender() =>
      _config.MotionPath.Enabled && _beatmapState.editingMode == BeatmapEditingMode.EventBoxes;

   private void InvalidateGeometry(bool clearRenderedSegments)
   {
      _requestVersion++;
      DropPlanningState();
      ClearChunkData();
      _dirty = true;
      _clearPending |= clearRenderedSegments;
      _presentationUpdatePending = true;
      _rebuildAfterFrame = Time.frameCount + 1;
   }

   private void HandleEditingModeChanged(BeatmapEditingModeSwitchedSignal _) => InvalidateGeometry(true);
   private void HandleEventBoxSelected(EventBoxSelectedSignal _) => InvalidateGeometry(true);
   private void HandleEventBoxesUpdated(EventBoxesUpdatedSignal _) => InvalidateGeometry(false);
   private void HandleEventBoxModified(EventBoxModifiedSignal _) => InvalidateGeometry(false);
   private void HandleBeatmapLevelUpdated(BeatmapLevelUpdatedSignal _) => InvalidateGeometry(false);
   private void HandleBeatmapLevelStateTimeUpdated() => QueuePresentationUpdate();
   private void HandlePlaybackPositionChanged(int _) => QueuePresentationUpdate();

   private void HandleRefresh(MotionPathRefreshSignal _)
   {
      var currentConfiguration = MotionPathGeometryConfiguration.Capture(_config.MotionPath);
      if (_geometryConfiguration.RequiresRebuild(currentConfiguration))
      {
         var clearRenderedSegments = _geometryConfiguration.Enabled != currentConfiguration.Enabled;
         _geometryConfiguration = currentConfiguration;
         InvalidateGeometry(clearRenderedSegments);
         return;
      }

      _geometryConfiguration = currentConfiguration;
      QueuePresentationUpdate();
   }

   private void HandleEventInserted(
      BeatmapEventData _,
      LinkedListNode<BeatmapDataItem> __) => InvalidateGeometry(false);

   private void HandleEventRemoved(BeatmapEventData _) => InvalidateGeometry(false);

   private void StartBake()
   {
      _dirty = false;
      _geometryConfiguration = MotionPathGeometryConfiguration.Capture(_config.MotionPath);
      if (!_viewLocator.TryGetEventBoxView(out var eventBoxView) || eventBoxView._eventBox == null)
      {
         ClearRenderer();
         return;
      }

      var group = _eventBoxGroupsState.eventBoxGroupContext;
      if (group == null
          || group.type != EventBoxGroupType.Rotation && group.type != EventBoxGroupType.Translation)
      {
         ClearRenderer();
         return;
      }

      var outcome = _transformPlanner.CreatePreparation(group, eventBoxView._eventBox, out var preparation);
      if (outcome == MotionPathBakePreparationOutcome.Ready)
      {
         _preparation = preparation;
         _preparationRequestVersion = _requestVersion;
         return;
      }

      if (outcome == MotionPathBakePreparationOutcome.ServicesUnavailable)
         RetryAfterServicesInitialize();
      else
         ClearRenderer();
   }

   private void AdvancePreparation()
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

      var outcome = _preparation.CompletedOutcome.Value;
      var session = _preparation.CompletedSession;
      DropPreparation();
      if (outcome == MotionPathBakePreparationOutcome.Ready && session != null)
      {
         _session = session;
         _sessionRequestVersion = _requestVersion;
         return;
      }

      if (outcome == MotionPathBakePreparationOutcome.ServicesUnavailable)
         RetryAfterServicesInitialize();
      else
         ClearRenderer();
   }

   private void BuildPriorities(float currentBeat)
   {
      _priorityGroups.Clear();
      _retainedChunks.Clear();
      _visibleChunks.Clear();
      _visibleChunkOrder.Clear();
      _aheadChunkOrder.Clear();
      _behindChunkOrder.Clear();
      if (_session.SongEndBeat <= 0f || _session.TrackCount <= 0) return;

      var visibleStartBeat = Mathf.Clamp(
         currentBeat - _config.MotionPath.GetBackwardRangeInBeats(),
         0f,
         _session.SongEndBeat);
      var visibleEndBeat = Mathf.Clamp(
         currentBeat + _config.MotionPath.GetForwardRangeInBeats(),
         visibleStartBeat,
         _session.SongEndBeat);
      var firstVisibleChunk = GetChunkIndex(visibleStartBeat);
      var lastVisibleChunk = GetChunkIndex(visibleEndBeat);
      var lastSongChunk = GetLastChunkIndex();
      for (var chunkIndex = firstVisibleChunk; chunkIndex <= lastVisibleChunk; chunkIndex++)
         AddChunk(_visibleChunkOrder, chunkIndex);

      AddChunk(_aheadChunkOrder, lastVisibleChunk + 1, lastSongChunk);
      AddChunk(_aheadChunkOrder, lastVisibleChunk + 2, lastSongChunk);
      AddChunk(_behindChunkOrder, firstVisibleChunk - 1, lastSongChunk);

      foreach (var chunkIndex in _visibleChunkOrder)
      {
         _visibleChunks.Add(chunkIndex);
         _retainedChunks.Add(chunkIndex);
      }
      foreach (var chunkIndex in _aheadChunkOrder) _retainedChunks.Add(chunkIndex);
      foreach (var chunkIndex in _behindChunkOrder) _retainedChunks.Add(chunkIndex);

      AddPriorityGroup(_visibleChunkOrder, 0, _session.FocusedTrackCount, 0);
      AddPriorityGroup(
         _visibleChunkOrder,
         _session.FocusedTrackCount,
         _session.TrackCount - _session.FocusedTrackCount,
         1);
      AddPriorityGroup(_aheadChunkOrder, 0, _session.FocusedTrackCount, 2);
      AddPriorityGroup(
         _aheadChunkOrder,
         _session.FocusedTrackCount,
         _session.TrackCount - _session.FocusedTrackCount,
         3);
      AddPriorityGroup(_behindChunkOrder, 0, _session.TrackCount, 4);
   }

   private void AddChunk(List<int> chunks, int chunkIndex, int lastChunkIndex = int.MaxValue)
   {
      if (chunkIndex < 0 || chunkIndex > lastChunkIndex || chunks.Contains(chunkIndex)) return;
      chunks.Add(chunkIndex);
   }

   private void AddPriorityGroup(
      List<int> chunks,
      int firstTrackIndex,
      int trackCount,
      int priorityClass)
   {
      if (chunks.Count == 0 || trackCount <= 0) return;
      _priorityGroups.Add(new ChunkPriorityGroup(chunks, firstTrackIndex, trackCount, priorityClass));
   }

   private void BuildWorkQueue()
   {
      _workQueue.Clear();
      foreach (var group in _priorityGroups)
      foreach (var chunkIndex in group.ChunkIndices)
      {
         var key = CreateChunkKey(chunkIndex);
         if (!TryFindMissingTrackRange(
                key,
                group.FirstTrackIndex,
                group.TrackCount,
                group.PriorityClass,
                out var work))
            continue;
         _workQueue.Add(work);
      }
   }

   private bool TryFindMissingTrackRange(
      MotionPathChunkKey key,
      int firstTrackIndex,
      int trackCount,
      int priorityClass,
      out ChunkWorkItem work)
   {
      var endTrackIndex = firstTrackIndex + trackCount;
      var missingStart = firstTrackIndex;
      while (missingStart < endTrackIndex && HasTrack(key, missingStart)) missingStart++;
      if (missingStart >= endTrackIndex)
      {
         work = default;
         return false;
      }

      var missingEnd = missingStart + 1;
      while (missingEnd < endTrackIndex && !HasTrack(key, missingEnd)) missingEnd++;
      work = new ChunkWorkItem(key, missingStart, missingEnd - missingStart, priorityClass);
      return true;
   }

   private bool TryPublishAvailableSegment(float currentBeat, int maximumPriorityClass)
   {
      foreach (var group in _priorityGroups)
      {
         if (group.PriorityClass > maximumPriorityClass) break;
         foreach (var chunkIndex in group.ChunkIndices)
         {
            var key = CreateChunkKey(chunkIndex);
            var endTrackIndex = group.FirstTrackIndex + group.TrackCount;
            for (var trackIndex = group.FirstTrackIndex; trackIndex < endTrackIndex; trackIndex++)
            {
               var renderedKey = new RenderedSegmentKey(chunkIndex, trackIndex);
               if (_renderedSegments.TryGetValue(renderedKey, out var generation)
                   && generation == _requestVersion)
                  continue;
               if (!TryGetTrack(key, trackIndex, out var track)) continue;

               PublishSegment(key, trackIndex, track, currentBeat);
               TouchChunk(key);
               return true;
            }
         }
      }

      return false;
   }

   private void EnsureActiveOperationMatchesPriority()
   {
      if (_operation == null) return;
      if (_operationRequestVersion != _requestVersion || _workQueue.Count == 0)
      {
         DropOperation();
         return;
      }

      var activeRequiredPriorityClass = int.MaxValue;
      var activeEnd = _activeWork.FirstTrackIndex + _activeWork.TrackCount;
      foreach (var work in _workQueue)
      {
         if (work.Key != _activeWork.Key
             || work.FirstTrackIndex < _activeWork.FirstTrackIndex
             || work.FirstTrackIndex >= activeEnd) continue;
         activeRequiredPriorityClass = Math.Min(activeRequiredPriorityClass, work.PriorityClass);
      }
      if (activeRequiredPriorityClass == int.MaxValue)
      {
         DropOperation();
         return;
      }

      _activeWork = new ChunkWorkItem(
         _activeWork.Key,
         _activeWork.FirstTrackIndex,
         _activeWork.TrackCount,
         activeRequiredPriorityClass);
      var highestPriorityClass = _workQueue[0].PriorityClass;
      var visibleWorkPreemptsBackground = highestPriorityClass <= 1 && _activeWork.PriorityClass > 1;
      var higherVisibleClassPreemptsVisible = _activeWork.PriorityClass <= 1
                                             && highestPriorityClass < _activeWork.PriorityClass;
      if (visibleWorkPreemptsBackground || higherVisibleClassPreemptsVisible) DropOperation();
   }

   private void StartOperation(ChunkWorkItem work)
   {
      if (_session == null || _sessionRequestVersion != _requestVersion) return;
      _activeWork = work;
      _operation = _session.CreateOperation(work.Key, work.FirstTrackIndex, work.TrackCount);
      _operationRequestVersion = _requestVersion;
   }

   private void AdvanceOperation(float currentBeat)
   {
      if (_operationRequestVersion != _requestVersion
          || _sessionRequestVersion != _requestVersion
          || _operation.Key.GeometryGeneration != _requestVersion
          || _operation.Key.SamplesPerBeat != _geometryConfiguration.SamplesPerBeat)
      {
         DropOperation();
         return;
      }

      _operation.Advance(BakeBudgetMilliseconds);
      if (_operationRequestVersion != _requestVersion || _sessionRequestVersion != _requestVersion)
      {
         DropOperation();
         return;
      }

      if (_operation.TryTakeTrackPublication(out var publication))
      {
         if (publication.Key != _operation.Key
             || publication.Key.GeometryGeneration != _requestVersion
             || publication.Key.SamplesPerBeat != _geometryConfiguration.SamplesPerBeat)
            return;

         var build = GetOrCreateBuild(publication.Key);
         build.SetTrack(publication.TrackIndex, publication.Track);
         PublishSegment(publication.Key, publication.TrackIndex, publication.Track, currentBeat);
         if (build.Completed)
         {
            CacheCompletedBuild(build);
            _renderer.TrimTracks(
               _session.TrackCount,
               currentBeat,
               CanEditVisibleEvents(),
               GetSelectedEventBoxGroupType());
         }
         return;
      }

      if (_operation.Completed) DropOperation();
      RefreshPresentationIfNeeded(currentBeat);
   }

   private void PublishSegment(
      MotionPathChunkKey key,
      int trackIndex,
      MotionPathTrack track,
      float currentBeat)
   {
      if (key.GeometryGeneration != _requestVersion
          || key.SamplesPerBeat != _geometryConfiguration.SamplesPerBeat
          || !_retainedChunks.Contains(key.ChunkIndex))
         return;

        var refreshAllTracks = _renderedChunks.Count > 0
           && (_renderedBeat != currentBeat || _presentationUpdatePending);
       _renderedChunks.Add(key.ChunkIndex);
       _renderedSegments[new RenderedSegmentKey(key.ChunkIndex, trackIndex)] = key.GeometryGeneration;
        if (!_renderer.SetTrackSegment(
           trackIndex,
           key.ChunkIndex,
          key.GeometryGeneration,
          track,
          currentBeat,
          CanEditVisibleEvents(),
           GetSelectedEventBoxGroupType(),
           refreshAllTracks))
          return;
       _renderedBeat = currentBeat;
       _presentationUpdatePending = false;
   }

   private ChunkBuildState GetOrCreateBuild(MotionPathChunkKey key)
   {
      if (_chunkBuilds.TryGetValue(key, out var build)) return build;
      build = new ChunkBuildState(key, _session.TrackCount, _session.SongEndBeat);
      _chunkBuilds.Add(key, build);
      return build;
   }

   private bool HasTrack(MotionPathChunkKey key, int trackIndex)
   {
      if (_chunkCache.TryGetValue(key, out var cached))
         return trackIndex >= 0 && trackIndex < cached.Value.Tracks.Count;
      return _chunkBuilds.TryGetValue(key, out var build) && build.HasTrack(trackIndex);
   }

   private bool TryGetTrack(MotionPathChunkKey key, int trackIndex, out MotionPathTrack track)
   {
      if (_chunkCache.TryGetValue(key, out var cached)
          && trackIndex >= 0
          && trackIndex < cached.Value.Tracks.Count)
      {
         track = cached.Value.Tracks[trackIndex];
         return true;
      }

      if (_chunkBuilds.TryGetValue(key, out var build)) return build.TryGetTrack(trackIndex, out track);
      track = null;
      return false;
   }

   private void CacheCompletedBuild(ChunkBuildState build)
   {
      if (_chunkCache.ContainsKey(build.Key))
      {
         _chunkBuilds.Remove(build.Key);
         TouchChunk(build.Key);
         return;
      }

      var node = _chunkLru.AddLast(build.CreateChunk());
      _chunkCache.Add(build.Key, node);
      _chunkBuilds.Remove(build.Key);
      EvictChunks();
   }

   private void TouchChunk(MotionPathChunkKey key)
   {
      if (!_chunkCache.TryGetValue(key, out var node)) return;
      _chunkLru.Remove(node);
      _chunkLru.AddLast(node);
   }

   private void EvictChunks()
   {
      while (_chunkLru.Count > CachedChunkLimit)
      {
         var candidate = _chunkLru.First;
         while (candidate != null && _retainedChunks.Contains(candidate.Value.Key.ChunkIndex))
            candidate = candidate.Next;
         candidate ??= _chunkLru.First;
         _chunkLru.Remove(candidate);
         _chunkCache.Remove(candidate.Value.Key);
      }
   }

   private bool RemoveOneUnneededRenderedChunk(float currentBeat)
   {
      var removedChunk = -1;
      foreach (var chunkIndex in _renderedChunks)
      {
         if (_retainedChunks.Contains(chunkIndex)) continue;
         removedChunk = chunkIndex;
         break;
      }
      if (removedChunk < 0) return false;

      _renderer.RemoveChunk(
         removedChunk,
         currentBeat,
         CanEditVisibleEvents(),
         GetSelectedEventBoxGroupType());
      _renderedChunks.Remove(removedChunk);
      _removedRenderedSegmentKeys.Clear();
      foreach (var pair in _renderedSegments)
         if (pair.Key.ChunkIndex == removedChunk)
            _removedRenderedSegmentKeys.Add(pair.Key);
      foreach (var key in _removedRenderedSegmentKeys) _renderedSegments.Remove(key);
      _renderedBeat = currentBeat;
      _presentationUpdatePending = false;
      return true;
   }

   private void RemoveUnneededPartialBuilds()
   {
      _removedBuildKeys.Clear();
      foreach (var pair in _chunkBuilds)
         if (!_retainedChunks.Contains(pair.Key.ChunkIndex))
            _removedBuildKeys.Add(pair.Key);
      foreach (var key in _removedBuildKeys) _chunkBuilds.Remove(key);
   }

   private void DropOperation()
   {
      var operation = _operation;
      _operation = null;
      _operationRequestVersion = 0;
      _activeWork = default;
      if (operation == null) return;

      operation.MarkStale();
      if (operation.Drain())
      {
         operation.Dispose(false);
         return;
      }

      if (_drainingOperation != null)
         throw new InvalidOperationException("Only one motion-path sampling job can drain at a time.");
      _drainingOperation = operation;
   }

   private bool DrainStaleOperation()
   {
      if (_drainingOperation == null) return true;
      if (!_drainingOperation.Drain()) return false;

      _drainingOperation.Dispose(false);
      _drainingOperation = null;
      return true;
   }

   private void DropPreparation()
   {
      _preparation = null;
      _preparationRequestVersion = 0;
   }

   private void DropPlanningState(bool completeRunningJob = false)
   {
      DropPreparation();
      DropOperation();
      if (completeRunningJob && _drainingOperation != null)
      {
         _drainingOperation.Dispose(true);
         _drainingOperation = null;
      }
      _session = null;
      _sessionRequestVersion = 0;
      _priorityGroups.Clear();
      _workQueue.Clear();
      _retainedChunks.Clear();
      _visibleChunks.Clear();
   }

   private void ClearChunkData()
   {
      _chunkBuilds.Clear();
      _chunkCache.Clear();
      _chunkLru.Clear();
   }

   private void ClearRenderer()
   {
      if (_renderedChunks.Count > 0 || _renderedSegments.Count > 0) _renderer.Clear();
      _renderedChunks.Clear();
      _renderedSegments.Clear();
      _presentationUpdatePending = false;
      _renderedBeat = float.NaN;
   }

   private void RefreshPresentationIfNeeded(float currentBeat)
   {
      if (_renderedChunks.Count == 0
          || !_presentationUpdatePending && _renderedBeat == currentBeat)
         return;
      _renderer.RefreshPresentation(currentBeat, CanEditVisibleEvents(), GetSelectedEventBoxGroupType());
      _renderedBeat = currentBeat;
      _presentationUpdatePending = false;
   }

   private void QueuePresentationUpdate() => _presentationUpdatePending = true;

   private bool CanEditVisibleEvents()
   {
      if (_session == null || _sessionRequestVersion != _requestVersion || _visibleChunks.Count == 0)
         return false;
      foreach (var chunkIndex in _visibleChunks)
      for (var trackIndex = 0; trackIndex < _session.TrackCount; trackIndex++)
      {
         var key = new RenderedSegmentKey(chunkIndex, trackIndex);
         if (!_renderedSegments.TryGetValue(key, out var generation) || generation != _requestVersion)
            return false;
      }
      return true;
   }

   private void RetryAfterServicesInitialize()
   {
      _dirty = true;
      _rebuildAfterFrame = Time.frameCount + ServiceRetryFrames;
   }

   private float GetCurrentBeat() => _songPreviewController.isPlaying
      ? _audioDataModel.bpmData.SampleToBeat(_songPreviewController.currentSample)
      : _beatmapState.beat;

   private int GetLastChunkIndex() =>
      Mathf.Max(0, Mathf.CeilToInt(_session.SongEndBeat / MotionPathChunk.SizeInBeats) - 1);

   private int GetChunkIndex(float beat)
   {
      if (beat >= _session.SongEndBeat) return GetLastChunkIndex();
      return Mathf.Clamp(
         Mathf.FloorToInt(beat / MotionPathChunk.SizeInBeats),
         0,
         GetLastChunkIndex());
   }

   private MotionPathChunkKey CreateChunkKey(int chunkIndex) =>
      new(chunkIndex, _requestVersion, _geometryConfiguration.SamplesPerBeat);

   private EventBoxGroupType? GetSelectedEventBoxGroupType()
   {
      if (_beatmapState.editingMode != BeatmapEditingMode.EventBoxes) return null;
      return _eventBoxGroupsState.eventBoxGroupContext?.type switch
      {
         EventBoxGroupType.Rotation => EventBoxGroupType.Rotation,
         EventBoxGroupType.Translation => EventBoxGroupType.Translation,
         _ => null
      };
   }

   private readonly struct ChunkPriorityGroup
   {
      public ChunkPriorityGroup(
         List<int> chunkIndices,
         int firstTrackIndex,
         int trackCount,
         int priorityClass)
      {
         ChunkIndices = chunkIndices;
         FirstTrackIndex = firstTrackIndex;
         TrackCount = trackCount;
         PriorityClass = priorityClass;
      }

      public List<int> ChunkIndices { get; }
      public int FirstTrackIndex { get; }
      public int PriorityClass { get; }
      public int TrackCount { get; }
   }

   private readonly struct ChunkWorkItem
   {
      public ChunkWorkItem(
         MotionPathChunkKey key,
         int firstTrackIndex,
         int trackCount,
         int priorityClass)
      {
         Key = key;
         FirstTrackIndex = firstTrackIndex;
         TrackCount = trackCount;
         PriorityClass = priorityClass;
      }

      public MotionPathChunkKey Key { get; }
      public int FirstTrackIndex { get; }
      public int PriorityClass { get; }
      public int TrackCount { get; }
   }

   private sealed class ChunkBuildState
   {
      private readonly float _songEndBeat;
      private readonly MotionPathTrack[] _tracks;
      private int _completedTrackCount;

      public ChunkBuildState(MotionPathChunkKey key, int trackCount, float songEndBeat)
      {
         Key = key;
         _tracks = new MotionPathTrack[trackCount];
         _songEndBeat = songEndBeat;
      }

      public bool Completed => _completedTrackCount == _tracks.Length;
      public MotionPathChunkKey Key { get; }

      public bool HasTrack(int trackIndex) =>
         trackIndex >= 0 && trackIndex < _tracks.Length && _tracks[trackIndex] != null;

      public bool TryGetTrack(int trackIndex, out MotionPathTrack track)
      {
         track = trackIndex >= 0 && trackIndex < _tracks.Length ? _tracks[trackIndex] : null;
         return track != null;
      }

      public void SetTrack(int trackIndex, MotionPathTrack track)
      {
         if (trackIndex < 0 || trackIndex >= _tracks.Length || track == null) return;
         if (_tracks[trackIndex] == null) _completedTrackCount++;
         _tracks[trackIndex] = track;
      }

      public MotionPathChunk CreateChunk()
      {
         var startBeat = (float)((long)Key.ChunkIndex * MotionPathChunk.SizeInBeats);
         var endBeat = Mathf.Min(startBeat + MotionPathChunk.SizeInBeats, _songEndBeat);
         return new MotionPathChunk(Key, startBeat, endBeat, _songEndBeat, _tracks);
      }
   }

   private readonly struct RenderedSegmentKey : IEquatable<RenderedSegmentKey>
   {
      public RenderedSegmentKey(int chunkIndex, int trackIndex)
      {
         ChunkIndex = chunkIndex;
         TrackIndex = trackIndex;
      }

      public int ChunkIndex { get; }
      private int TrackIndex { get; }

      public bool Equals(RenderedSegmentKey other) =>
         ChunkIndex == other.ChunkIndex && TrackIndex == other.TrackIndex;

      public override bool Equals(object obj) => obj is RenderedSegmentKey other && Equals(other);

      public override int GetHashCode()
      {
         unchecked
         {
            return ChunkIndex * 397 ^ TrackIndex;
         }
      }
   }

   private readonly struct MotionPathGeometryConfiguration
   {
      private MotionPathGeometryConfiguration(
         bool enabled,
         int samplesPerBeat,
         int maximumSelectedTargets,
         int maximumSelectedTracks)
      {
         Enabled = enabled;
         SamplesPerBeat = samplesPerBeat;
         MaximumSelectedTargets = maximumSelectedTargets;
         MaximumSelectedTracks = maximumSelectedTracks;
      }

      public bool Enabled { get; }
      public int SamplesPerBeat { get; }
      private int MaximumSelectedTargets { get; }
      private int MaximumSelectedTracks { get; }

      public static MotionPathGeometryConfiguration Capture(MotionPathConfig config) => new(
         config.Enabled,
         config.GetSamplesPerBeat(),
         config.GetMaximumSelectedTargets(),
         config.GetMaximumSelectedTracks());

      public bool RequiresRebuild(MotionPathGeometryConfiguration other) =>
         Enabled != other.Enabled
         || SamplesPerBeat != other.SamplesPerBeat
         || MaximumSelectedTargets != other.MaximumSelectedTargets
         || MaximumSelectedTracks != other.MaximumSelectedTracks;
   }
}
