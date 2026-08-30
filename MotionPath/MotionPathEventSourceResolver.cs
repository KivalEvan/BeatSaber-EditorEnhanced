using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapDataLoaderVersion4;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.SerializedData;
using EditorEnhanced.Utils;
using Tweening;
using UnityEngine;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.MotionPath;

internal sealed class MotionPathEventSourceResolver
{
   private const float MatchTolerance = 0.0001f;
   private const bool LivePreviewUsesRotationDirection = true;

   private readonly AudioDataModel _audioDataModel;
   private readonly BeatmapEventBoxGroupsDataModel _dataModel;

   public MotionPathEventSourceResolver(
      BeatmapEventBoxGroupsDataModel dataModel,
      AudioDataModel audioDataModel)
   {
      _dataModel = dataModel;
      _audioDataModel = audioDataModel;
   }

   public MotionPathEventSourceLookupPreparation CreateLookup(IReadOnlyCollection<int> groupIds) =>
      new(this, groupIds);

    private SourceCandidate CreateCandidate(
       EventBoxGroupEditorData group,
       OwnedBoxElement owned,
       BaseEditorData baseEvent,
       int baseEventIndex,
       float maxBeat)
    {
       var beat = group.beat + baseEvent.beat + owned.BeatStep * owned.DurationOrder;
       if (beat > maxBeat) return null;

       switch (owned.EventBox)
       {
          case LightRotationEventBoxEditorData rotationBox
             when baseEvent is LightRotationBaseEditorData rotationEvent:
          {
             var distribution = GetDistribution(
                rotationBox.rotationDistributionShouldAffectFirstBaseEvent,
                baseEventIndex == 0,
                owned.DistributionOrder,
                rotationBox.rotationDistributionParam,
                rotationBox.rotationDistributionParamType,
                rotationBox.rotationDistributionEaseType,
                owned.IndexFilter);
             var distributionLoops = Mathf.FloorToInt(Mathf.Abs(distribution) / 360f);
             var distributionRemainder = Mathf.Abs(distribution) % 360f * Mathf.Sign(distribution);
             var flipSign = rotationBox.flipRotation && LivePreviewUsesRotationDirection ? -1f : 1f;
             return SourceCandidate.Rotation(
                group,
                rotationBox,
                rotationEvent,
                owned,
                beat,
                (rotationEvent.rotation + distributionRemainder) * flipSign,
                rotationEvent.loopsCount + distributionLoops,
                flipSign);
          }
          case LightTranslationEventBoxEditorData translationBox
             when baseEvent is LightTranslationBaseEditorData translationEvent:
          {
             var distribution = GetDistribution(
                translationBox.gapDistributionShouldAffectFirstBaseEvent,
                baseEventIndex == 0,
                owned.DistributionOrder,
                translationBox.gapDistributionParam,
                translationBox.gapDistributionParamType,
                translationBox.gapDistributionEaseType,
                owned.IndexFilter);
             var flipSign = translationBox.flipTranslation ? -1f : 1f;
             return SourceCandidate.Translation(
                group,
                translationBox,
                translationEvent,
                owned,
                beat,
                translationEvent.translation * flipSign,
                distribution * flipSign,
                flipSign);
          }
          default:
             return null;
       }
    }

   private static float GetDistribution(
      bool affectsFirst,
      bool isFirst,
      int distributionOrder,
      float distributionParam,
      BeatmapEventDataBox.DistributionParamType distributionType,
      EaseType easeType,
      IndexFilter indexFilter)
   {
      if (!affectsFirst && isFirst) return 0f;
      var count = indexFilter.limitsDistribution ? indexFilter.VisibleCount : indexFilter.Count;
      if (distributionType == BeatmapEventDataBox.DistributionParamType.Wave)
         return distributionParam * Interpolation.Interpolate(
            distributionOrder / (float)Mathf.Max(count - 1, 1),
            easeType);
      return distributionParam
             * Interpolation.Interpolate(distributionOrder / (float)count, easeType)
             * count;
   }

    private static bool TryGetAxisAndValueType(
       EventBoxEditorData eventBox,
       out LightAxis axis,
       out MotionPathEventValueType valueType)
   {
      switch (eventBox)
      {
          case LightRotationEventBoxEditorData rotation:
             axis = rotation.axis;
             valueType = MotionPathEventValueType.Rotation;
             return true;
          case LightTranslationEventBoxEditorData translation:
             axis = translation.axis;
             valueType = MotionPathEventValueType.Translation;
             return true;
          default:
             axis = default;
             valueType = default;
             return false;
      }
   }

    private sealed class OwnedGroup
    {
       public OwnedGroup(EventBoxGroupEditorData group)
       {
          Group = group;
       }

       public List<OwnedBoxElement> Entries { get; } = [];
       public EventBoxGroupEditorData Group { get; }
       public HashSet<(MotionPathEventValueType ValueType, LightAxis Axis, int ElementId)> Keys { get; } = [];
    }

    internal sealed class MotionPathEventSourceLookupPreparation
    {
       private readonly Dictionary<(MotionPathEventValueType valueType, int groupId, int elementId, LightAxis axis), List<SourceCandidate>> _candidatesByKey = [];
       private readonly Dictionary<int, SortedSet<GroupOrderEntry>> _groupsById = [];
       private readonly List<int> _groupIds = [];
       private readonly HashSet<int> _includedGroupIds;
       private readonly SortedSet<CandidateOrderEntry> _orderedCandidates = new(CandidateOrderComparer.Instance);
       private readonly MotionPathEventSourceResolver _resolver;

       private IEnumerator<BaseEditorData> _baseEventEnumerator;
       private int _baseEventDiscoveryOrder;
       private int _candidateDiscoveryOrder;
       private IEnumerator<CandidateOrderEntry> _candidateEnumerator;
       private int _candidateSortOrder;
       private int _currentBaseEventIndex;
       private int _currentOwnedEntryIndex;
       private int _currentOwnedGroupIndex;
       private IReadOnlyList<EventBoxEditorData> _eventBoxes;
       private int _eventBoxIndex;
       private LightAxis _eventBoxAxis;
       private List<BaseEditorData> _eventBoxBaseEvents;
       private float _eventBoxBeatStep;
       private IndexFilter _eventBoxIndexFilter;
       private MotionPathEventValueType _eventBoxValueType;
       private IEnumerator<(int element, int durationOrder, int distributionOrder)> _elementEnumerator;
       private IEnumerator<EventBoxGroupEditorData> _groupEnumerator;
       private int _groupDiscoveryOrder;
       private int _groupIdIndex;
       private int _groupSize;
       private OwnedGroup _ownedGroup;
       private IEnumerator<GroupOrderEntry> _ownedGroupEnumerator;
       private List<OwnedGroup> _ownedGroups;
       private int _nextOwnedEntryIndex;
       private int _nextOwnedGroupIndex;
       private OwnedBoxElement _ownedElement;
       private IEnumerator<BaseEventOrderEntry> _orderedBaseEventEnumerator;
       private SortedSet<BaseEventOrderEntry> _orderedBaseEvents;
       private PreparationPhase _phase;
       private float _ownershipEndBeat;

       public MotionPathEventSourceLookupPreparation(
          MotionPathEventSourceResolver resolver,
          IReadOnlyCollection<int> groupIds)
       {
          _resolver = resolver;
          _includedGroupIds = new HashSet<int>(groupIds);
          if (_includedGroupIds.Count == 0)
             CompletedLookup = new MotionPathEventSourceLookup(resolver._audioDataModel.bpmData, _candidatesByKey);
       }

       public MotionPathEventSourceLookup CompletedLookup { get; private set; }

       public void AdvanceOne()
       {
          if (CompletedLookup != null) return;

          switch (_phase)
          {
             case PreparationPhase.CollectGroups:
                CollectGroup();
                break;
             case PreparationPhase.BeginGroup:
                BeginGroup();
                break;
             case PreparationPhase.BeginOwnedGroup:
                BeginOwnedGroup();
                break;
             case PreparationPhase.BeginEventBox:
                BeginEventBox();
                break;
             case PreparationPhase.CollectBaseEvents:
                CollectBaseEvent();
                break;
             case PreparationPhase.OrderBaseEvents:
                OrderBaseEvent();
                break;
             case PreparationPhase.CollectOwnedElements:
                CollectOwnedElement();
                break;
             case PreparationPhase.BeginOwnership:
                BeginOwnership();
                break;
             case PreparationPhase.FindOwnershipEnd:
                FindOwnershipEnd();
                break;
             case PreparationPhase.ExpandCandidates:
                ExpandCandidate();
                break;
             case PreparationPhase.BeginCandidateIndexing:
                BeginCandidateIndexing();
                break;
             case PreparationPhase.IndexCandidates:
                IndexCandidate();
                break;
          }
       }

       private void CollectGroup()
       {
          _groupEnumerator ??= _resolver._dataModel.GetAllEventBoxGroups().GetEnumerator();
          if (_groupEnumerator.MoveNext())
          {
             var group = _groupEnumerator.Current;
             if (!_includedGroupIds.Contains(group.groupId)
                 || group.type is not (EventBoxGroupType.Rotation or EventBoxGroupType.Translation)) return;

             if (!_groupsById.TryGetValue(group.groupId, out var groups))
             {
                groups = new SortedSet<GroupOrderEntry>(GroupOrderComparer.Instance);
                _groupsById.Add(group.groupId, groups);
                _groupIds.Add(group.groupId);
             }

             groups.Add(new GroupOrderEntry(group, _groupDiscoveryOrder++));
             return;
          }

          _groupEnumerator.Dispose();
          _groupEnumerator = null;
          _phase = PreparationPhase.BeginGroup;
       }

       private void BeginGroup()
       {
          if (_groupIdIndex >= _groupIds.Count)
          {
             _phase = PreparationPhase.BeginCandidateIndexing;
             return;
          }

          var groupId = _groupIds[_groupIdIndex++];
          if (!_resolver._dataModel.TryGetGroupSizeByEventBoxGroupId(groupId, out var groupSize)
              || groupSize <= 0) return;

          _groupSize = groupSize;
          _ownedGroups = [];
          _ownedGroupEnumerator = _groupsById[groupId].GetEnumerator();
          _phase = PreparationPhase.BeginOwnedGroup;
       }

       private void BeginOwnedGroup()
       {
          if (!_ownedGroupEnumerator.MoveNext())
          {
             _ownedGroupEnumerator.Dispose();
             _ownedGroupEnumerator = null;
             _currentOwnedGroupIndex = 0;
             _currentOwnedEntryIndex = 0;
             _phase = PreparationPhase.BeginOwnership;
             return;
          }

          _ownedGroup = new OwnedGroup(_ownedGroupEnumerator.Current.Group);
          _ownedGroups.Add(_ownedGroup);
          _eventBoxes = _resolver._dataModel.GetEventBoxesByEventBoxGroupId(_ownedGroup.Group.id);
          _eventBoxIndex = 0;
          _phase = PreparationPhase.BeginEventBox;
       }

       private void BeginEventBox()
       {
          if (_eventBoxIndex >= _eventBoxes.Count)
          {
             _ownedGroup = null;
             _eventBoxes = null;
             _phase = PreparationPhase.BeginOwnedGroup;
             return;
          }

          var eventBox = _eventBoxes[_eventBoxIndex++];
          if (!TryGetAxisAndValueType(eventBox, out _eventBoxAxis, out _eventBoxValueType)) return;

          _eventBoxIndexFilter = IndexFilterConverter.Convert(
             LightshowSaver.ConvertIndexFilter(eventBox.indexFilter),
             _groupSize);
          if (_eventBoxIndexFilter == null) return;

          _eventBoxBaseEvents = [];
          _orderedBaseEvents = new SortedSet<BaseEventOrderEntry>(BaseEventOrderComparer.Instance);
          _baseEventDiscoveryOrder = 0;
          _baseEventEnumerator = GetBaseEventEnumerator(eventBox);
          _phase = PreparationPhase.CollectBaseEvents;
       }

       private IEnumerator<BaseEditorData> GetBaseEventEnumerator(EventBoxEditorData eventBox)
       {
          return eventBox switch
          {
             LightRotationEventBoxEditorData rotationBox => _resolver._dataModel
                .GetBaseEventsListByEventBoxId<LightRotationBaseEditorData>(rotationBox.id)
                .Cast<BaseEditorData>()
                .GetEnumerator(),
             LightTranslationEventBoxEditorData translationBox => _resolver._dataModel
                .GetBaseEventsListByEventBoxId<LightTranslationBaseEditorData>(translationBox.id)
                .Cast<BaseEditorData>()
                .GetEnumerator(),
             _ => Enumerable.Empty<BaseEditorData>().GetEnumerator()
          };
       }

       private void CollectBaseEvent()
       {
          if (_baseEventEnumerator.MoveNext())
          {
             var baseEvent = _baseEventEnumerator.Current;
             _orderedBaseEvents.Add(new BaseEventOrderEntry(baseEvent, _baseEventDiscoveryOrder++));
             return;
          }

          _baseEventEnumerator.Dispose();
          _baseEventEnumerator = null;
          _orderedBaseEventEnumerator = _orderedBaseEvents.GetEnumerator();
          _phase = PreparationPhase.OrderBaseEvents;
       }

       private void OrderBaseEvent()
       {
          if (_orderedBaseEventEnumerator.MoveNext())
          {
             _eventBoxBaseEvents.Add(_orderedBaseEventEnumerator.Current.BaseEvent);
             return;
          }

          _orderedBaseEventEnumerator.Dispose();
          _orderedBaseEventEnumerator = null;
          _orderedBaseEvents = null;
          if (_eventBoxBaseEvents.Count == 0)
          {
             _eventBoxBaseEvents = null;
             _phase = PreparationPhase.BeginEventBox;
             return;
          }

          var eventBox = _eventBoxes[_eventBoxIndex - 1];
          _eventBoxBeatStep = BeatmapEventDataBox.GetBeatStep(
             _eventBoxIndexFilter,
             eventBox.beatDistributionParamType,
             eventBox.beatDistributionParam,
             _eventBoxBaseEvents[_eventBoxBaseEvents.Count - 1].beat);
          _elementEnumerator = _eventBoxIndexFilter.GetEnumerator();
          _phase = PreparationPhase.CollectOwnedElements;
       }

       private void CollectOwnedElement()
       {
          if (_elementEnumerator.MoveNext())
          {
             var (element, durationOrder, distributionOrder) = _elementEnumerator.Current;
             if (_ownedGroup.Keys.Add((_eventBoxValueType, _eventBoxAxis, element)))
             {
                var eventBox = _eventBoxes[_eventBoxIndex - 1];
                _ownedGroup.Entries.Add(
                   new OwnedBoxElement(
                      eventBox,
                      _eventBoxIndexFilter,
                      _eventBoxBaseEvents,
                      _eventBoxValueType,
                      _eventBoxAxis,
                      element,
                      durationOrder,
                      distributionOrder,
                      _eventBoxBeatStep,
                      _ownedGroup.Group.beat + _eventBoxBeatStep * durationOrder));
             }
             return;
          }

          _elementEnumerator.Dispose();
          _elementEnumerator = null;
          _eventBoxBaseEvents = null;
          _phase = PreparationPhase.BeginEventBox;
       }

       private void BeginOwnership()
       {
          if (_currentOwnedGroupIndex >= _ownedGroups.Count)
          {
             _ownedGroups = null;
             _phase = PreparationPhase.BeginGroup;
             return;
          }

          var entries = _ownedGroups[_currentOwnedGroupIndex].Entries;
          if (_currentOwnedEntryIndex >= entries.Count)
          {
             _currentOwnedGroupIndex++;
             _currentOwnedEntryIndex = 0;
             return;
          }

          _ownedElement = entries[_currentOwnedEntryIndex];
          _nextOwnedGroupIndex = _currentOwnedGroupIndex + 1;
          _nextOwnedEntryIndex = 0;
          _ownershipEndBeat = float.PositiveInfinity;
          _currentBaseEventIndex = 0;
          _phase = PreparationPhase.FindOwnershipEnd;
       }

       private void FindOwnershipEnd()
       {
          if (_nextOwnedGroupIndex >= _ownedGroups.Count)
          {
             _phase = PreparationPhase.ExpandCandidates;
             return;
          }

          var entries = _ownedGroups[_nextOwnedGroupIndex].Entries;
          if (_nextOwnedEntryIndex >= entries.Count)
          {
             _nextOwnedGroupIndex++;
             _nextOwnedEntryIndex = 0;
             return;
          }

          var nextOwnedElement = entries[_nextOwnedEntryIndex++];
          if (nextOwnedElement.ValueType != _ownedElement.ValueType
              || nextOwnedElement.Axis != _ownedElement.Axis
              || nextOwnedElement.ElementId != _ownedElement.ElementId) return;

          _ownershipEndBeat = nextOwnedElement.StartBeat;
          _phase = PreparationPhase.ExpandCandidates;
       }

       private void ExpandCandidate()
       {
          if (_currentBaseEventIndex >= _ownedElement.BaseEvents.Count)
          {
             _currentOwnedEntryIndex++;
             _ownedElement = null;
             _phase = PreparationPhase.BeginOwnership;
             return;
          }

          var baseEvent = _ownedElement.BaseEvents[_currentBaseEventIndex];
          var candidate = _resolver.CreateCandidate(
             _ownedGroups[_currentOwnedGroupIndex].Group,
             _ownedElement,
             baseEvent,
             _currentBaseEventIndex++,
             _ownershipEndBeat);
          if (candidate != null)
             _orderedCandidates.Add(new CandidateOrderEntry(candidate, _candidateDiscoveryOrder++));
       }

       private void BeginCandidateIndexing()
       {
          _candidateEnumerator = _orderedCandidates.GetEnumerator();
          _phase = PreparationPhase.IndexCandidates;
       }

       private void IndexCandidate()
       {
          if (_candidateEnumerator.MoveNext())
          {
             var candidate = _candidateEnumerator.Current.Candidate;
             candidate.SortOrder = _candidateSortOrder++;
             var key = (candidate.ValueType, candidate.GroupId, candidate.ElementId, candidate.Axis);
             if (!_candidatesByKey.TryGetValue(key, out var candidates))
             {
                candidates = [];
                _candidatesByKey.Add(key, candidates);
             }
             candidates.Add(candidate);
             return;
          }

          _candidateEnumerator.Dispose();
          _candidateEnumerator = null;
          CompletedLookup = new MotionPathEventSourceLookup(_resolver._audioDataModel.bpmData, _candidatesByKey);
       }

       private enum PreparationPhase
       {
          CollectGroups,
          BeginGroup,
          BeginOwnedGroup,
          BeginEventBox,
          CollectBaseEvents,
          OrderBaseEvents,
          CollectOwnedElements,
          BeginOwnership,
          FindOwnershipEnd,
          ExpandCandidates,
          BeginCandidateIndexing,
          IndexCandidates
       }

       private sealed class CandidateOrderEntry
       {
          public CandidateOrderEntry(SourceCandidate candidate, int discoveryOrder)
          {
             Candidate = candidate;
             DiscoveryOrder = discoveryOrder;
          }

          public SourceCandidate Candidate { get; }
          public int DiscoveryOrder { get; }
       }

       private sealed class BaseEventOrderEntry
       {
          public BaseEventOrderEntry(BaseEditorData baseEvent, int discoveryOrder)
          {
             BaseEvent = baseEvent;
             DiscoveryOrder = discoveryOrder;
          }

          public BaseEditorData BaseEvent { get; }
          public int DiscoveryOrder { get; }
       }

       private sealed class BaseEventOrderComparer : IComparer<BaseEventOrderEntry>
       {
          public static readonly BaseEventOrderComparer Instance = new();

          public int Compare(BaseEventOrderEntry left, BaseEventOrderEntry right)
          {
             var comparison = left.BaseEvent.beat.CompareTo(right.BaseEvent.beat);
             return comparison != 0 ? comparison : left.DiscoveryOrder.CompareTo(right.DiscoveryOrder);
          }
       }

       private sealed class CandidateOrderComparer : IComparer<CandidateOrderEntry>
       {
          public static readonly CandidateOrderComparer Instance = new();

          public int Compare(CandidateOrderEntry left, CandidateOrderEntry right)
          {
             var comparison = left.Candidate.ValueType.CompareTo(right.Candidate.ValueType);
             if (comparison != 0) return comparison;
             comparison = left.Candidate.GroupId.CompareTo(right.Candidate.GroupId);
             if (comparison != 0) return comparison;
             comparison = left.Candidate.EventBoxGroupId.GetHashCode().CompareTo(right.Candidate.EventBoxGroupId.GetHashCode());
             if (comparison != 0) return comparison;
             comparison = left.Candidate.EventBoxId.GetHashCode().CompareTo(right.Candidate.EventBoxId.GetHashCode());
             if (comparison != 0) return comparison;
             comparison = left.Candidate.BaseEventId.GetHashCode().CompareTo(right.Candidate.BaseEventId.GetHashCode());
             if (comparison != 0) return comparison;
             comparison = left.Candidate.ElementId.CompareTo(right.Candidate.ElementId);
             if (comparison != 0) return comparison;
             comparison = left.Candidate.DistributionOrder.CompareTo(right.Candidate.DistributionOrder);
             if (comparison != 0) return comparison;
             comparison = left.Candidate.Axis.CompareTo(right.Candidate.Axis);
             return comparison != 0 ? comparison : left.DiscoveryOrder.CompareTo(right.DiscoveryOrder);
          }
       }

       private sealed class GroupOrderEntry
       {
          public GroupOrderEntry(EventBoxGroupEditorData group, int discoveryOrder)
          {
             Group = group;
             DiscoveryOrder = discoveryOrder;
          }

          public int DiscoveryOrder { get; }
          public EventBoxGroupEditorData Group { get; }
       }

       private sealed class GroupOrderComparer : IComparer<GroupOrderEntry>
       {
          public static readonly GroupOrderComparer Instance = new();

          public int Compare(GroupOrderEntry left, GroupOrderEntry right)
          {
             var comparison = left.Group.beat.CompareTo(right.Group.beat);
             return comparison != 0 ? comparison : left.DiscoveryOrder.CompareTo(right.DiscoveryOrder);
          }
       }
    }

    internal sealed class OwnedBoxElement
    {
       public OwnedBoxElement(
          EventBoxEditorData eventBox,
          IndexFilter indexFilter,
          IReadOnlyList<BaseEditorData> baseEvents,
          MotionPathEventValueType valueType,
          LightAxis axis,
          int elementId,
          int durationOrder,
          int distributionOrder,
          float beatStep,
          float startBeat)
       {
          EventBox = eventBox;
          IndexFilter = indexFilter;
          BaseEvents = baseEvents;
          ValueType = valueType;
         Axis = axis;
          ElementId = elementId;
          DurationOrder = durationOrder;
          DistributionOrder = distributionOrder;
          BeatStep = beatStep;
          StartBeat = startBeat;
       }

       public LightAxis Axis { get; }
       public IReadOnlyList<BaseEditorData> BaseEvents { get; }
       public float BeatStep { get; }
       public int DistributionOrder { get; }
      public int DurationOrder { get; }
      public int ElementId { get; }
       public EventBoxEditorData EventBox { get; }
       public IndexFilter IndexFilter { get; }
       public float StartBeat { get; }
       public MotionPathEventValueType ValueType { get; }
   }

    internal sealed class MotionPathEventSourceLookup
    {
       private readonly BpmData _bpmData;
       private readonly IReadOnlyDictionary<(MotionPathEventValueType valueType, int groupId, int elementId, LightAxis axis), List<SourceCandidate>> _candidatesByKey;

       public MotionPathEventSourceLookup(
          BpmData bpmData,
          IReadOnlyDictionary<(MotionPathEventValueType valueType, int groupId, int elementId, LightAxis axis), List<SourceCandidate>> candidatesByKey)
       {
          _bpmData = bpmData;
          _candidatesByKey = candidatesByKey;
       }

      public IReadOnlyList<AuthoredMotionPathEventSource> ResolveRotation(
         int groupId,
         int elementId,
         LightAxis axis,
         LightRotationBeatmapEventData runtimeEvent,
         Transform source,
         bool mirrored)
      {
         var candidates = GetCandidates(MotionPathEventValueType.Rotation, groupId, elementId, axis)
            .Where(candidate => candidate.MatchesOwnership(groupId, elementId, axis, runtimeEvent, _bpmData))
            .ToArray();
         var exactCandidates = candidates
            .Where(candidate => candidate.Matches(groupId, elementId, axis, runtimeEvent, _bpmData))
            .ToArray();
         return (exactCandidates.Length == 0 ? candidates : exactCandidates)
             .Select(candidate => candidate.ToSource(source, mirrored, default, default))
             .ToArray();
      }

      public IReadOnlyList<AuthoredMotionPathEventSource> ResolveTranslation(
         int groupId,
         int elementId,
         LightAxis axis,
         LightTranslationBeatmapEventData runtimeEvent,
         Transform source,
         bool mirrored,
         Vector2 translationLimits,
         Vector2 distributionLimits)
      {
         var candidates = GetCandidates(MotionPathEventValueType.Translation, groupId, elementId, axis)
            .Where(candidate => candidate.MatchesOwnership(groupId, elementId, axis, runtimeEvent, _bpmData))
            .ToArray();
         var exactCandidates = candidates
            .Where(candidate => candidate.Matches(groupId, elementId, axis, runtimeEvent, _bpmData))
            .ToArray();
         return (exactCandidates.Length == 0 ? candidates : exactCandidates)
             .Select(candidate => candidate.ToSource(source, mirrored, translationLimits, distributionLimits))
             .ToArray();
      }

      private IReadOnlyList<SourceCandidate> GetCandidates(
         MotionPathEventValueType valueType,
         int groupId,
         int elementId,
         LightAxis axis)
      {
         return _candidatesByKey.TryGetValue((valueType, groupId, elementId, axis), out var candidates)
            ? candidates
            : Array.Empty<SourceCandidate>();
      }
   }

   internal sealed class AuthoredMotionPathEventSource
   {
      public BeatmapEditorObjectId BaseEventId;
      public int DistributionOrder;
      public Vector2 DistributionLimits;
      public int ElementId;
      public BeatmapEditorObjectId EventBoxGroupId;
      public BeatmapEditorObjectId EventBoxId;
      public float FlipSign;
      public EventBoxGroupType GroupType;
      public bool IsInvertible;
      public bool Mirrored;
      public float RuntimeDistribution;
      public int SortOrder;
      public Transform Source;
      public Vector2 TranslationLimits;
      public float Value;
      public MotionPathEventValueType ValueType;
      public LightAxis Axis;
   }

   internal sealed class SourceCandidate
   {
      private EaseType _easeType;
      private int _loopsCount;
      private LightRotationDirection _rotationDirection;
      private float _runtimeValue;
      private bool _usePreviousValue;

      private SourceCandidate()
      {
      }

      public LightAxis Axis { get; private set; }
      public float AuthoredValue { get; private set; }
      public BeatmapEditorObjectId BaseEventId { get; private set; }
      public float Beat { get; private set; }
      public int DistributionOrder { get; private set; }
      public int ElementId { get; private set; }
      public BeatmapEditorObjectId EventBoxGroupId { get; private set; }
      public BeatmapEditorObjectId EventBoxId { get; private set; }
      public float FlipSign { get; private set; }
      public int GroupId { get; private set; }
      public EventBoxGroupType GroupType { get; private set; }
      public float RuntimeDistribution { get; private set; }
      public int SortOrder { get; set; }
      public MotionPathEventValueType ValueType { get; private set; }

      public static SourceCandidate Rotation(
         EventBoxGroupEditorData group,
         LightRotationEventBoxEditorData eventBox,
         LightRotationBaseEditorData baseEvent,
         OwnedBoxElement owned,
         float beat,
         float runtimeValue,
         int loopsCount,
         float flipSign)
      {
         return new SourceCandidate
         {
            EventBoxGroupId = group.id,
            EventBoxId = eventBox.id,
            BaseEventId = baseEvent.id,
            GroupId = group.groupId,
            GroupType = group.type,
            ValueType = MotionPathEventValueType.Rotation,
            Axis = owned.Axis,
            ElementId = owned.ElementId,
            DistributionOrder = owned.DistributionOrder,
            Beat = beat,
            AuthoredValue = baseEvent.rotation,
            FlipSign = flipSign,
            _runtimeValue = runtimeValue,
            _loopsCount = loopsCount,
            _rotationDirection = baseEvent.rotationDirection,
            _usePreviousValue = baseEvent.usePreviousValue,
            _easeType = EaseTypeHelpers.ToEaseType((baseEvent.easeLeadType, baseEvent.easeCurveType))
         };
      }

      public static SourceCandidate Translation(
         EventBoxGroupEditorData group,
         LightTranslationEventBoxEditorData eventBox,
         LightTranslationBaseEditorData baseEvent,
         OwnedBoxElement owned,
         float beat,
         float runtimeValue,
         float runtimeDistribution,
         float flipSign)
      {
         return new SourceCandidate
         {
            EventBoxGroupId = group.id,
            EventBoxId = eventBox.id,
            BaseEventId = baseEvent.id,
            GroupId = group.groupId,
            GroupType = group.type,
            ValueType = MotionPathEventValueType.Translation,
            Axis = owned.Axis,
            ElementId = owned.ElementId,
            DistributionOrder = owned.DistributionOrder,
            Beat = beat,
            AuthoredValue = baseEvent.translation,
            FlipSign = flipSign,
            RuntimeDistribution = runtimeDistribution,
            _runtimeValue = runtimeValue,
            _usePreviousValue = baseEvent.usePreviousValue,
            _easeType = EaseTypeHelpers.ToEaseType((baseEvent.easeLeadType, baseEvent.easeCurveType))
         };
      }

      public bool Matches(
         int groupId,
         int elementId,
         LightAxis axis,
         LightRotationBeatmapEventData runtimeEvent,
         BpmData bpmData)
      {
          return MatchesOwnership(groupId, elementId, axis, runtimeEvent, bpmData)
                 && (_usePreviousValue || Mathf.Abs(runtimeEvent.rotation - _runtimeValue) <= MatchTolerance)
                && runtimeEvent.loopCount == _loopsCount
                && runtimeEvent.rotationDirection == _rotationDirection;
      }

      public bool Matches(
         int groupId,
         int elementId,
         LightAxis axis,
         LightTranslationBeatmapEventData runtimeEvent,
         BpmData bpmData)
      {
          return MatchesOwnership(groupId, elementId, axis, runtimeEvent, bpmData)
                 && (_usePreviousValue
                    || Mathf.Abs(runtimeEvent.translation - _runtimeValue) <= MatchTolerance
                    && Mathf.Abs(runtimeEvent.distribution - RuntimeDistribution) <= MatchTolerance);
      }

      public AuthoredMotionPathEventSource ToSource(
         Transform source,
         bool mirrored,
         Vector2 translationLimits,
         Vector2 distributionLimits)
      {
         return new AuthoredMotionPathEventSource
         {
            EventBoxGroupId = EventBoxGroupId,
            EventBoxId = EventBoxId,
            BaseEventId = BaseEventId,
            GroupType = GroupType,
            ValueType = ValueType,
            Axis = Axis,
            ElementId = ElementId,
            DistributionOrder = DistributionOrder,
            Value = AuthoredValue,
            FlipSign = FlipSign,
            Mirrored = mirrored,
             RuntimeDistribution = RuntimeDistribution,
             SortOrder = SortOrder,
            TranslationLimits = translationLimits,
            DistributionLimits = distributionLimits,
            Source = source,
            IsInvertible = !_usePreviousValue
                           && (ValueType != MotionPathEventValueType.Translation
                               || !Mathf.Approximately(translationLimits.x, translationLimits.y))
          };
      }

      public bool MatchesOwnership(
         int groupId,
         int elementId,
         LightAxis axis,
         LightRotationBeatmapEventData runtimeEvent,
         BpmData bpmData)
      {
         return ValueType == MotionPathEventValueType.Rotation
                && MatchesCommon(groupId, elementId, axis, runtimeEvent.time, runtimeEvent.usePreviousEventValue, runtimeEvent.easeType, bpmData);
      }

      public bool MatchesOwnership(
         int groupId,
         int elementId,
         LightAxis axis,
         LightTranslationBeatmapEventData runtimeEvent,
         BpmData bpmData)
      {
         return ValueType == MotionPathEventValueType.Translation
                && MatchesCommon(groupId, elementId, axis, runtimeEvent.time, runtimeEvent.usePreviousEventValue, runtimeEvent.easeType, bpmData);
      }

      private bool MatchesCommon(
         int groupId,
         int elementId,
         LightAxis axis,
         float time,
         bool usePreviousValue,
         EaseType easeType,
         BpmData bpmData)
      {
         return GroupId == groupId
                && ElementId == elementId
                && Axis == axis
                && _usePreviousValue == usePreviousValue
                && _easeType == easeType
                && Mathf.Abs(bpmData.BeatToSeconds(Beat) - time) <= MatchTolerance;
      }
   }
}
