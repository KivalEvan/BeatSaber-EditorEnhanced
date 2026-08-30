using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo;
using EditorEnhanced.MotionPath.Configuration;
using EditorEnhanced.Utils;
using Tweening;
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
          _config.MotionPath.GetMaximumSelectedTracks(),
          _config.MotionPath.GetBakeBufferInBeats());
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
      private readonly float _bakeBufferInBeats;
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
          int maximumTracks,
          float bakeBufferInBeats)
      {
         _planner = planner;
         _effects = effects;
         _group = group;
         _selectedEventBox = selectedEventBox;
         _maximumTargets = maximumTargets;
         _maximumTracks = maximumTracks;
          _bakeBufferInBeats = bakeBufferInBeats;
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
             bpmData.totalBeats,
             _bakeBufferInBeats);
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
          IBakeOperation CreateOperation(
             float requestedBeat,
             float backwardRangeInBeats,
             float forwardRangeInBeats,
             int samplesPerBeat);
      }

      internal interface IBakeOperation
     {
        MotionPathPlan CompletedPlan { get; }

        bool ContainsVisibleWindow(float currentBeat, float backwardRangeInBeats, float forwardRangeInBeats);
        void Advance(double budgetMilliseconds);
     }

     private sealed class BakeOperation : IBakeOperation
     {
        private readonly BpmData _bpmData;
        private readonly float _endBeat;
        private readonly float[] _eventBeats;
        private readonly int _eventBeatEndIndex;
        private readonly Dictionary<MotionPathNode, Matrix4x4> _matrices = [];
        private readonly MotionPathTrack[] _pathTracks;
        private readonly int _sampleCount;
        private readonly float _songEndBeat;
        private readonly float _startBeat;
        private readonly IReadOnlyList<BakedTrack> _tracks;

        private float _currentBeat;
        private float _currentTime;
        private int _eventBeatIndex;
        private int _finalizeTrackIndex;
        private int _sampleIndex;
        private int _sampleTrackIndex;
        private BakedTrack.TrackFinalizer _trackFinalizer;
        private bool _hasCurrentBeat;
        private bool _hasLastBeat;
        private float _lastBeat;

        public BakeOperation(
           IReadOnlyList<BakedTrack> tracks,
           float[] eventBeats,
           BpmData bpmData,
           float startBeat,
           float endBeat,
           float songEndBeat,
           int samplesPerBeat)
        {
           _tracks = tracks;
           _eventBeats = eventBeats;
           _bpmData = bpmData;
           _startBeat = startBeat;
           _endBeat = endBeat;
           _songEndBeat = songEndBeat;
           _sampleCount = Mathf.Max(1, Mathf.CeilToInt((endBeat - startBeat) * samplesPerBeat));
           _pathTracks = new MotionPathTrack[tracks.Count];
            _eventBeatEndIndex = eventBeats.Length;
        }

        public MotionPathPlan CompletedPlan { get; private set; }

        public bool ContainsVisibleWindow(
           float currentBeat,
           float backwardRangeInBeats,
           float forwardRangeInBeats)
        {
           var visibleStartBeat = Mathf.Max(0f, currentBeat - backwardRangeInBeats);
           var visibleEndBeat = Mathf.Min(_songEndBeat, currentBeat + forwardRangeInBeats);
           return _startBeat <= visibleStartBeat && _endBeat >= visibleEndBeat;
        }

        public void Advance(double budgetMilliseconds)
        {
           if (CompletedPlan != null) return;

           var startedAt = Stopwatch.GetTimestamp();
           var budgetTicks = budgetMilliseconds * Stopwatch.Frequency / 1000d;
           do
           {
              AdvanceOne();
              if (CompletedPlan != null) return;
           }
           while (Stopwatch.GetTimestamp() - startedAt < budgetTicks);
        }

        private void AdvanceOne()
        {
           if (_sampleIndex <= _sampleCount || _eventBeatIndex < _eventBeatEndIndex || _hasCurrentBeat)
           {
              AdvanceSampling();
              return;
           }

           if (_finalizeTrackIndex < _tracks.Count)
           {
              _trackFinalizer ??= _tracks[_finalizeTrackIndex].CreateFinalizer(
                 _bpmData,
                 _startBeat,
                 _endBeat);
              _trackFinalizer.AdvanceOne();
              if (_trackFinalizer.CompletedTrack != null)
              {
                 _pathTracks[_finalizeTrackIndex++] = _trackFinalizer.CompletedTrack;
                 _trackFinalizer = null;
              }
              return;
           }

           CompletedPlan = new MotionPathPlan(_pathTracks, _startBeat, _endBeat, _songEndBeat);
        }

        private void AdvanceSampling()
        {
           if (!_hasCurrentBeat)
           {
              if (!TryMoveToNextBeat()) return;
              _currentTime = _bpmData.BeatToSeconds(_currentBeat);
              _matrices.Clear();
              _sampleTrackIndex = 0;
              _hasCurrentBeat = true;
           }

           var track = _tracks[_sampleTrackIndex++];
           track.AddSample(
              _currentBeat,
              GetWorldMatrix(track.Node, _currentTime, _matrices).MultiplyPoint3x4(Vector3.zero));
           if (_sampleTrackIndex >= _tracks.Count) _hasCurrentBeat = false;
        }

        private bool TryMoveToNextBeat()
        {
           while (_sampleIndex <= _sampleCount || _eventBeatIndex < _eventBeatEndIndex)
           {
              var hasGridBeat = _sampleIndex <= _sampleCount;
              var gridBeat = hasGridBeat
                 ? Mathf.Lerp(_startBeat, _endBeat, (float)_sampleIndex / _sampleCount)
                 : 0f;
              var hasEventBeat = _eventBeatIndex < _eventBeatEndIndex;
              var eventBeat = hasEventBeat ? _eventBeats[_eventBeatIndex] : 0f;

              float nextBeat;
              if (!hasEventBeat || hasGridBeat && gridBeat < eventBeat)
              {
                 nextBeat = gridBeat;
                 _sampleIndex++;
              }
              else if (!hasGridBeat || eventBeat < gridBeat)
              {
                 nextBeat = eventBeat;
                 _eventBeatIndex++;
              }
              else
              {
                 nextBeat = gridBeat;
                 _sampleIndex++;
                 _eventBeatIndex++;
              }

              if (_hasLastBeat && nextBeat.Equals(_lastBeat)) continue;
              _lastBeat = nextBeat;
              _hasLastBeat = true;
              _currentBeat = nextBeat;
              return true;
           }

           return false;
        }

      }

      private sealed class BakeSession : IBakeSession
      {
         private readonly float _bakeBufferInBeats;
         private readonly BpmData _bpmData;
         private readonly BakeSampleData _sampleData;
         private readonly float _songEndBeat;
         private readonly IReadOnlyList<PreparedTrack> _tracks;

          public BakeSession(
             IReadOnlyList<PreparedTrack> tracks,
             float[] eventBeats,
             BpmData bpmData,
             float songEndBeat,
             float bakeBufferInBeats)
         {
            _tracks = tracks.ToArray();
            _bpmData = bpmData;
            _songEndBeat = songEndBeat;
             _bakeBufferInBeats = bakeBufferInBeats;
             _sampleData = new BakeSampleData(eventBeats);
         }

          public IBakeOperation CreateOperation(
             float requestedBeat,
             float backwardRangeInBeats,
             float forwardRangeInBeats,
             int samplesPerBeat)
         {
            var startBeat = Mathf.Clamp(
               requestedBeat - backwardRangeInBeats - _bakeBufferInBeats,
               0f,
               _songEndBeat);
            var endBeat = Mathf.Clamp(
               requestedBeat + forwardRangeInBeats + _bakeBufferInBeats,
               startBeat,
               _songEndBeat);
            var tracks = _tracks.Select(track => track.CreateBakedTrack()).ToArray();
            return new BakeOperation(
               tracks,
               _sampleData.GetEventBeats(startBeat, endBeat),
               _bpmData,
               startBeat,
                endBeat,
                _songEndBeat,
                samplesPerBeat);
         }
      }

       private sealed class BakeSampleData
      {
          public BakeSampleData(float[] eventBeats)
          {
             EventBeats = eventBeats;
          }

         private float[] EventBeats { get; }

         public float[] GetEventBeats(float startBeat, float endBeat)
         {
            var startIndex = FindFirstIndex(startBeat, false);
            var endIndex = FindFirstIndex(endBeat, true);
            var result = new float[endIndex - startIndex];
            Array.Copy(EventBeats, startIndex, result, 0, result.Length);
            return result;
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

         public void AddSample(float beat, Vector3 position) => _samples.Add(new MotionPathSample(beat, position));

        public TrackFinalizer CreateFinalizer(BpmData bpmData, float startBeat, float endBeat) =>
           new(this, bpmData, startBeat, endBeat);

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
             private readonly float _startBeat;
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

           public TrackFinalizer(BakedTrack track, BpmData bpmData, float startBeat, float endBeat)
           {
              _track = track;
              _bpmData = bpmData;
              _startBeat = startBeat;
              _endBeat = endBeat;
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
                  if (beat < _startBeat || beat > _endBeat || float.IsNaN(beat) || float.IsInfinity(beat))
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
                 if (beat >= _startBeat && beat <= _endBeat) _stepBeatSet.Add(beat);
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
                _nextRegularVisualBeat = _track._samples.Count > 0 ? _track._samples[0].Beat : 0f;
                _phase = FinalizationPhase.CollectVisualSampleIndices;
             }

             private void CollectVisualSampleIndex()
             {
                if (_visualSampleIndex >= _track._samples.Count)
                {
                   _phase = FinalizationPhase.CreateTrack;
                   return;
                }

                var sampleIndex = _visualSampleIndex++;
                var sampleBeat = _track._samples[sampleIndex].Beat;
                var preservesEventBeat = ContainsBeat(_eventPoints, ref _visualEventPointIndex, sampleBeat);
                var preservesStepBeat = ContainsBeat(_stepBeats, ref _visualStepBeatIndex, sampleBeat);
                if (sampleIndex != 0
                    && sampleIndex != _track._samples.Count - 1
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
               CompletedTrack = new MotionPathTrack(
                  _track.Node.Source,
                   _track.IsFocused,
                   _track._samples,
                   _eventPoints,
                   _stepBeats,
                   _visualSampleIndices.ToArray());
             }

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
