using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo;
using EditorEnhanced.MotionPath.Configuration;
using EditorEnhanced.Utils;
using Tweening;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.MotionPath;

internal enum MotionPathBakePreparationOutcome
{
   Ready,
   ServicesUnavailable,
   NoData
}

internal sealed class MotionPathTransformPlanner
{
    private readonly AudioDataModel _audioDataModel;
   private readonly PluginConfig _config;
   private readonly BeatmapData _livePreviewBeatmapData;
   private readonly GizmoEffectContextResolver _effectContextResolver;
   private readonly MotionPathEventSourceResolver _eventSourceResolver;

   public MotionPathTransformPlanner(
      BeatmapData livePreviewBeatmapData,
      AudioDataModel audioDataModel,
      PluginConfig config,
      GizmoEffectContextResolver effectContextResolver,
      MotionPathEventSourceResolver eventSourceResolver)
   {
      _livePreviewBeatmapData = livePreviewBeatmapData;
      _audioDataModel = audioDataModel;
      _config = config;
      _effectContextResolver = effectContextResolver;
      _eventSourceResolver = eventSourceResolver;
   }

   public MotionPathBakePreparationOutcome CreatePreparation(
      EventBoxGroupEditorData group,
      EventBoxEditorData selectedEventBox,
      out IPreparationOperation operation)
   {
      operation = null;
      if (group.type is not (EventBoxGroupType.Rotation or EventBoxGroupType.Translation))
         return MotionPathBakePreparationOutcome.NoData;
      if (!_audioDataModel.isLoaded || !_effectContextResolver.TryResolve(group.type, out var effects))
         return MotionPathBakePreparationOutcome.ServicesUnavailable;
      if ((group.type == EventBoxGroupType.Rotation
           && selectedEventBox is not LightRotationEventBoxEditorData)
          || (group.type == EventBoxGroupType.Translation
              && selectedEventBox is not LightTranslationEventBoxEditorData))
         return MotionPathBakePreparationOutcome.NoData;

      operation = new PreparationOperation(
         this,
         effects,
          group,
          selectedEventBox,
          _config.MotionPath.GetMaximumSelectedTargets(),
          _config.MotionPath.GetMaximumSelectedTracks());
      return MotionPathBakePreparationOutcome.Ready;
   }

   private LightRotationBeatmapEventData[] GetRotationEvents(int groupId, int elementId, LightAxis axis)
   {
      return _livePreviewBeatmapData
         .GetBeatmapDataItems<LightRotationBeatmapEventData>(
            LightRotationBeatmapEventData.SubtypeIdentifier(groupId, elementId, axis))
         .ToArray();
   }

   private LightTranslationBeatmapEventData[] GetTranslationEvents(int groupId, int elementId, LightAxis axis)
   {
      return _livePreviewBeatmapData
         .GetBeatmapDataItems<LightTranslationBeatmapEventData>(
            LightTranslationBeatmapEventData.SubtypeIdentifier(groupId, elementId, axis))
         .ToArray();
   }

   private static TransformAnimation GetAnimation(
      IDictionary<Transform, TransformAnimation> animations,
      Transform transform)
   {
      if (animations.TryGetValue(transform, out var animation)) return animation;
      animation = new TransformAnimation();
      animations.Add(transform, animation);
      return animation;
   }

   internal interface IPreparationOperation
   {
      MotionPathBakePreparationOutcome? CompletedOutcome { get; }
      IBakeSession CompletedSession { get; }
      void Advance(double budgetMilliseconds);
   }

   private sealed class PreparationOperation : IPreparationOperation
   {
      private readonly Dictionary<Transform, TransformAnimation> _animations = [];
      private readonly List<RuntimeBinding> _bindings = [];
      private readonly GizmoEffectContext _effects;
      private readonly SortedSet<float> _eventBeats = [];
      private readonly EventBoxGroupEditorData _group;
      private readonly HashSet<int> _includedGroupIds = [];
      private readonly int _maximumTargets;
      private readonly int _maximumTracks;
      private readonly Dictionary<Transform, MotionPathNode> _nodes = [];
      private readonly MotionPathTransformPlanner _planner;
      private readonly HashSet<Transform> _relevantTransforms = [];
      private readonly EventBoxEditorData _selectedEventBox;
      private readonly List<Transform> _targets = [];
      private readonly List<MotionPathNode> _targetNodes = [];
      private readonly List<PreparedTrack> _tracks = [];

      private HashSet<MotionPathNode> _addedTrackNodes;
      private AnimationEvent[] _animationEvents;
      private int _ancestorTargetIndex;
      private RuntimeBinding _binding;
      private int _bindingEventIndex;
      private int _bindingIndex;
      private IEnumerator<AnimationEvent> _eventEnumerator;
      private int _eventTrackIndex;
      private int _nodeTargetIndex;
      private Transform _nodeWalk;
      private readonly Stack<Transform> _nodeStack = [];
      private MotionPathEventSourceResolver.MotionPathEventSourceLookupPreparation _lookupPreparation;
      private MotionPathEventSourceResolver.MotionPathEventSourceLookup _sourceLookup;
      private PreparationPhase _phase;
      private IEnumerator<LightRotationGroup> _rotationGroupEnumerator;
      private LightRotationGroup _rotationGroup;
      private int _rotationAxis;
      private int _rotationElement;
      private IEnumerator<LightTranslationGroup> _translationGroupEnumerator;
      private LightTranslationGroup _translationGroup;
      private int _translationAxis;
      private int _translationElement;
      private IEnumerator<(int index, int chunkIndex)> _targetEnumerator;
      private IReadOnlyList<Transform> _targetTransforms;
      private Transform _walkedAncestor;
      private int _walkTargetIndex;
      private MotionPathNode _trackAncestor;

      public PreparationOperation(
         MotionPathTransformPlanner planner,
         GizmoEffectContext effects,
          EventBoxGroupEditorData group,
          EventBoxEditorData selectedEventBox,
          int maximumTargets,
          int maximumTracks)
      {
         _planner = planner;
         _effects = effects;
         _group = group;
          _selectedEventBox = selectedEventBox;
          _maximumTargets = maximumTargets;
          _maximumTracks = maximumTracks;
      }

      public MotionPathBakePreparationOutcome? CompletedOutcome { get; private set; }
      public IBakeSession CompletedSession { get; private set; }

      public void Advance(double budgetMilliseconds)
      {
         if (CompletedOutcome.HasValue) return;

         var startedAt = Stopwatch.GetTimestamp();
         var budgetTicks = budgetMilliseconds * Stopwatch.Frequency / 1000d;
         do
         {
            AdvanceOne();
            if (CompletedOutcome.HasValue) return;
         }
         while (Stopwatch.GetTimestamp() - startedAt < budgetTicks);
      }

      private void AdvanceOne()
      {
         switch (_phase)
         {
            case PreparationPhase.FindTargetGroup:
               FindTargetGroup();
               break;
            case PreparationPhase.CollectTargets:
               CollectTarget();
               break;
            case PreparationPhase.CollectAncestors:
               CollectAncestor();
               break;
            case PreparationPhase.DiscoverRotationBindings:
               DiscoverRotationBinding();
               break;
            case PreparationPhase.DiscoverTranslationBindings:
               DiscoverTranslationBinding();
               break;
            case PreparationPhase.CreateSourceLookup:
               CreateSourceLookup();
               break;
            case PreparationPhase.BuildBindings:
               BuildBinding();
               break;
            case PreparationPhase.BuildNodes:
               BuildNode();
               break;
            case PreparationPhase.BuildTargetTracks:
               BuildTargetTrack();
               break;
            case PreparationPhase.BuildAncestorTracks:
               BuildAncestorTrack();
               break;
            case PreparationPhase.CollectEventBeats:
               CollectEventBeat();
               break;
            case PreparationPhase.CreateSession:
               CreateSession();
               break;
         }
      }

      private void FindTargetGroup()
      {
         if (_group.type == EventBoxGroupType.Rotation)
         {
            _rotationGroupEnumerator ??= _effects.RotationManager._lightRotationGroups
               .Cast<LightRotationGroup>()
               .GetEnumerator();
            if (!_rotationGroupEnumerator.MoveNext())
            {
               CompleteNoData();
               return;
            }

            var runtimeGroup = _rotationGroupEnumerator.Current;
            if (runtimeGroup.groupId != _group.groupId) return;
            _rotationGroupEnumerator.Dispose();
            _rotationGroupEnumerator = null;
            var eventBox = (LightRotationEventBoxEditorData)_selectedEventBox;
            _targetTransforms = GetRotationTransforms(runtimeGroup, eventBox.axis);
            _targetEnumerator = IndexFilterHelpers
               .GetIndexFilterRange(eventBox.indexFilter, runtimeGroup.numberOfElements)
               .GetEnumerator();
         }
         else
         {
            _translationGroupEnumerator ??= _effects.TranslationManager._lightTranslationGroups
               .Cast<LightTranslationGroup>()
               .GetEnumerator();
            if (!_translationGroupEnumerator.MoveNext())
            {
               CompleteNoData();
               return;
            }

            var runtimeGroup = _translationGroupEnumerator.Current;
            if (runtimeGroup.groupId != _group.groupId) return;
            _translationGroupEnumerator.Dispose();
            _translationGroupEnumerator = null;
            var eventBox = (LightTranslationEventBoxEditorData)_selectedEventBox;
            _targetTransforms = GetTranslationTransforms(runtimeGroup, eventBox.axis);
            _targetEnumerator = IndexFilterHelpers
               .GetIndexFilterRange(eventBox.indexFilter, runtimeGroup.numberOfElements)
               .GetEnumerator();
         }

         _phase = PreparationPhase.CollectTargets;
      }

      private void CollectTarget()
      {
         if (_targets.Count >= _maximumTargets || !_targetEnumerator.MoveNext())
         {
            _targetEnumerator.Dispose();
            _targetEnumerator = null;
            if (_targets.Count == 0)
            {
               CompleteNoData();
               return;
            }

            _phase = PreparationPhase.CollectAncestors;
            return;
         }

         var transform = _targetTransforms.ElementAtOrDefault(_targetEnumerator.Current.index);
         if (transform != null && !_targets.Contains(transform)) _targets.Add(transform);
      }

      private void CollectAncestor()
      {
         if (_walkedAncestor == null)
         {
            if (_walkTargetIndex >= _targets.Count)
            {
               _phase = PreparationPhase.DiscoverRotationBindings;
               return;
            }

            _walkedAncestor = _targets[_walkTargetIndex++];
         }

         if (!_relevantTransforms.Add(_walkedAncestor))
         {
            _walkedAncestor = null;
            return;
         }

         _walkedAncestor = _walkedAncestor.parent;
      }

      private void DiscoverRotationBinding()
      {
         if (_effects.RotationManager == null)
         {
            _phase = PreparationPhase.DiscoverTranslationBindings;
            return;
         }

         _rotationGroupEnumerator ??= _effects.RotationManager._lightRotationGroups
            .Cast<LightRotationGroup>()
            .GetEnumerator();
         if (_rotationGroup == null)
         {
            if (!_rotationGroupEnumerator.MoveNext())
            {
               _rotationGroupEnumerator.Dispose();
               _rotationGroupEnumerator = null;
               _phase = PreparationPhase.DiscoverTranslationBindings;
               return;
            }
            _rotationGroup = _rotationGroupEnumerator.Current;
         }

         var transforms = GetRotationTransforms(_rotationGroup, (LightAxis)_rotationAxis);
         if (_rotationElement >= transforms.Count)
         {
            _rotationElement = 0;
            _rotationAxis++;
            if (_rotationAxis >= 3)
            {
               _rotationAxis = 0;
               _rotationGroup = null;
            }
            return;
         }

         var elementId = _rotationElement++;
         var transform = transforms[elementId];
         if (transform == null || !_relevantTransforms.Contains(transform)) return;
         _bindings.Add(RuntimeBinding.Rotation(_rotationGroup, (LightAxis)_rotationAxis, elementId, transform));
         _includedGroupIds.Add(_rotationGroup.groupId);
      }

      private void DiscoverTranslationBinding()
      {
         if (_effects.TranslationManager == null)
         {
            BeginSourceLookup();
            return;
         }

         _translationGroupEnumerator ??= _effects.TranslationManager._lightTranslationGroups
            .Cast<LightTranslationGroup>()
            .GetEnumerator();
         if (_translationGroup == null)
         {
            if (!_translationGroupEnumerator.MoveNext())
            {
               _translationGroupEnumerator.Dispose();
               _translationGroupEnumerator = null;
               BeginSourceLookup();
               return;
            }
            _translationGroup = _translationGroupEnumerator.Current;
         }

         if (_translationElement >= _translationGroup.count)
         {
            _translationElement = 0;
            _translationAxis = 0;
            _translationGroup = null;
            return;
         }

         var axis = (LightAxis)_translationAxis;
         var elementId = _translationElement;
         var transform = GetTranslationTransforms(_translationGroup, axis).ElementAtOrDefault(elementId);
         _translationAxis++;
         if (_translationAxis >= 3)
         {
            _translationAxis = 0;
            _translationElement++;
         }
         if (transform == null || !_relevantTransforms.Contains(transform)) return;
         _bindings.Add(RuntimeBinding.Translation(_translationGroup, axis, elementId, transform));
         _includedGroupIds.Add(_translationGroup.groupId);
      }

      private void BeginSourceLookup()
      {
         _lookupPreparation = _planner._eventSourceResolver.CreateLookup(_includedGroupIds);
         _phase = PreparationPhase.CreateSourceLookup;
      }

      private void CreateSourceLookup()
      {
         _lookupPreparation.AdvanceOne();
         if (_lookupPreparation.CompletedLookup == null) return;
         _sourceLookup = _lookupPreparation.CompletedLookup;
         _lookupPreparation = null;
         _phase = PreparationPhase.BuildBindings;
      }

      private void BuildBinding()
      {
         if (_binding == null)
         {
            if (_bindingIndex >= _bindings.Count)
            {
               _phase = PreparationPhase.BuildNodes;
               return;
            }

            _binding = _bindings[_bindingIndex];
            if (_binding.ValueType == MotionPathEventValueType.Rotation)
            {
               _binding.RotationEvents = _planner.GetRotationEvents(
                  _binding.GroupId,
                  _binding.ElementId,
                  _binding.Axis);
               _animationEvents = new AnimationEvent[_binding.RotationEvents.Length];
            }
            else
            {
               _binding.TranslationEvents = _planner.GetTranslationEvents(
                  _binding.GroupId,
                  _binding.ElementId,
                  _binding.Axis);
               _animationEvents = new AnimationEvent[_binding.TranslationEvents.Length];
            }
            return;
         }

         if (_bindingEventIndex < _animationEvents.Length)
         {
            if (_binding.ValueType == MotionPathEventValueType.Rotation)
            {
               var runtimeEvent = _binding.RotationEvents[_bindingEventIndex];
               _animationEvents[_bindingEventIndex++] = new AnimationEvent(
                  runtimeEvent.time,
                  _sourceLookup.ResolveRotation(
                     _binding.GroupId,
                     _binding.ElementId,
                     _binding.Axis,
                     runtimeEvent,
                     _binding.Transform,
                     _binding.Mirrored));
            }
            else
            {
               var runtimeEvent = _binding.TranslationEvents[_bindingEventIndex];
               _animationEvents[_bindingEventIndex++] = new AnimationEvent(
                  runtimeEvent.time,
                  _sourceLookup.ResolveTranslation(
                     _binding.GroupId,
                     _binding.ElementId,
                     _binding.Axis,
                     runtimeEvent,
                     _binding.Transform,
                     _binding.Mirrored,
                     _binding.TranslationLimits,
                     _binding.DistributionLimits));
            }
            return;
         }

         var animation = GetAnimation(_animations, _binding.Transform);
         if (_binding.ValueType == MotionPathEventValueType.Rotation)
            animation.SetRotation(
               (int)_binding.Axis,
               new RotationBinding(_binding.RotationEvents, _animationEvents, _binding.Mirrored));
         else
            animation.SetTranslation(
               (int)_binding.Axis,
               new TranslationBinding(
                  _binding.TranslationEvents,
                  _animationEvents,
                  _binding.Mirrored,
                  _binding.TranslationLimits,
                  _binding.DistributionLimits));
         _binding = null;
         _bindingEventIndex = 0;
         _bindingIndex++;
      }

      private void BuildNode()
      {
         if (_nodeWalk != null)
         {
            if (_nodes.ContainsKey(_nodeWalk))
               _nodeWalk = null;
            else
            {
               _nodeStack.Push(_nodeWalk);
               _nodeWalk = _nodeWalk.parent;
            }
            return;
         }

         if (_nodeStack.Count > 0)
         {
            var transform = _nodeStack.Pop();
            _animations.TryGetValue(transform, out var animation);
            MotionPathNode parent = null;
            if (transform.parent != null) _nodes.TryGetValue(transform.parent, out parent);
            _nodes.Add(
               transform,
               new MotionPathNode(
                  transform,
                  parent,
                  transform.localPosition,
                  transform.localRotation,
                  transform.localScale,
                  animation));
            return;
         }

         if (_nodeTargetIndex >= _targets.Count)
         {
            _addedTrackNodes = new HashSet<MotionPathNode>(_targetNodes);
            _phase = PreparationPhase.BuildTargetTracks;
            return;
         }

         var target = _targets[_nodeTargetIndex];
         if (_nodes.TryGetValue(target, out var node))
         {
            _targetNodes.Add(node);
            _nodeTargetIndex++;
            return;
         }
         _nodeWalk = target;
      }

      private void BuildTargetTrack()
      {
         if (TrackLimitReached() || _tracks.Count >= _targetNodes.Count)
         {
            _phase = PreparationPhase.BuildAncestorTracks;
            return;
         }
         _tracks.Add(new PreparedTrack(_targetNodes[_tracks.Count], true));
      }

      private void BuildAncestorTrack()
      {
         if (TrackLimitReached())
         {
            BeginEventBeatCollection();
            return;
         }

         if (_trackAncestor == null)
         {
            if (_ancestorTargetIndex >= _targetNodes.Count)
            {
               BeginEventBeatCollection();
               return;
            }
            _trackAncestor = _targetNodes[_ancestorTargetIndex++].Parent;
            if (_trackAncestor == null) return;
         }

         var ancestor = _trackAncestor;
         _trackAncestor = ancestor.Parent;
         if (ancestor.Animation != null && _addedTrackNodes.Add(ancestor))
            _tracks.Add(new PreparedTrack(ancestor, false));
      }

      private bool TrackLimitReached() => _maximumTracks > 0 && _tracks.Count >= _maximumTracks;

      private void BeginEventBeatCollection()
      {
         if (_tracks.Count == 0)
         {
            CompleteNoData();
            return;
         }
         _phase = PreparationPhase.CollectEventBeats;
      }

      private void CollectEventBeat()
      {
         if (_eventEnumerator == null)
         {
            if (_eventTrackIndex >= _tracks.Count)
            {
               _phase = PreparationPhase.CreateSession;
               return;
            }
            _eventEnumerator = _tracks[_eventTrackIndex].GetEvents().GetEnumerator();
         }

         if (_eventEnumerator.MoveNext())
         {
            var beat = _eventEnumerator.Current.GetBeat(_planner._audioDataModel.bpmData);
            if (!float.IsNaN(beat) && !float.IsInfinity(beat)) _eventBeats.Add(beat);
            return;
         }

         _eventEnumerator.Dispose();
         _eventEnumerator = null;
         _eventTrackIndex++;
      }

      private void CreateSession()
      {
         var bpmData = _planner._audioDataModel.bpmData;
          CompletedSession = new BakeSession(
             _tracks,
             _eventBeats.ToArray(),
             bpmData,
             bpmData.totalBeats);
         CompletedOutcome = MotionPathBakePreparationOutcome.Ready;
      }

      private void CompleteNoData()
      {
         CompletedOutcome = MotionPathBakePreparationOutcome.NoData;
      }

      private static IReadOnlyList<Transform> GetRotationTransforms(LightRotationGroup group, LightAxis axis)
      {
         return axis switch
         {
            LightAxis.X => group.xTransforms,
            LightAxis.Y => group.yTransforms,
            _ => group.zTransforms
         };
      }

      private static IReadOnlyList<Transform> GetTranslationTransforms(LightTranslationGroup group, LightAxis axis)
      {
         return axis switch
         {
            LightAxis.X => group.xTransforms,
            LightAxis.Y => group.yTransforms,
            _ => group.zTransforms
         };
      }

      private sealed class RuntimeBinding
      {
         public LightAxis Axis;
         public Vector2 DistributionLimits;
         public int ElementId;
         public int GroupId;
         public bool Mirrored;
         public LightRotationBeatmapEventData[] RotationEvents;
         public Transform Transform;
         public Vector2 TranslationLimits;
         public LightTranslationBeatmapEventData[] TranslationEvents;
         public MotionPathEventValueType ValueType;

         public static RuntimeBinding Rotation(
            LightRotationGroup group,
            LightAxis axis,
            int elementId,
            Transform transform)
         {
            return new RuntimeBinding
            {
               ValueType = MotionPathEventValueType.Rotation,
               GroupId = group.groupId,
               Axis = axis,
               ElementId = elementId,
               Transform = transform,
               Mirrored = axis switch
               {
                  LightAxis.X => group.mirrorX,
                  LightAxis.Y => group.mirrorY,
                  _ => group.mirrorZ
               }
            };
         }

         public static RuntimeBinding Translation(
            LightTranslationGroup group,
            LightAxis axis,
            int elementId,
            Transform transform)
         {
            var (mirrored, translationLimits, distributionLimits) = axis switch
            {
               LightAxis.X => (group.mirrorX, group.xTranslationLimits, group.xDistributionLimits),
               LightAxis.Y => (group.mirrorY, group.yTranslationLimits, group.yDistributionLimits),
               _ => (group.mirrorZ, group.zTranslationLimits, group.zDistributionLimits)
            };
            return new RuntimeBinding
            {
               ValueType = MotionPathEventValueType.Translation,
               GroupId = group.groupId,
               Axis = axis,
               ElementId = elementId,
               Transform = transform,
               Mirrored = mirrored,
               TranslationLimits = translationLimits,
               DistributionLimits = distributionLimits
            };
         }
      }

      private enum PreparationPhase
      {
         FindTargetGroup,
         CollectTargets,
         CollectAncestors,
         DiscoverRotationBindings,
         DiscoverTranslationBindings,
         CreateSourceLookup,
         BuildBindings,
         BuildNodes,
         BuildTargetTracks,
         BuildAncestorTracks,
         CollectEventBeats,
         CreateSession
      }
   }

    private static Matrix4x4 GetWorldMatrix(
       MotionPathNode node,
       float time,
       IDictionary<MotionPathNode, Matrix4x4> matrices)
   {
      if (matrices.TryGetValue(node, out var matrix)) return matrix;
      matrix = Matrix4x4.TRS(node.GetPosition(time), node.GetRotation(time), node.LocalScale);
      if (node.Parent != null) matrix = GetWorldMatrix(node.Parent, time, matrices) * matrix;
       matrices.Add(node, matrix);
        return matrix;
     }

      internal interface IBakeSession
      {
         float SongEndBeat { get; }
         int TrackCount { get; }
         int FocusedTrackCount { get; }

         IBakeOperation CreateOperation(MotionPathChunkKey key, int firstTrackIndex, int trackCount);
      }

      internal interface IBakeOperation
      {
         MotionPathChunkKey Key { get; }
         MotionPathChunk CompletedChunk { get; }
         bool Completed { get; }
         bool IsJobRunning { get; }
         bool CanDisposeWithoutCompletion { get; }
         bool IsUsingMainThreadFallback { get; }
         Exception JobFallbackException { get; }

         void Advance(double budgetMilliseconds);
         bool TryTakeTrackPublication(out MotionPathTrackPublication publication);

         /// <summary>Rejects pending and future publications while preserving owned native state.</summary>
         void MarkStale();

         /// <summary>
         /// Polls a stale job and disposes native state after completion without blocking.
         /// </summary>
         /// <returns><see langword="true"/> when the operation no longer owns running job state.</returns>
         bool Drain();

         /// <summary>
         /// Disposes all native state. Shutdown can request completion of an in-flight job.
         /// </summary>
         /// <param name="completeRunningJob">
         /// If <see langword="true"/>, completes an in-flight job before disposal; otherwise, a running job is invalid.
         /// </param>
         void Dispose(bool completeRunningJob);
      }

        private sealed class BakeOperation : IBakeOperation
        {
           private const long MaximumBatchOutputPayloadBytes = 4L * 1024 * 1024;
           private const long MaximumReusableFlattenedNativeInputPayloadBytes = 16L * 1024 * 1024;
           private const long MaximumWorldMatrixScratchPayloadBytes = 4L * 1024 * 1024;

          private readonly BpmData _bpmData;
          private readonly float _endBeat;
          private readonly int _firstTrackIndex;
          private readonly bool _isFinalChunk;
          private readonly BakeSampleData _sampleData;
          private readonly BakeSession _session;
          private readonly int _sessionTrackCount;
          private readonly float _songEndBeat;
          private readonly float _startBeat;
          private readonly int _trackCount;
          private readonly IReadOnlyList<PreparedTrack> _tracks;

          private readonly List<BakedTrack> _bakedTracks = [];
          private readonly Stack<MotionPathNode> _nodeStack = [];
          private readonly Dictionary<MotionPathNode, int> _nodeIndices = [];
          private readonly List<MotionPathNode> _samplingNodes = [];
          private readonly List<SamplingNodeDescriptor> _samplingNodeDescriptors = [];
          private readonly List<RotationBindingDescriptor> _rotationBindingDescriptors = [];
          private readonly List<TranslationBindingDescriptor> _translationBindingDescriptors = [];
          private readonly List<RotationEventDescriptor> _rotationEventDescriptors = [];
          private readonly List<TranslationEventDescriptor> _translationEventDescriptors = [];
          private readonly Dictionary<LightRotationBeatmapEventData, int> _rotationEventIndices =
             new(ReferenceComparer<LightRotationBeatmapEventData>.Instance);
          private readonly Dictionary<LightTranslationBeatmapEventData, int> _translationEventIndices =
             new(ReferenceComparer<LightTranslationBeatmapEventData>.Instance);
          private readonly List<int> _trackNodeIndices = [];
          private readonly List<float> _sampleBeats = [];
          private readonly List<float> _sampleTimes = [];

           private AncestorMatrixCache _ancestorMatrixCache;
           private int _batchOutputPositionCount;
           private int _batchSampleCount;
           private int _batchSampleIndex;
           private int _batchSampleStartIndex;
           private int _batchWorldMatrixCount;
           private MotionPathTrack[] _completedTracks;
          private bool _disposed;
          private int _eventEndIndex;
          private int _eventStartIndex;
          private int _fallbackSampleIndex;
          private int _flattenTrackIndex;
          private JobHandle _jobHandle;
          private bool _jobScheduled;
          private Dictionary<MotionPathNode, Matrix4x4> _matrices;
          private NativeArray<MotionPathSamplingNode> _nativeNodes;
          private NativeArray<MotionPathSamplingRotationBinding> _nativeRotationBindings;
          private NativeArray<MotionPathSamplingTranslationBinding> _nativeTranslationBindings;
          private NativeArray<MotionPathSamplingRotationEvent> _nativeRotationEvents;
          private NativeArray<MotionPathSamplingTranslationEvent> _nativeTranslationEvents;
          private NativeArray<int> _nativeTrackNodeIndices;
          private NativeArray<float> _nativeSampleTimes;
           private NativeArray<Vector3> _nativeOutputPositions;
           private NativeArray<Matrix4x4> _nativeWorldMatrixScratch;
          private MotionPathNode _nodeWalk;
           private MotionPathTrackPublication? _pendingPublication;
          private int _rotationBindingBuildIndex;
          private int _rotationBindingEventIndex;
          private int _rotationExternalSourceIndex;
          private int _rotationSourceEventCount;
          private int _samplingNodeBuildIndex;
          private SampleBeatCursor _sampleCursor;
          private bool _stale;
          private BakedTrack _track;
          private int _trackBuildIndex;
          private int _trackNodeBuildIndex;
          private int _trackOffset;
          private BakedTrack.TrackFinalizer _trackFinalizer;
          private int _translationBindingBuildIndex;
          private int _translationBindingEventIndex;
          private int _translationExternalSourceIndex;
          private int _translationSourceEventCount;
           private bool _usingMainThreadFallback;
           private int _maximumBatchSampleCount;

          private BakePhase _phase;

          public BakeOperation(
             BakeSession session,
             MotionPathChunkKey key,
             IReadOnlyList<PreparedTrack> tracks,
             int firstTrackIndex,
             int trackCount,
             int sessionTrackCount,
             BakeSampleData sampleData,
             BpmData bpmData,
             float startBeat,
             float endBeat,
             float songEndBeat)
          {
             _session = session;
             Key = key;
             _tracks = tracks;
             _firstTrackIndex = firstTrackIndex;
             _trackCount = trackCount;
             _sessionTrackCount = sessionTrackCount;
             _sampleData = sampleData;
             _bpmData = bpmData;
             _startBeat = startBeat;
             _endBeat = endBeat;
             _songEndBeat = songEndBeat;
             _isFinalChunk = endBeat.Equals(songEndBeat);
             _phase = BakePhase.PrepareEventRange;
          }

          public bool Completed { get; private set; }
          public MotionPathChunk CompletedChunk { get; private set; }
          public MotionPathChunkKey Key { get; }
          public bool IsJobRunning => !_disposed && _jobScheduled && !_jobHandle.IsCompleted;
          public bool CanDisposeWithoutCompletion =>
             _disposed || !_jobScheduled || _jobHandle.IsCompleted;
          public bool IsUsingMainThreadFallback => _usingMainThreadFallback;
          public Exception JobFallbackException { get; private set; }

          public void Advance(double budgetMilliseconds)
          {
             if (_disposed || Completed || _pendingPublication.HasValue) return;
             if (_stale)
             {
                Drain();
                return;
             }

             var startedAt = Stopwatch.GetTimestamp();
             var budgetTicks = budgetMilliseconds * Stopwatch.Frequency / 1000d;
             do
             {
                try
                {
                   AdvanceOne();
                }
                catch (Exception exception) when (CanActivateMainThreadFallback())
                {
                   ActivateMainThreadFallback(exception);
                }

                if (_disposed || Completed || _pendingPublication.HasValue) return;
             }
             while (Stopwatch.GetTimestamp() - startedAt < budgetTicks);
          }

          public bool TryTakeTrackPublication(out MotionPathTrackPublication publication)
          {
             if (_stale || !_pendingPublication.HasValue)
             {
                publication = default;
                return false;
             }

             publication = _pendingPublication.Value;
             _pendingPublication = null;
             return true;
          }

          public void MarkStale()
          {
             _stale = true;
             _pendingPublication = null;
          }

          public bool Drain()
          {
             if (_disposed) return true;
             MarkStale();
             if (_jobScheduled)
             {
                if (!_jobHandle.IsCompleted) return false;
                try
                {
                   _jobHandle.Complete();
                }
                finally
                {
                   _jobScheduled = false;
                   DisposeNativeArrays();
                }
             }

             DisposeNativeArrays();
             return true;
          }

          public void Dispose(bool completeRunningJob)
          {
             if (_disposed) return;
             MarkStale();
             if (_jobScheduled && !_jobHandle.IsCompleted && !completeRunningJob)
                throw new InvalidOperationException(
                   "A running motion-path sampling job must be drained or completed before disposal.");

             try
             {
                if (_jobScheduled) _jobHandle.Complete();
             }
             finally
             {
                _jobScheduled = false;
                DisposeNativeArrays();
                _disposed = true;
             }
          }

          private void AdvanceOne()
          {
             switch (_phase)
             {
                case BakePhase.PrepareEventRange:
                   PrepareEventRange();
                   break;
                case BakePhase.AllocateCompletedTracks:
                   AllocateCompletedTracks();
                   break;
                case BakePhase.BuildSampleBeats:
                   BuildSampleBeat();
                   break;
                case BakePhase.BuildSampleTimes:
                   BuildSampleTime();
                   break;
                case BakePhase.FlattenNodes:
                   FlattenNode();
                   break;
                case BakePhase.BuildNodeDescriptors:
                   BuildNodeDescriptor();
                   break;
                case BakePhase.BuildTrackNodeIndices:
                   BuildTrackNodeIndex();
                   break;
                case BakePhase.BuildRotationBindings:
                   BuildRotationBindingDescriptor();
                   break;
                case BakePhase.BuildTranslationBindings:
                   BuildTranslationBindingDescriptor();
                   break;
                case BakePhase.BuildRotationEvents:
                   BuildRotationEventDescriptor();
                   break;
                case BakePhase.BuildTranslationEvents:
                   BuildTranslationEventDescriptor();
                   break;
                case BakePhase.BuildRotationExternalEvents:
                   BuildRotationExternalEventDescriptor();
                   break;
                case BakePhase.BuildTranslationExternalEvents:
                   BuildTranslationExternalEventDescriptor();
                   break;
                case BakePhase.ValidateNativeAllocation:
                   ValidateNativeAllocation();
                   break;
                case BakePhase.AllocateNodes:
                    _nativeNodes = new NativeArray<MotionPathSamplingNode>(
                       _samplingNodeDescriptors.Count,
                       Allocator.Persistent,
                       NativeArrayOptions.UninitializedMemory);
                    _phase = BakePhase.AllocateRotationBindings;
                    break;
                case BakePhase.AllocateRotationBindings:
                    _nativeRotationBindings = new NativeArray<MotionPathSamplingRotationBinding>(
                       _rotationBindingDescriptors.Count,
                       Allocator.Persistent,
                       NativeArrayOptions.UninitializedMemory);
                    _phase = BakePhase.AllocateTranslationBindings;
                    break;
                case BakePhase.AllocateTranslationBindings:
                    _nativeTranslationBindings = new NativeArray<MotionPathSamplingTranslationBinding>(
                       _translationBindingDescriptors.Count,
                       Allocator.Persistent,
                       NativeArrayOptions.UninitializedMemory);
                    _phase = BakePhase.AllocateRotationEvents;
                    break;
                case BakePhase.AllocateRotationEvents:
                    _nativeRotationEvents = new NativeArray<MotionPathSamplingRotationEvent>(
                       _rotationEventDescriptors.Count,
                       Allocator.Persistent,
                       NativeArrayOptions.UninitializedMemory);
                    _phase = BakePhase.AllocateTranslationEvents;
                    break;
                case BakePhase.AllocateTranslationEvents:
                    _nativeTranslationEvents = new NativeArray<MotionPathSamplingTranslationEvent>(
                       _translationEventDescriptors.Count,
                       Allocator.Persistent,
                       NativeArrayOptions.UninitializedMemory);
                    _phase = BakePhase.AllocateTrackNodeIndices;
                    break;
                case BakePhase.AllocateTrackNodeIndices:
                    _nativeTrackNodeIndices = new NativeArray<int>(
                       _trackNodeIndices.Count,
                       Allocator.Persistent,
                       NativeArrayOptions.UninitializedMemory);
                    _phase = BakePhase.AllocateSampleTimes;
                    break;
                case BakePhase.AllocateSampleTimes:
                    _nativeSampleTimes = new NativeArray<float>(
                       _sampleTimes.Count,
                       Allocator.Persistent,
                       NativeArrayOptions.UninitializedMemory);
                    _phase = BakePhase.PopulateNodes;
                    break;
                case BakePhase.PrepareSampleBatch:
                    PrepareSampleBatch();
                    break;
                case BakePhase.AllocateBatchOutputPositions:
                    _nativeOutputPositions = new NativeArray<Vector3>(
                       _batchOutputPositionCount,
                       Allocator.Persistent,
                       NativeArrayOptions.UninitializedMemory);
                    _phase = BakePhase.AllocateBatchWorldMatrixScratch;
                    break;
                case BakePhase.AllocateBatchWorldMatrixScratch:
                    _nativeWorldMatrixScratch = new NativeArray<Matrix4x4>(
                       _batchWorldMatrixCount,
                       Allocator.Persistent,
                       NativeArrayOptions.UninitializedMemory);
                    _phase = BakePhase.ScheduleJob;
                    break;
                case BakePhase.PopulateNodes:
                   PopulateNode();
                   break;
                case BakePhase.PopulateRotationBindings:
                   PopulateRotationBinding();
                   break;
                case BakePhase.PopulateTranslationBindings:
                   PopulateTranslationBinding();
                   break;
                case BakePhase.PopulateRotationEvents:
                   PopulateRotationEvent();
                   break;
                case BakePhase.PopulateTranslationEvents:
                   PopulateTranslationEvent();
                   break;
                case BakePhase.PopulateTrackNodeIndices:
                   PopulateTrackNodeIndex();
                   break;
                case BakePhase.PopulateSampleTimes:
                   PopulateSampleTime();
                   break;
                case BakePhase.CreateBakedTracks:
                    CreateBakedTrack();
                    break;
                case BakePhase.ScheduleJob:
                   ScheduleJob();
                   break;
                case BakePhase.WaitForJob:
                   WaitForJob();
                   break;
                case BakePhase.CopyJobSamples:
                   CopyJobSample();
                   break;
                case BakePhase.BeginJobFinalizing:
                   BeginJobFinalizing();
                   break;
                case BakePhase.FallbackBeginTrack:
                   BeginFallbackTrack();
                   break;
                case BakePhase.FallbackPrepareSampling:
                   PrepareFallbackSampling();
                   break;
                case BakePhase.FallbackSampling:
                   AdvanceFallbackSampling();
                   break;
                case BakePhase.Finalizing:
                   AdvanceFinalizing();
                   break;
             }
          }

          private void PrepareEventRange()
          {
             _sampleData.GetEventBeatRange(
                _startBeat,
                _endBeat,
                _isFinalChunk,
                out _eventStartIndex,
                out _eventEndIndex);
             _phase = _firstTrackIndex == 0 && _trackCount == _sessionTrackCount
                ? BakePhase.AllocateCompletedTracks
                : BakePhase.BuildSampleBeats;
          }

          private void AllocateCompletedTracks()
          {
             _completedTracks = new MotionPathTrack[_trackCount];
             _phase = BakePhase.BuildSampleBeats;
          }

          private void BuildSampleBeat()
          {
             if (_sampleCursor == null)
             {
                var firstGridTick = (long)Key.ChunkIndex
                                    * MotionPathChunk.SizeInBeats
                                    * Key.SamplesPerBeat;
                _sampleCursor = new SampleBeatCursor(
                   _startBeat,
                   _endBeat,
                   _isFinalChunk,
                   _sampleData.EventBeats,
                   _eventStartIndex,
                   _eventEndIndex,
                   Key.SamplesPerBeat,
                   firstGridTick);
             }

             if (_sampleCursor.TryMoveNext(out var beat))
             {
                _sampleBeats.Add(beat);
                return;
             }

             _sampleCursor = null;
             _phase = BakePhase.BuildSampleTimes;
          }

          private void BuildSampleTime()
          {
             if (_sampleTimes.Count < _sampleBeats.Count)
             {
                _sampleTimes.Add(_bpmData.BeatToSeconds(_sampleBeats[_sampleTimes.Count]));
                return;
             }

             if (_trackCount == 0)
             {
                CompleteBake();
                return;
             }

             if (_session.IsJobFallbackActive(Key.GeometryGeneration))
             {
                _usingMainThreadFallback = true;
                _phase = BakePhase.FallbackBeginTrack;
                return;
             }

             _phase = BakePhase.FlattenNodes;
          }

          private void FlattenNode()
          {
             if (_nodeWalk != null)
             {
                if (_nodeIndices.ContainsKey(_nodeWalk))
                   _nodeWalk = null;
                else
                {
                   _nodeStack.Push(_nodeWalk);
                   _nodeWalk = _nodeWalk.Parent;
                }
                return;
             }

             if (_nodeStack.Count > 0)
             {
                var node = _nodeStack.Pop();
                var parentIndex = -1;
                if (node.Parent != null && !_nodeIndices.TryGetValue(node.Parent, out parentIndex))
                   throw new InvalidOperationException("Motion-path nodes must be flattened parent first.");
                _nodeIndices.Add(node, _samplingNodes.Count);
                _samplingNodes.Add(node);
                return;
             }

             if (_flattenTrackIndex < _trackCount)
             {
                _nodeWalk = _tracks[_firstTrackIndex + _flattenTrackIndex].Node;
                _flattenTrackIndex++;
                return;
             }

             _phase = BakePhase.BuildNodeDescriptors;
          }

          private void BuildNodeDescriptor()
          {
             if (_samplingNodeBuildIndex >= _samplingNodes.Count)
             {
                _samplingNodeBuildIndex = 0;
                _phase = BakePhase.BuildTrackNodeIndices;
                return;
             }

             var node = _samplingNodes[_samplingNodeBuildIndex++];
             var descriptor = new SamplingNodeDescriptor
             {
                Node = node,
                ParentIndex = node.Parent == null ? -1 : _nodeIndices[node.Parent],
                RotationXBindingIndex = -1,
                RotationYBindingIndex = -1,
                RotationZBindingIndex = -1,
                TranslationXBindingIndex = -1,
                TranslationYBindingIndex = -1,
                TranslationZBindingIndex = -1
             };
             var animation = node.Animation;
             descriptor.HasRotation = animation != null && animation.HasRotation;
             descriptor.HasTranslation = animation != null && animation.HasTranslation;
             for (var axis = 0; axis < 3; axis++)
             {
                var rotation = animation?.GetLastRotationBinding(axis);
                if (rotation != null)
                {
                   var index = _rotationBindingDescriptors.Count;
                   _rotationBindingDescriptors.Add(new RotationBindingDescriptor(rotation));
                   SetRotationBindingIndex(descriptor, axis, index);
                }

                var translation = animation?.GetLastTranslationBinding(axis);
                if (translation == null) continue;
                var translationIndex = _translationBindingDescriptors.Count;
                _translationBindingDescriptors.Add(new TranslationBindingDescriptor(translation));
                SetTranslationBindingIndex(descriptor, axis, translationIndex);
             }
             _samplingNodeDescriptors.Add(descriptor);
          }

          private void BuildTrackNodeIndex()
          {
             if (_trackNodeBuildIndex >= _trackCount)
             {
                _phase = BakePhase.BuildRotationBindings;
                return;
             }

             var node = _tracks[_firstTrackIndex + _trackNodeBuildIndex++].Node;
             _trackNodeIndices.Add(_nodeIndices[node]);
          }

          private void BuildRotationBindingDescriptor()
          {
             if (_rotationBindingBuildIndex >= _rotationBindingDescriptors.Count)
             {
                _phase = BakePhase.BuildTranslationBindings;
                return;
             }

             var descriptorIndex = _rotationBindingBuildIndex++;
             var descriptor = _rotationBindingDescriptors[descriptorIndex];
             descriptor.EventStartIndex = descriptorIndex == 0
                ? 0
                : CheckedSum(
                   _rotationBindingDescriptors[descriptorIndex - 1].EventStartIndex,
                   _rotationBindingDescriptors[descriptorIndex - 1].EventCount);
             descriptor.EventCount = descriptor.Binding.EventCount;
             EnsureEventCapacity(descriptor.EventStartIndex, descriptor.EventCount);
             _rotationSourceEventCount = CheckedSum(descriptor.EventStartIndex, descriptor.EventCount);
          }

          private void BuildTranslationBindingDescriptor()
          {
             if (_translationBindingBuildIndex >= _translationBindingDescriptors.Count)
             {
                _phase = BakePhase.BuildRotationEvents;
                return;
             }

             var descriptorIndex = _translationBindingBuildIndex++;
             var descriptor = _translationBindingDescriptors[descriptorIndex];
             descriptor.EventStartIndex = descriptorIndex == 0
                ? 0
                : CheckedSum(
                   _translationBindingDescriptors[descriptorIndex - 1].EventStartIndex,
                   _translationBindingDescriptors[descriptorIndex - 1].EventCount);
             descriptor.EventCount = descriptor.Binding.EventCount;
             EnsureEventCapacity(descriptor.EventStartIndex, descriptor.EventCount);
             _translationSourceEventCount = CheckedSum(descriptor.EventStartIndex, descriptor.EventCount);
          }

          private void BuildRotationEventDescriptor()
          {
             if (_rotationBindingEventIndex >= _rotationBindingDescriptors.Count)
             {
                _phase = BakePhase.BuildTranslationEvents;
                return;
             }

             var binding = _rotationBindingDescriptors[_rotationBindingEventIndex];
             var eventOffset = _rotationEventDescriptors.Count - binding.EventStartIndex;
             if (eventOffset >= binding.EventCount)
             {
                _rotationBindingEventIndex++;
                return;
             }

             var eventData = binding.Binding.GetEvent(eventOffset);
             AddRotationEventDescriptor(eventData);
          }

          private void BuildTranslationEventDescriptor()
          {
             if (_translationBindingEventIndex >= _translationBindingDescriptors.Count)
             {
                _phase = BakePhase.BuildRotationExternalEvents;
                return;
             }

             var binding = _translationBindingDescriptors[_translationBindingEventIndex];
             var eventOffset = _translationEventDescriptors.Count - binding.EventStartIndex;
             if (eventOffset >= binding.EventCount)
             {
                _translationBindingEventIndex++;
                return;
             }

             var eventData = binding.Binding.GetEvent(eventOffset);
             AddTranslationEventDescriptor(eventData);
          }

          private void BuildRotationExternalEventDescriptor()
          {
             if (_rotationExternalSourceIndex >= _rotationSourceEventCount)
             {
                _phase = BakePhase.BuildTranslationExternalEvents;
                return;
             }

             var next = _rotationEventDescriptors[_rotationExternalSourceIndex++]
                .Event.nextSameTypeEventData as LightRotationBeatmapEventData;
             if (next != null && !_rotationEventIndices.ContainsKey(next)) AddRotationEventDescriptor(next);
          }

          private void BuildTranslationExternalEventDescriptor()
          {
             if (_translationExternalSourceIndex >= _translationSourceEventCount)
             {
                _phase = BakePhase.ValidateNativeAllocation;
                return;
             }

             var next = _translationEventDescriptors[_translationExternalSourceIndex++]
                .Event.nextSameTypeEventData as LightTranslationBeatmapEventData;
             if (next != null && !_translationEventIndices.ContainsKey(next)) AddTranslationEventDescriptor(next);
          }

           private void ValidateNativeAllocation()
           {
              if (_sampleBeats.Count == 0 || _samplingNodeDescriptors.Count == 0 || _trackNodeIndices.Count == 0)
                 throw new InvalidOperationException("Motion-path sampling requires at least one sample, node, and track.");
              var reusableInputBytes = CheckedByteSum(
                 GetPayloadBytes<MotionPathSamplingNode>(_samplingNodeDescriptors.Count),
                 GetPayloadBytes<MotionPathSamplingRotationBinding>(_rotationBindingDescriptors.Count));
              reusableInputBytes = CheckedByteSum(
                 reusableInputBytes,
                 GetPayloadBytes<MotionPathSamplingTranslationBinding>(_translationBindingDescriptors.Count));
              reusableInputBytes = CheckedByteSum(
                 reusableInputBytes,
                 GetPayloadBytes<MotionPathSamplingRotationEvent>(_rotationEventDescriptors.Count));
              reusableInputBytes = CheckedByteSum(
                 reusableInputBytes,
                 GetPayloadBytes<MotionPathSamplingTranslationEvent>(_translationEventDescriptors.Count));
              reusableInputBytes = CheckedByteSum(
                 reusableInputBytes,
                 GetPayloadBytes<int>(_trackNodeIndices.Count));
              reusableInputBytes = CheckedByteSum(
                 reusableInputBytes,
                 GetPayloadBytes<float>(_sampleTimes.Count));
              if (reusableInputBytes > MaximumReusableFlattenedNativeInputPayloadBytes)
                 throw new InvalidOperationException(
                    "Motion-path sampling flattened native input exceeds the 16 MiB safety limit.");

              var worldMatrixBytesPerSample =
                 GetPayloadBytes<Matrix4x4>(_samplingNodeDescriptors.Count);
              if (worldMatrixBytesPerSample > MaximumWorldMatrixScratchPayloadBytes)
                 throw new InvalidOperationException(
                    "Motion-path sampling world-matrix scratch for one sample exceeds the 4 MiB safety limit.");

              var outputBytesPerSample = GetPayloadBytes<Vector3>(_trackNodeIndices.Count);
              if (outputBytesPerSample > MaximumBatchOutputPayloadBytes)
                 throw new InvalidOperationException(
                    "Motion-path sampling output for one sample exceeds the 4 MiB safety limit.");

              var scratchLimitedSampleCount =
                 MaximumWorldMatrixScratchPayloadBytes / worldMatrixBytesPerSample;
              var outputLimitedSampleCount = MaximumBatchOutputPayloadBytes / outputBytesPerSample;
              _maximumBatchSampleCount = (int)Math.Min(
                 Math.Min(scratchLimitedSampleCount, outputLimitedSampleCount),
                 _sampleBeats.Count);
              if (_maximumBatchSampleCount <= 0)
                 throw new InvalidOperationException("Motion-path sampling cannot fit one sample in a native batch.");

              _samplingNodeBuildIndex = 0;
              _rotationBindingBuildIndex = 0;
              _translationBindingBuildIndex = 0;
             _rotationBindingEventIndex = 0;
             _translationBindingEventIndex = 0;
             _trackNodeBuildIndex = 0;
             _trackBuildIndex = 0;
             _phase = BakePhase.AllocateNodes;
          }

          private void PopulateNode()
          {
             var index = _samplingNodeBuildIndex;
             if (index >= _samplingNodeDescriptors.Count)
             {
                _samplingNodeBuildIndex = 0;
                _phase = BakePhase.PopulateRotationBindings;
                return;
             }

             var descriptor = _samplingNodeDescriptors[index];
             _nativeNodes[index] = new MotionPathSamplingNode
             {
                ParentIndex = descriptor.ParentIndex,
                LocalPosition = descriptor.Node.LocalPosition,
                LocalRotation = descriptor.Node.LocalRotation,
                LocalScale = descriptor.Node.LocalScale,
                RotationXBindingIndex = descriptor.RotationXBindingIndex,
                RotationYBindingIndex = descriptor.RotationYBindingIndex,
                RotationZBindingIndex = descriptor.RotationZBindingIndex,
                TranslationXBindingIndex = descriptor.TranslationXBindingIndex,
                TranslationYBindingIndex = descriptor.TranslationYBindingIndex,
                TranslationZBindingIndex = descriptor.TranslationZBindingIndex,
                HasRotation = descriptor.HasRotation ? (byte)1 : (byte)0,
                HasTranslation = descriptor.HasTranslation ? (byte)1 : (byte)0
             };
             _samplingNodeBuildIndex++;
          }

          private void PopulateRotationBinding()
          {
             if (_rotationBindingBuildIndex >= _rotationBindingDescriptors.Count)
             {
                _rotationBindingBuildIndex = 0;
                _phase = BakePhase.PopulateTranslationBindings;
                return;
             }

             var descriptor = _rotationBindingDescriptors[_rotationBindingBuildIndex];
             _nativeRotationBindings[_rotationBindingBuildIndex++] = new MotionPathSamplingRotationBinding
             {
                EventStartIndex = descriptor.EventStartIndex,
                EventCount = descriptor.EventCount,
                Mirrored = descriptor.Binding.Mirrored ? (byte)1 : (byte)0
             };
          }

          private void PopulateTranslationBinding()
          {
             if (_translationBindingBuildIndex >= _translationBindingDescriptors.Count)
             {
                _translationBindingBuildIndex = 0;
                _phase = BakePhase.PopulateRotationEvents;
                return;
             }

             var descriptor = _translationBindingDescriptors[_translationBindingBuildIndex];
             _nativeTranslationBindings[_translationBindingBuildIndex++] =
                new MotionPathSamplingTranslationBinding
                {
                   EventStartIndex = descriptor.EventStartIndex,
                   EventCount = descriptor.EventCount,
                   Mirrored = descriptor.Binding.Mirrored ? (byte)1 : (byte)0,
                   TranslationLimits = descriptor.Binding.TranslationLimits,
                   DistributionLimits = descriptor.Binding.DistributionLimits
                };
          }

          private void PopulateRotationEvent()
          {
             if (_rotationBindingEventIndex >= _rotationEventDescriptors.Count)
             {
                _rotationBindingEventIndex = 0;
                _phase = BakePhase.PopulateTranslationEvents;
                return;
             }

             var eventData = _rotationEventDescriptors[_rotationBindingEventIndex].Event;
             var next = eventData.nextSameTypeEventData as LightRotationBeatmapEventData;
             var nextIndex = -1;
             if (next != null) _rotationEventIndices.TryGetValue(next, out nextIndex);
             _nativeRotationEvents[_rotationBindingEventIndex++] = new MotionPathSamplingRotationEvent
             {
                Time = eventData.time,
                Value = eventData.rotation,
                EaseType = eventData.easeType,
                NextEventIndex = nextIndex,
                LoopCount = eventData.loopCount,
                RotationDirection = eventData.rotationDirection
             };
          }

          private void PopulateTranslationEvent()
          {
             if (_translationBindingEventIndex >= _translationEventDescriptors.Count)
             {
                _translationBindingEventIndex = 0;
                _phase = BakePhase.PopulateTrackNodeIndices;
                return;
             }

             var eventData = _translationEventDescriptors[_translationBindingEventIndex].Event;
             var next = eventData.nextSameTypeEventData as LightTranslationBeatmapEventData;
             var nextIndex = -1;
             if (next != null) _translationEventIndices.TryGetValue(next, out nextIndex);
             _nativeTranslationEvents[_translationBindingEventIndex++] = new MotionPathSamplingTranslationEvent
             {
                Time = eventData.time,
                Value = eventData.translation,
                Distribution = eventData.distribution,
                EaseType = eventData.easeType,
                NextEventIndex = nextIndex
             };
          }

          private void PopulateTrackNodeIndex()
          {
             if (_trackNodeBuildIndex >= _trackNodeIndices.Count)
             {
                _trackNodeBuildIndex = 0;
                _phase = BakePhase.PopulateSampleTimes;
                return;
             }

             _nativeTrackNodeIndices[_trackNodeBuildIndex] = _trackNodeIndices[_trackNodeBuildIndex];
             _trackNodeBuildIndex++;
          }

          private void PopulateSampleTime()
          {
             if (_trackBuildIndex >= _sampleTimes.Count)
             {
                _trackBuildIndex = 0;
                _phase = BakePhase.CreateBakedTracks;
                return;
             }

             _nativeSampleTimes[_trackBuildIndex] = _sampleTimes[_trackBuildIndex];
             _trackBuildIndex++;
          }

           private void CreateBakedTrack()
           {
              if (_trackBuildIndex >= _trackCount)
              {
                 _batchSampleStartIndex = 0;
                 _batchSampleCount = 0;
                 _batchSampleIndex = 0;
                 _phase = BakePhase.PrepareSampleBatch;
                 return;
              }

              _bakedTracks.Add(_tracks[_firstTrackIndex + _trackBuildIndex].CreateBakedTrack());
              _trackBuildIndex++;
           }

           private void PrepareSampleBatch()
           {
              if (_batchSampleStartIndex >= _sampleBeats.Count)
              {
                 _trackOffset = 0;
                 _phase = BakePhase.BeginJobFinalizing;
                 return;
              }

              var remainingSampleCount = _sampleBeats.Count - _batchSampleStartIndex;
              _batchSampleCount = Math.Min(_maximumBatchSampleCount, remainingSampleCount);
              if (_batchSampleCount <= 0)
                 throw new InvalidOperationException("Motion-path sampling cannot prepare an empty native batch.");

              _batchOutputPositionCount = CheckedProduct(_batchSampleCount, _trackCount);
              _batchWorldMatrixCount = CheckedProduct(_batchSampleCount, _samplingNodeDescriptors.Count);
              EnsurePayloadWithinLimit(
                 GetPayloadBytes<Vector3>(_batchOutputPositionCount),
                 MaximumBatchOutputPayloadBytes,
                 "batch output");
              EnsurePayloadWithinLimit(
                 GetPayloadBytes<Matrix4x4>(_batchWorldMatrixCount),
                 MaximumWorldMatrixScratchPayloadBytes,
                 "world-matrix scratch");
              _batchSampleIndex = 0;
              _trackOffset = 0;
              _phase = BakePhase.AllocateBatchOutputPositions;
           }

          private void ScheduleJob()
          {
             var job = new MotionPathSamplingJob
             {
                Nodes = _nativeNodes,
                RotationBindings = _nativeRotationBindings,
                TranslationBindings = _nativeTranslationBindings,
                RotationEvents = _nativeRotationEvents,
                 TranslationEvents = _nativeTranslationEvents,
                 TrackNodeIndices = _nativeTrackNodeIndices,
                 SampleTimes = _nativeSampleTimes,
                 SampleStartIndex = _batchSampleStartIndex,
                 OutputPositions = _nativeOutputPositions,
                 WorldMatrixScratch = _nativeWorldMatrixScratch
              };
             _jobHandle = job.Schedule(_batchSampleCount, 1);
             _jobScheduled = true;
             _phase = BakePhase.WaitForJob;
           }

          private void WaitForJob()
          {
             if (!_jobHandle.IsCompleted) return;
              try
              {
                 _jobHandle.Complete();
              }
              catch
              {
                 _jobScheduled = false;
                 DisposeNativeArrays();
                 throw;
              }
              _jobScheduled = false;
              _phase = BakePhase.CopyJobSamples;
           }

           private void CopyJobSample()
           {
              if (_batchSampleIndex >= _batchSampleCount)
              {
                 DisposeBatchNativeArrays();
                 _batchSampleStartIndex = CheckedSum(_batchSampleStartIndex, _batchSampleCount);
                 _batchSampleCount = 0;
                 _batchSampleIndex = 0;
                 _trackOffset = 0;
                 if (_batchSampleStartIndex < _sampleBeats.Count)
                 {
                    _phase = BakePhase.PrepareSampleBatch;
                    return;
                 }

                 DisposeNativeArrays();
                 _phase = BakePhase.BeginJobFinalizing;
                 return;
              }

              var sampleIndex = CheckedSum(_batchSampleStartIndex, _batchSampleIndex);
              var outputIndex = CheckedSum(
                 CheckedProduct(_batchSampleIndex, _trackCount),
                 _trackOffset);
              _bakedTracks[_trackOffset].AddSample(
                 _sampleBeats[sampleIndex],
                 _nativeOutputPositions[outputIndex]);
              _trackOffset++;
              if (_trackOffset >= _trackCount)
              {
                 _trackOffset = 0;
                 _batchSampleIndex++;
              }
           }

          private void BeginJobFinalizing()
          {
             if (_trackOffset >= _trackCount)
             {
                CompleteBake();
                return;
             }

             _track = _bakedTracks[_trackOffset];
             _trackFinalizer = _track.CreateFinalizer(
                _track.CreateSampleSnapshot(),
                _bpmData,
                _startBeat,
                _endBeat,
                _isFinalChunk);
             _phase = BakePhase.Finalizing;
          }

          private void BeginFallbackTrack()
          {
             if (_trackOffset >= _trackCount)
             {
                CompleteBake();
                return;
             }

             _track = _tracks[_firstTrackIndex + _trackOffset].CreateBakedTrack();
             _fallbackSampleIndex = 0;
             _phase = BakePhase.FallbackPrepareSampling;
          }

          private void PrepareFallbackSampling()
          {
             _ancestorMatrixCache ??= new AncestorMatrixCache();
             _matrices ??= [];
             _phase = BakePhase.FallbackSampling;
          }

          private void AdvanceFallbackSampling()
          {
             if (_fallbackSampleIndex >= _sampleBeats.Count)
             {
                _trackFinalizer = _track.CreateFinalizer(
                   _track.CreateSampleSnapshot(),
                   _bpmData,
                   _startBeat,
                   _endBeat,
                   _isFinalChunk);
                _phase = BakePhase.Finalizing;
                return;
             }

             _matrices.Clear();
             var position = GetSampleWorldMatrix(
                   _track.Node,
                   _sampleTimes[_fallbackSampleIndex],
                   _matrices,
                   _ancestorMatrixCache,
                   false)
                .MultiplyPoint3x4(Vector3.zero);
             _track.AddSample(_sampleBeats[_fallbackSampleIndex], position);
             _fallbackSampleIndex++;
          }

          private void AdvanceFinalizing()
          {
             _trackFinalizer.AdvanceOne();
             if (_trackFinalizer.CompletedTrack == null) return;

             var completedTrack = _trackFinalizer.CompletedTrack;
             if (_completedTracks != null) _completedTracks[_trackOffset] = completedTrack;
             _pendingPublication = new MotionPathTrackPublication(
                Key,
                _firstTrackIndex + _trackOffset,
                completedTrack);
             _trackFinalizer = null;
             _track = null;
             _trackOffset++;
             if (_trackOffset >= _trackCount)
             {
                CompleteBake();
                return;
             }
             _phase = _usingMainThreadFallback
                ? BakePhase.FallbackBeginTrack
                : BakePhase.BeginJobFinalizing;
          }

          private void CompleteBake()
          {
             if (_firstTrackIndex == 0 && _trackCount == _sessionTrackCount)
                CompletedChunk = new MotionPathChunk(
                   Key,
                   _startBeat,
                   _endBeat,
                   _songEndBeat,
                   _completedTracks);
             Completed = true;
             _phase = BakePhase.Complete;
          }

          private bool CanActivateMainThreadFallback() =>
             !_usingMainThreadFallback && !_jobScheduled && IsJobSetupPhase(_phase);

          private void ActivateMainThreadFallback(Exception exception)
          {
             JobFallbackException = exception;
             _usingMainThreadFallback = true;
             _session.ReportJobFallback(Key.GeometryGeneration, exception);
             DisposeNativeArrays();
             _bakedTracks.Clear();
             _batchOutputPositionCount = 0;
             _batchSampleCount = 0;
             _batchSampleIndex = 0;
             _batchSampleStartIndex = 0;
             _batchWorldMatrixCount = 0;
             _track = null;
             _trackFinalizer = null;
             _trackOffset = 0;
             _fallbackSampleIndex = 0;
             _phase = BakePhase.FallbackBeginTrack;
          }

          private void DisposeBatchNativeArrays()
          {
             if (_nativeOutputPositions.IsCreated) _nativeOutputPositions.Dispose();
             if (_nativeWorldMatrixScratch.IsCreated) _nativeWorldMatrixScratch.Dispose();
          }

          private void DisposeNativeArrays()
          {
             if (_nativeNodes.IsCreated) _nativeNodes.Dispose();
             if (_nativeRotationBindings.IsCreated) _nativeRotationBindings.Dispose();
             if (_nativeTranslationBindings.IsCreated) _nativeTranslationBindings.Dispose();
             if (_nativeRotationEvents.IsCreated) _nativeRotationEvents.Dispose();
             if (_nativeTranslationEvents.IsCreated) _nativeTranslationEvents.Dispose();
             if (_nativeTrackNodeIndices.IsCreated) _nativeTrackNodeIndices.Dispose();
             if (_nativeSampleTimes.IsCreated) _nativeSampleTimes.Dispose();
             DisposeBatchNativeArrays();
          }

          private void AddRotationEventDescriptor(LightRotationBeatmapEventData eventData)
          {
             EnsureEventCapacity(_rotationEventDescriptors.Count, 1);
             if (_rotationEventIndices.ContainsKey(eventData))
                throw new InvalidOperationException("A rotation event was flattened more than once.");
             _rotationEventIndices.Add(eventData, _rotationEventDescriptors.Count);
             _rotationEventDescriptors.Add(new RotationEventDescriptor(eventData));
          }

          private void AddTranslationEventDescriptor(LightTranslationBeatmapEventData eventData)
          {
             EnsureEventCapacity(_translationEventDescriptors.Count, 1);
             if (_translationEventIndices.ContainsKey(eventData))
                throw new InvalidOperationException("A translation event was flattened more than once.");
             _translationEventIndices.Add(eventData, _translationEventDescriptors.Count);
             _translationEventDescriptors.Add(new TranslationEventDescriptor(eventData));
          }

          private static void EnsureEventCapacity(int startIndex, int count)
          {
             if (count < 0 || startIndex < 0 || count > int.MaxValue - startIndex)
                throw new OverflowException("Motion-path sampling event data exceeds native-array limits.");
          }

          private static int CheckedProduct(int left, int right)
          {
             if (left < 0 || right < 0 || left != 0 && right > int.MaxValue / left)
                throw new OverflowException("Motion-path sampling data exceeds native-array limits.");
             return left * right;
          }

           private static int CheckedSum(int left, int right)
           {
              if (left < 0 || right < 0 || right > int.MaxValue - left)
                 throw new OverflowException("Motion-path sampling data exceeds native-array limits.");
              return left + right;
           }

           private static long CheckedByteSum(long left, long right)
           {
              if (left < 0 || right < 0 || right > long.MaxValue - left)
                 throw new OverflowException("Motion-path sampling payload size exceeds native-array limits.");
              return left + right;
           }

           private static void EnsurePayloadWithinLimit(long payloadBytes, long limitBytes, string payloadName)
           {
              if (payloadBytes > limitBytes)
                 throw new InvalidOperationException(
                    $"Motion-path sampling {payloadName} exceeds its native allocation safety limit.");
           }

           private static long GetPayloadBytes<T>(int elementCount) where T : struct
           {
              if (elementCount < 0)
                 throw new OverflowException("Motion-path sampling element count exceeds native-array limits.");

              var elementSize = UnsafeUtility.SizeOf<T>();
              if (elementSize <= 0 || elementCount > long.MaxValue / elementSize)
                 throw new OverflowException("Motion-path sampling payload size exceeds native-array limits.");
              return (long)elementCount * elementSize;
           }

           private static bool IsJobSetupPhase(BakePhase phase) =>
              phase >= BakePhase.FlattenNodes && phase <= BakePhase.ScheduleJob;

          private static void SetRotationBindingIndex(SamplingNodeDescriptor descriptor, int axis, int index)
          {
             switch (axis)
             {
                case 0:
                   descriptor.RotationXBindingIndex = index;
                   break;
                case 1:
                   descriptor.RotationYBindingIndex = index;
                   break;
                default:
                   descriptor.RotationZBindingIndex = index;
                   break;
             }
          }

          private static void SetTranslationBindingIndex(SamplingNodeDescriptor descriptor, int axis, int index)
          {
             switch (axis)
             {
                case 0:
                   descriptor.TranslationXBindingIndex = index;
                   break;
                case 1:
                   descriptor.TranslationYBindingIndex = index;
                   break;
                default:
                   descriptor.TranslationZBindingIndex = index;
                   break;
             }
          }

          private sealed class SamplingNodeDescriptor
          {
             public bool HasRotation;
             public bool HasTranslation;
             public MotionPathNode Node;
             public int ParentIndex;
             public int RotationXBindingIndex;
             public int RotationYBindingIndex;
             public int RotationZBindingIndex;
             public int TranslationXBindingIndex;
             public int TranslationYBindingIndex;
             public int TranslationZBindingIndex;
          }

          private sealed class RotationBindingDescriptor
          {
             public RotationBindingDescriptor(RotationBinding binding)
             {
                Binding = binding;
             }

             public RotationBinding Binding { get; }
             public int EventCount { get; set; }
             public int EventStartIndex { get; set; }
          }

          private sealed class TranslationBindingDescriptor
          {
             public TranslationBindingDescriptor(TranslationBinding binding)
             {
                Binding = binding;
             }

             public TranslationBinding Binding { get; }
             public int EventCount { get; set; }
             public int EventStartIndex { get; set; }
          }

          private sealed class RotationEventDescriptor
          {
             public RotationEventDescriptor(LightRotationBeatmapEventData eventData)
             {
                Event = eventData;
             }

             public LightRotationBeatmapEventData Event { get; }
          }

          private sealed class TranslationEventDescriptor
          {
             public TranslationEventDescriptor(LightTranslationBeatmapEventData eventData)
             {
                Event = eventData;
             }

             public LightTranslationBeatmapEventData Event { get; }
          }

          private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
          {
             public static readonly ReferenceComparer<T> Instance = new();

             public bool Equals(T left, T right) => ReferenceEquals(left, right);

             public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
          }

          private enum BakePhase
          {
             PrepareEventRange,
             AllocateCompletedTracks,
              BuildSampleBeats,
              BuildSampleTimes,
              FlattenNodes,
              BuildNodeDescriptors,
              BuildTrackNodeIndices,
              BuildRotationBindings,
              BuildTranslationBindings,
              BuildRotationEvents,
              BuildTranslationEvents,
              BuildRotationExternalEvents,
              BuildTranslationExternalEvents,
              ValidateNativeAllocation,
              AllocateNodes,
              AllocateRotationBindings,
              AllocateTranslationBindings,
              AllocateRotationEvents,
              AllocateTranslationEvents,
               AllocateTrackNodeIndices,
               AllocateSampleTimes,
               PopulateNodes,
              PopulateRotationBindings,
              PopulateTranslationBindings,
              PopulateRotationEvents,
              PopulateTranslationEvents,
               PopulateTrackNodeIndices,
               PopulateSampleTimes,
               CreateBakedTracks,
               PrepareSampleBatch,
               AllocateBatchOutputPositions,
               AllocateBatchWorldMatrixScratch,
               ScheduleJob,
              WaitForJob,
              CopyJobSamples,
              BeginJobFinalizing,
              FallbackBeginTrack,
              FallbackPrepareSampling,
              FallbackSampling,
              Finalizing,
              Complete
          }

         private static Matrix4x4 GetSampleWorldMatrix(
            MotionPathNode node,
            float time,
            IDictionary<MotionPathNode, Matrix4x4> matrices,
            AncestorMatrixCache ancestorCache,
            bool cacheOnMiss)
         {
            if (matrices.TryGetValue(node, out var matrix)) return matrix;
            if (ancestorCache.TryGet(node, time, out matrix))
            {
               matrices.Add(node, matrix);
               return matrix;
            }

            matrix = Matrix4x4.TRS(node.GetPosition(time), node.GetRotation(time), node.LocalScale);
            if (node.Parent != null)
               matrix = GetSampleWorldMatrix(node.Parent, time, matrices, ancestorCache, true) * matrix;
            matrices.Add(node, matrix);
            if (cacheOnMiss) ancestorCache.Set(node, time, matrix);
            return matrix;
         }

         private sealed class AncestorMatrixCache
         {
            private const int EntryLimit = 3072;
            private const int SlotCount = 4096;
            private const int SlotMask = SlotCount - 1;

            private readonly int[] _generations = new int[SlotCount];
            private readonly Matrix4x4[] _matrices = new Matrix4x4[SlotCount];
            private readonly MotionPathNode[] _nodes = new MotionPathNode[SlotCount];
            private readonly float[] _times = new float[SlotCount];

            private int _count;
            private int _generation = 1;

            public bool TryGet(MotionPathNode node, float time, out Matrix4x4 matrix)
            {
               var slot = GetInitialSlot(node, time);
               while (_generations[slot] == _generation)
               {
                  if (ReferenceEquals(_nodes[slot], node) && _times[slot].Equals(time))
                  {
                     matrix = _matrices[slot];
                     return true;
                  }
                  slot = (slot + 1) & SlotMask;
               }

               matrix = default;
               return false;
            }

            public void Set(MotionPathNode node, float time, Matrix4x4 matrix)
            {
               if (_count >= EntryLimit) Reset();

               var slot = GetInitialSlot(node, time);
               while (_generations[slot] == _generation)
               {
                  if (ReferenceEquals(_nodes[slot], node) && _times[slot].Equals(time))
                  {
                     _matrices[slot] = matrix;
                     return;
                  }
                  slot = (slot + 1) & SlotMask;
               }

               _nodes[slot] = node;
               _times[slot] = time;
               _matrices[slot] = matrix;
               _generations[slot] = _generation;
               _count++;
            }

            private static int GetInitialSlot(MotionPathNode node, float time)
            {
               unchecked
               {
                  return (RuntimeHelpers.GetHashCode(node) * 397 ^ time.GetHashCode()) & SlotMask;
               }
            }

            private void Reset()
            {
               _count = 0;
               if (_generation == int.MaxValue)
               {
                  Array.Clear(_generations, 0, _generations.Length);
                  _generation = 1;
               }
               else
                  _generation++;
            }
         }

         private sealed class SampleBeatCursor
         {
             private readonly float _endBeat;
             private readonly float[] _eventBeats;
             private readonly int _eventEndIndex;
            private readonly bool _includeEndBeat;
            private readonly int _samplesPerBeat;
            private readonly float _startBeat;

            private bool _emittedEndBeat;
            private bool _emittedStartBeat;
            private int _eventIndex;
            private bool _hasLastBeat;
            private float _lastBeat;
            private long _nextGridTick;

             public SampleBeatCursor(
                float startBeat,
                float endBeat,
                bool includeEndBeat,
                float[] eventBeats,
                int eventStartIndex,
                int eventEndIndex,
                int samplesPerBeat,
                long firstGridTick)
            {
               _startBeat = startBeat;
               _endBeat = endBeat;
               _includeEndBeat = includeEndBeat;
                _eventBeats = eventBeats;
                _samplesPerBeat = samplesPerBeat;
                _nextGridTick = firstGridTick;
                _eventIndex = eventStartIndex;
                _eventEndIndex = eventEndIndex;
            }

            public bool TryMoveNext(out float beat)
            {
               while (true)
               {
                  var hasGridBeat = TryGetGridBeat(out var gridBeat);
                  var hasEventBeat = _eventIndex < _eventEndIndex;
                  var eventBeat = hasEventBeat ? _eventBeats[_eventIndex] : 0f;
                  var hasBoundaryBeat = !_emittedStartBeat || _includeEndBeat && !_emittedEndBeat;
                  var boundaryBeat = !_emittedStartBeat ? _startBeat : _endBeat;
                  if (!hasGridBeat && !hasEventBeat && !hasBoundaryBeat)
                  {
                     beat = 0f;
                     return false;
                  }

                  beat = float.PositiveInfinity;
                  if (hasGridBeat && gridBeat < beat) beat = gridBeat;
                  if (hasEventBeat && eventBeat < beat) beat = eventBeat;
                  if (hasBoundaryBeat && boundaryBeat < beat) beat = boundaryBeat;
                  if (hasGridBeat && gridBeat.Equals(beat)) _nextGridTick++;
                  if (hasEventBeat && eventBeat.Equals(beat)) _eventIndex++;
                  if (hasBoundaryBeat && boundaryBeat.Equals(beat))
                  {
                     if (!_emittedStartBeat) _emittedStartBeat = true;
                     else _emittedEndBeat = true;
                  }
                  if (_hasLastBeat && beat.Equals(_lastBeat)) continue;
                  _lastBeat = beat;
                  _hasLastBeat = true;
                  return true;
               }
            }

            private bool TryGetGridBeat(out float beat)
            {
               beat = (float)_nextGridTick / _samplesPerBeat;
               return beat < _endBeat || _includeEndBeat && beat <= _endBeat;
            }
         }
      }

       private sealed class BakeSession : IBakeSession
       {
           private readonly BpmData _bpmData;
           private readonly HashSet<long> _reportedJobFallbackGenerations = [];
           private readonly BakeSampleData _sampleData;
          private readonly float _songEndBeat;
          private readonly IReadOnlyList<PreparedTrack> _tracks;

         public BakeSession(
            IReadOnlyList<PreparedTrack> tracks,
            float[] eventBeats,
            BpmData bpmData,
            float songEndBeat)
         {
            _tracks = tracks.ToArray();
            _bpmData = bpmData;
            _songEndBeat = songEndBeat;
            _sampleData = new BakeSampleData(eventBeats);
            FocusedTrackCount = _tracks.Count(track => track.IsFocused);
         }

         public int FocusedTrackCount { get; }
         public float SongEndBeat => _songEndBeat;
         public int TrackCount => _tracks.Count;

         public IBakeOperation CreateOperation(
            MotionPathChunkKey key,
            int firstTrackIndex,
            int trackCount)
         {
            if (key.ChunkIndex < 0) throw new ArgumentOutOfRangeException(nameof(key));
            if (key.SamplesPerBeat <= 0) throw new ArgumentOutOfRangeException(nameof(key));
            if (firstTrackIndex < 0 || firstTrackIndex > TrackCount)
               throw new ArgumentOutOfRangeException(nameof(firstTrackIndex));
            if (trackCount < 0 || trackCount > TrackCount - firstTrackIndex)
               throw new ArgumentOutOfRangeException(nameof(trackCount));

             var startBeat = (float)((long)key.ChunkIndex * MotionPathChunk.SizeInBeats);
             if (startBeat >= _songEndBeat) throw new ArgumentOutOfRangeException(nameof(key));
             var endBeat = Mathf.Min(startBeat + MotionPathChunk.SizeInBeats, _songEndBeat);

              return new BakeOperation(
                 this,
                 key,
                 _tracks,
                firstTrackIndex,
                trackCount,
                TrackCount,
                _sampleData,
                _bpmData,
                startBeat,
                endBeat,
                _songEndBeat);
          }

          public void ReportJobFallback(long geometryGeneration, Exception exception)
          {
             if (!_reportedJobFallbackGenerations.Add(geometryGeneration)) return;
             Plugin.Log.Error(
                $"Motion-path sampling job setup failed for geometry generation {geometryGeneration}; "
                 + $"using the main-thread sampler: {exception}");
          }

          public bool IsJobFallbackActive(long geometryGeneration) =>
             _reportedJobFallbackGenerations.Contains(geometryGeneration);
       }

       private sealed class BakeSampleData
       {
           public BakeSampleData(float[] eventBeats)
           {
              EventBeats = eventBeats;
           }

          public float[] EventBeats { get; }

           public void GetEventBeatRange(
              float startBeat,
              float endBeat,
              bool includeEndBeat,
              out int startIndex,
              out int endIndex)
           {
              startIndex = FindFirstIndex(startBeat, false);
               endIndex = FindFirstIndex(endBeat, includeEndBeat);
           }

         private int FindFirstIndex(float beat, bool afterEqual)
         {
            var low = 0;
            var high = EventBeats.Length;
            while (low < high)
            {
               var middle = low + (high - low) / 2;
               if (EventBeats[middle] < beat || afterEqual && EventBeats[middle].Equals(beat))
                  low = middle + 1;
               else
                  high = middle;
            }

            return low;
         }
     }

    private sealed class MotionPathNode
   {
      public MotionPathNode(
         Transform source,
         MotionPathNode parent,
         Vector3 localPosition,
         Quaternion localRotation,
         Vector3 localScale,
         TransformAnimation animation)
      {
         Source = source;
         Parent = parent;
         LocalPosition = localPosition;
         LocalRotation = localRotation;
         LocalScale = localScale;
         Animation = animation;
      }

      public TransformAnimation Animation { get; }
      public Transform Source { get; }
      public Vector3 LocalPosition { get; }
      public Quaternion LocalRotation { get; }
      public Vector3 LocalScale { get; }
      public MotionPathNode Parent { get; }

      public Vector3 GetPosition(float time) => Animation?.GetPosition(LocalPosition, time) ?? LocalPosition;
      public Quaternion GetRotation(float time) => Animation?.GetRotation(LocalRotation, time) ?? LocalRotation;
   }

    private sealed class TransformAnimation
    {
       private readonly List<RotationBinding>[] _rotations = [[], [], []];
       private readonly List<TranslationBinding>[] _translations = [[], [], []];
       private bool _hasRotation;
       private bool _hasTranslation;

       public bool HasRotation => _hasRotation;
       public bool HasTranslation => _hasTranslation;

      public Vector3 GetPosition(Vector3 current, float time)
       {
          if (_hasTranslation) current = Vector3.zero;
          for (var axis = 0; axis < _translations.Length; axis++)
          {
             var bindings = _translations[axis];
             if (bindings.Count > 0) current[axis] = bindings[bindings.Count - 1].Evaluate(time);
          }
          return current;
       }

      public Quaternion GetRotation(Quaternion current, float time)
      {
          if (!_hasRotation) return current;
          var angles = Vector3.zero;
          for (var axis = 0; axis < _rotations.Length; axis++)
          {
             var bindings = _rotations[axis];
             if (bindings.Count > 0) angles[axis] = bindings[bindings.Count - 1].Evaluate(time);
          }
          return Quaternion.AngleAxis(angles.x, Vector3.right)
                 * Quaternion.AngleAxis(angles.y, Vector3.up)
                 * Quaternion.AngleAxis(angles.z, Vector3.forward);
      }

       public void SetRotation(int axis, RotationBinding binding)
       {
          _rotations[axis].Add(binding);
          _hasRotation = true;
       }
        public void SetTranslation(int axis, TranslationBinding binding)
        {
           _translations[axis].Add(binding);
           _hasTranslation = true;
        }

       public RotationBinding GetLastRotationBinding(int axis)
       {
          var bindings = _rotations[axis];
          return bindings.Count == 0 ? null : bindings[bindings.Count - 1];
       }

       public TranslationBinding GetLastTranslationBinding(int axis)
       {
          var bindings = _translations[axis];
          return bindings.Count == 0 ? null : bindings[bindings.Count - 1];
       }

       public IEnumerable<AnimationEvent> GetEvents()
       {
          foreach (var bindings in _rotations)
          foreach (var binding in bindings)
          foreach (var animationEvent in binding.Events)
             yield return animationEvent;

          foreach (var bindings in _translations)
          foreach (var binding in bindings)
          foreach (var animationEvent in binding.Events)
             yield return animationEvent;
       }

       public IEnumerable<float> GetStepTimes()
       {
          foreach (var bindings in _rotations)
          foreach (var binding in bindings)
          foreach (var stepTime in binding.StepTimes)
             yield return stepTime;

          foreach (var bindings in _translations)
          foreach (var binding in bindings)
          foreach (var stepTime in binding.StepTimes)
             yield return stepTime;
       }
    }

    private sealed class RotationBinding
   {
      private readonly LightRotationBeatmapEventData[] _events;
      private readonly AnimationEvent[] _animationEvents;
      private readonly bool _mirrored;

        public RotationBinding(
           LightRotationBeatmapEventData[] events,
           AnimationEvent[] animationEvents,
           bool mirrored)
        {
           _events = events;
           _animationEvents = animationEvents;
           _mirrored = mirrored;
        }

         public IEnumerable<AnimationEvent> Events => _animationEvents;
        public int EventCount => _events.Length;
        public bool Mirrored => _mirrored;
        public LightRotationBeatmapEventData GetEvent(int index) => _events[index];
        public IEnumerable<float> StepTimes => _events
          .Where(item => item.easeType == EaseType.None)
          .Select(item => item.time);

      public float Evaluate(float time)
      {
         var current = GetCurrentEvent(_events, time);
         if (current == null) return 0f;
         var value = Mathf.Repeat(current.rotation, 360f);
         var next = current.nextSameTypeEventData;
         if (next != null && next.easeType != EaseType.None && next.time > current.time)
         {
            var target = LightRotationEventHandler.ComputeTargetAngle(
               value,
               Mathf.Repeat(next.rotation, 360f),
               next.loopCount,
               next.rotationDirection);
            value = Mathf.LerpUnclamped(
               value,
               target,
               Interpolation.Interpolate(Mathf.InverseLerp(current.time, next.time, time), next.easeType));
         }
         return _mirrored ? -value : value;
      }
   }

    private sealed class TranslationBinding
   {
      private readonly Vector2 _distributionLimits;
      private readonly LightTranslationBeatmapEventData[] _events;
      private readonly AnimationEvent[] _animationEvents;
      private readonly bool _mirrored;
      private readonly Vector2 _translationLimits;

        public TranslationBinding(
           LightTranslationBeatmapEventData[] events,
           AnimationEvent[] animationEvents,
           bool mirrored,
          Vector2 translationLimits,
          Vector2 distributionLimits)
        {
           _events = events;
           _animationEvents = animationEvents;
          _mirrored = mirrored;
         _translationLimits = translationLimits;
         _distributionLimits = distributionLimits;
      }

         public IEnumerable<AnimationEvent> Events => _animationEvents;
        public Vector2 DistributionLimits => _distributionLimits;
        public int EventCount => _events.Length;
        public bool Mirrored => _mirrored;
        public Vector2 TranslationLimits => _translationLimits;
        public LightTranslationBeatmapEventData GetEvent(int index) => _events[index];
        public IEnumerable<float> StepTimes => _events
          .Where(item => item.easeType == EaseType.None)
          .Select(item => item.time);

      public float Evaluate(float time)
      {
         var current = GetCurrentEvent(_events, time);
         if (current == null) return 0f;
         var value = Evaluate(current);
         var next = current.nextSameTypeEventData;
         if (next != null && next.easeType != EaseType.None && next.time > current.time)
            value = Mathf.LerpUnclamped(
               value,
               Evaluate(next),
               Interpolation.Interpolate(Mathf.InverseLerp(current.time, next.time, time), next.easeType));
         return value;
    }

      private float Evaluate(LightTranslationBeatmapEventData data)
      {
         return LightTranslationEventHandler.ComputeTranslation(
            data.translation,
            _translationLimits,
            data.distribution,
            _distributionLimits,
            _mirrored);
      }
   }

    private sealed class AnimationEvent
    {
       private float _beat;
       private bool _hasBeat;

      public AnimationEvent(
         float time,
         IReadOnlyList<MotionPathEventSourceResolver.AuthoredMotionPathEventSource> sources)
      {
         Time = time;
         Sources = sources;
      }

       public IReadOnlyList<MotionPathEventSourceResolver.AuthoredMotionPathEventSource> Sources { get; }
       public float Time { get; }

       public float GetBeat(BpmData bpmData)
       {
          if (_hasBeat) return _beat;
          _beat = bpmData.SecondsToBeat(Time);
          _hasBeat = true;
          return _beat;
       }
   }

   private static T GetCurrentEvent<T>(IReadOnlyList<T> events, float time) where T : BeatmapEventData
   {
      var low = 0;
      var high = events.Count;
      while (low < high)
      {
         var middle = low + (high - low) / 2;
         if (events[middle].time <= time) low = middle + 1;
         else high = middle;
      }
      return low == 0 ? null : events[low - 1];
   }

     private sealed class PreparedTrack
    {
       private readonly bool _isFocused;
       private readonly MotionPathNode _node;

        public PreparedTrack(MotionPathNode node, bool isFocused)
        {
           _node = node;
           _isFocused = isFocused;
        }

         public bool IsFocused => _isFocused;
         public MotionPathNode Node => _node;
         public BakedTrack CreateBakedTrack() => new(_node, _isFocused);

       public IEnumerable<AnimationEvent> GetEvents()
       {
          for (var node = _node; node != null; node = node.Parent)
             if (node.Animation != null)
                foreach (var animationEvent in node.Animation.GetEvents())
                   yield return animationEvent;
       }
    }

     private sealed class BakedTrack
    {
       private readonly List<MotionPathSample> _samples = [];

      public BakedTrack(MotionPathNode node, bool isFocused)
      {
         Node = node;
         IsFocused = isFocused;
      }

      public bool IsFocused { get; }
      public MotionPathNode Node { get; }

       public void AddSample(float beat, Vector3 position) =>
          _samples.Add(new MotionPathSample(beat, position));

       public ReadOnlyCollection<MotionPathSample> CreateSampleSnapshot() =>
          Array.AsReadOnly(_samples.ToArray());

         public TrackFinalizer CreateFinalizer(
             ReadOnlyCollection<MotionPathSample> samples,
             BpmData bpmData,
             float startBeat,
             float endBeat,
             bool includeEndBeat) => new(this, samples, bpmData, startBeat, endBeat, includeEndBeat);

        private IEnumerable<float> GetAnimationTimes(Func<TransformAnimation, IEnumerable<float>> getTimes)
       {
          for (var node = Node; node != null; node = node.Parent)
             if (node.Animation != null)
                foreach (var time in getTimes(node.Animation))
                   yield return time;
        }

        private IEnumerable<(AnimationEvent Event, MotionPathNode Node)> GetAnimationEvents()
        {
           for (var node = Node; node != null; node = node.Parent)
              if (node.Animation != null)
                 foreach (var animationEvent in node.Animation.GetEvents())
                    yield return (animationEvent, node);
        }

         private static MotionPathEventSource EvaluateSource(
           MotionPathEventSourceResolver.AuthoredMotionPathEventSource source,
           MotionPathNode node,
           float time,
           IDictionary<MotionPathNode, Matrix4x4> matrices)
        {
           var parentMatrix = node.Parent == null
              ? Matrix4x4.identity
              : GetWorldMatrix(node.Parent, time, matrices);
           return new MotionPathEventSource(
              source.EventBoxGroupId,
              source.EventBoxId,
              source.BaseEventId,
              source.GroupType,
              source.ValueType,
              source.Axis,
              source.ElementId,
              source.DistributionOrder,
              source.Value,
              source.FlipSign,
              source.Mirrored,
              source.RuntimeDistribution,
              source.TranslationLimits,
               source.DistributionLimits,
               node.GetPosition(time),
               parentMatrix,
               source.IsInvertible,
               source,
                source.SortOrder);
         }

          internal sealed class TrackFinalizer
          {
             private const int FocusedVisualSamplesPerBeat = 32;
             private const int UnfocusedVisualSamplesPerBeat = 16;
             private const float VisualSampleBeatTolerance = 0.0001f;

              private readonly BpmData _bpmData;
              private readonly float _endBeat;
              private readonly SortedDictionary<float, List<EventGroup>> _eventGroupsByBeat = [];
              private readonly Dictionary<float, EventGroup> _eventGroupsByTime = [];
              private readonly List<MotionPathEventPoint> _eventPoints = [];
              private readonly bool _includeEndBeat;
               private readonly float _startBeat;
              private readonly ReadOnlyCollection<MotionPathSample> _samples;
             private readonly SortedSet<float> _stepBeatSet = [];
             private readonly List<float> _stepBeats = [];
             private readonly BakedTrack _track;
             private readonly List<int> _visualSampleIndices = [];

            private HashSet<MotionPathEventSourceResolver.AuthoredMotionPathEventSource> _addedSources;
            private IEnumerator<(AnimationEvent Event, MotionPathNode Node)> _eventEnumerator;
            private IEnumerator<KeyValuePair<float, List<EventGroup>>> _eventGroupBeatEnumerator;
            private List<EventGroup> _eventGroupsAtCurrentBeat;
            private int _eventGroupIndexAtCurrentBeat;
            private EventGroup _currentEventGroup;
            private int _currentEventItemIndex;
            private int _currentEventSourceIndex;
            private Dictionary<MotionPathNode, Matrix4x4> _eventMatrices;
             private Vector3 _eventPointPosition;
             private List<MotionPathEventSource> _eventSources;
             private IEnumerator<float> _stepTimeEnumerator;
             private IEnumerator<float> _stepBeatEnumerator;
             private FinalizationPhase _phase;
             private int _visualEventPointIndex;
             private float _nextRegularVisualBeat;
             private int _visualSampleIndex;
             private int _visualStepBeatIndex;

            public TrackFinalizer(
               BakedTrack track,
                ReadOnlyCollection<MotionPathSample> samples,
                BpmData bpmData,
                float startBeat,
                float endBeat,
                bool includeEndBeat)
            {
               _track = track;
               _samples = samples;
               _bpmData = bpmData;
               _startBeat = startBeat;
               _endBeat = endBeat;
               _includeEndBeat = includeEndBeat;
               _eventEnumerator = track.GetAnimationEvents().GetEnumerator();
           }

           public MotionPathTrack CompletedTrack { get; private set; }

           public void AdvanceOne()
           {
              switch (_phase)
              {
                  case FinalizationPhase.CollectEvents:
                     CollectEvent();
                     break;
                  case FinalizationPhase.BeginEventGroup:
                     BeginEventGroup();
                     break;
                  case FinalizationPhase.CollectEventPointSource:
                     CollectEventPointSource();
                     break;
                  case FinalizationPhase.FinishEventPoint:
                     FinishEventPoint();
                     break;
                  case FinalizationPhase.CollectStepBeats:
                     CollectStepBeat();
                     break;
                   case FinalizationPhase.CopyStepBeats:
                      CopyStepBeat();
                      break;
                   case FinalizationPhase.CollectVisualSampleIndices:
                      CollectVisualSampleIndex();
                      break;
                   case FinalizationPhase.CreateTrack:
                      CreateTrack();
                      break;
              }
           }

           private void CollectEvent()
           {
               if (_eventEnumerator.MoveNext())
               {
                  var item = _eventEnumerator.Current;
                   var beat = item.Event.GetBeat(_bpmData);
                   if (float.IsNaN(beat) || float.IsInfinity(beat) || !OwnsBeat(beat))
                     return;

                  if (!_eventGroupsByTime.TryGetValue(item.Event.Time, out var group))
                  {
                     group = new EventGroup(item.Event.Time, beat);
                     _eventGroupsByTime.Add(item.Event.Time, group);
                     if (!_eventGroupsByBeat.TryGetValue(beat, out var groupsAtBeat))
                     {
                        groupsAtBeat = [];
                        _eventGroupsByBeat.Add(beat, groupsAtBeat);
                     }

                     groupsAtBeat.Add(group);
                  }
                  group.Items.Add(item);
                  return;
              }

               _eventEnumerator.Dispose();
               _eventEnumerator = null;
               _eventGroupBeatEnumerator = _eventGroupsByBeat.GetEnumerator();
               _phase = FinalizationPhase.BeginEventGroup;
            }

            private void BeginEventGroup()
            {
               if (_eventGroupsAtCurrentBeat != null
                   && _eventGroupIndexAtCurrentBeat < _eventGroupsAtCurrentBeat.Count)
               {
                  _currentEventGroup = _eventGroupsAtCurrentBeat[_eventGroupIndexAtCurrentBeat++];
                  _eventMatrices = new Dictionary<MotionPathNode, Matrix4x4>();
                  _eventPointPosition = GetWorldMatrix(_track.Node, _currentEventGroup.Time, _eventMatrices)
                     .MultiplyPoint3x4(Vector3.zero);
                  _addedSources = [];
                  _eventSources = [];
                  _currentEventItemIndex = 0;
                  _currentEventSourceIndex = 0;
                  _phase = FinalizationPhase.CollectEventPointSource;
                  return;
               }

               if (_eventGroupBeatEnumerator.MoveNext())
               {
                  _eventGroupsAtCurrentBeat = _eventGroupBeatEnumerator.Current.Value;
                  _eventGroupIndexAtCurrentBeat = 0;
                  return;
               }

               _eventGroupBeatEnumerator.Dispose();
               _eventGroupBeatEnumerator = null;
               _stepTimeEnumerator = _track
                  .GetAnimationTimes(animation => animation.GetStepTimes())
                  .GetEnumerator();
               _phase = FinalizationPhase.CollectStepBeats;
            }

            private void CollectEventPointSource()
            {
               if (_currentEventItemIndex >= _currentEventGroup.Items.Count)
               {
                  _phase = FinalizationPhase.FinishEventPoint;
                  return;
               }

               var item = _currentEventGroup.Items[_currentEventItemIndex];
               if (_currentEventSourceIndex >= item.Event.Sources.Count)
               {
                  _currentEventItemIndex++;
                  _currentEventSourceIndex = 0;
                  return;
               }

               var source = item.Event.Sources[_currentEventSourceIndex++];
               if (_addedSources.Add(source))
                  _eventSources.Add(EvaluateSource(source, item.Node, _currentEventGroup.Time, _eventMatrices));
            }

            private void FinishEventPoint()
            {
                _eventPoints.Add(new MotionPathEventPoint(
                   _currentEventGroup.Beat,
                   _eventPointPosition,
                   _eventSources));
               _addedSources = null;
               _currentEventGroup = null;
               _eventMatrices = null;
               _eventSources = null;
               _phase = FinalizationPhase.BeginEventGroup;
            }

           private void CollectStepBeat()
           {
              if (_stepTimeEnumerator.MoveNext())
              {
                 var beat = _bpmData.SecondsToBeat(_stepTimeEnumerator.Current);
                  if (OwnsBeat(beat)) _stepBeatSet.Add(beat);
                 return;
              }

               _stepTimeEnumerator.Dispose();
               _stepTimeEnumerator = null;
               _stepBeatEnumerator = _stepBeatSet.GetEnumerator();
               _phase = FinalizationPhase.CopyStepBeats;
            }

            private void CopyStepBeat()
            {
               if (_stepBeatEnumerator.MoveNext())
               {
                  _stepBeats.Add(_stepBeatEnumerator.Current);
                  return;
               }

                _stepBeatEnumerator.Dispose();
                _stepBeatEnumerator = null;
                 _nextRegularVisualBeat = _samples.Count > 0 ? _samples[0].Beat : 0f;
                _phase = FinalizationPhase.CollectVisualSampleIndices;
             }

             private void CollectVisualSampleIndex()
             {
                 if (_visualSampleIndex >= _samples.Count)
                {
                   _phase = FinalizationPhase.CreateTrack;
                   return;
                }

                var sampleIndex = _visualSampleIndex++;
                 var sampleBeat = _samples[sampleIndex].Beat;
                var preservesEventBeat = ContainsBeat(_eventPoints, ref _visualEventPointIndex, sampleBeat);
                var preservesStepBeat = ContainsBeat(_stepBeats, ref _visualStepBeatIndex, sampleBeat);
                if (sampleIndex != 0
                     && sampleIndex != _samples.Count - 1
                    && !preservesEventBeat
                    && !preservesStepBeat
                    && sampleBeat + VisualSampleBeatTolerance < _nextRegularVisualBeat)
                   return;

                _visualSampleIndices.Add(sampleIndex);
                var sampleInterval = 1f / (_track.IsFocused
                   ? FocusedVisualSamplesPerBeat
                   : UnfocusedVisualSamplesPerBeat);
                while (_nextRegularVisualBeat <= sampleBeat + VisualSampleBeatTolerance)
                   _nextRegularVisualBeat += sampleInterval;
             }

              private void CreateTrack()
            {
               CompletedTrack = MotionPathTrack.CreateWithOwnedSampleSnapshot(
                  _track.Node.Source,
                   _track.IsFocused,
                   _samples,
                   _eventPoints,
                   _stepBeats,
                   _visualSampleIndices.ToArray());
              }

              private bool OwnsBeat(float beat) =>
                 beat >= _startBeat && (beat < _endBeat || _includeEndBeat && beat <= _endBeat);

              private static bool ContainsBeat(
                IReadOnlyList<MotionPathEventPoint> points,
                ref int pointIndex,
                float beat)
             {
                while (pointIndex < points.Count
                       && points[pointIndex].Beat < beat - VisualSampleBeatTolerance)
                   pointIndex++;
                return pointIndex < points.Count
                       && Mathf.Abs(points[pointIndex].Beat - beat) <= VisualSampleBeatTolerance;
             }

             private static bool ContainsBeat(IReadOnlyList<float> beats, ref int beatIndex, float beat)
             {
                while (beatIndex < beats.Count && beats[beatIndex] < beat - VisualSampleBeatTolerance)
                   beatIndex++;
                return beatIndex < beats.Count && Mathf.Abs(beats[beatIndex] - beat) <= VisualSampleBeatTolerance;
             }

            private sealed class EventGroup
            {
               public EventGroup(float time, float beat)
               {
                  Time = time;
                  Beat = beat;
               }

               public float Beat { get; }
               public List<(AnimationEvent Event, MotionPathNode Node)> Items { get; } = [];
               public float Time { get; }
            }

           private enum FinalizationPhase
            {
               CollectEvents,
               BeginEventGroup,
               CollectEventPointSource,
               FinishEventPoint,
                CollectStepBeats,
                CopyStepBeats,
                CollectVisualSampleIndices,
                CreateTrack
            }
        }
     }
}
