using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using UnityEngine;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.MotionPath;

internal sealed class MotionPathPlan
{
   public MotionPathPlan(IReadOnlyList<MotionPathTrack> tracks, float bakedStartBeat, float bakedEndBeat, float songEndBeat)
   {
      Tracks = tracks;
      BakedStartBeat = bakedStartBeat;
      BakedEndBeat = bakedEndBeat;
      SongEndBeat = songEndBeat;
   }

   public float BakedStartBeat { get; }
   public float BakedEndBeat { get; }
   public float SongEndBeat { get; }
   public IReadOnlyList<MotionPathTrack> Tracks { get; }

   public bool ContainsVisibleWindow(float currentBeat, float backwardRangeInBeats, float forwardRangeInBeats)
   {
      var visibleStartBeat = Mathf.Max(0f, currentBeat - backwardRangeInBeats);
      var visibleEndBeat = Mathf.Min(SongEndBeat, currentBeat + forwardRangeInBeats);
      return BakedStartBeat <= visibleStartBeat && BakedEndBeat >= visibleEndBeat;
   }
}

internal sealed class MotionPathTrack
{
   public MotionPathTrack(
      Transform source,
       bool isFocused,
       IReadOnlyList<MotionPathSample> samples,
       IReadOnlyList<MotionPathEventPoint> eventPoints,
       IReadOnlyList<float> stepBeats,
       int[] visualSampleIndices)
    {
       Source = source;
       IsFocused = isFocused;
       Samples = samples;
       EventPoints = eventPoints;
       StepBeats = stepBeats;
       VisualSampleIndices = Array.AsReadOnly(visualSampleIndices);
   }

   public IReadOnlyList<MotionPathEventPoint> EventPoints { get; }
   public Transform Source { get; }
    public bool IsFocused { get; }
    public IReadOnlyList<MotionPathSample> Samples { get; }
    public IReadOnlyList<float> StepBeats { get; }
    public IReadOnlyList<int> VisualSampleIndices { get; }
}

internal readonly struct MotionPathEventPoint
{
   public MotionPathEventPoint(
      float beat,
      Vector3 position,
      IReadOnlyList<MotionPathEventSource> sources)
   {
      Beat = beat;
      Position = position;
      Sources = sources;
   }

   public float Beat { get; }
   public Vector3 Position { get; }
   public IReadOnlyList<MotionPathEventSource> Sources { get; }
}

internal enum MotionPathEventValueType
{
   Rotation,
   Translation
}

internal sealed class MotionPathEventSource
{
   public MotionPathEventSource(
      BeatmapEditorObjectId eventBoxGroupId,
      BeatmapEditorObjectId eventBoxId,
      BeatmapEditorObjectId baseEventId,
      EventBoxGroupType groupType,
      MotionPathEventValueType valueType,
      LightAxis axis,
      int elementId,
      int distributionOrder,
      float authoredValue,
      float flipSign,
      bool mirrored,
      float runtimeDistribution,
      Vector2 translationLimits,
      Vector2 distributionLimits,
      Vector3 sourceLocalPosition,
      Matrix4x4 parentWorldMatrix,
      bool isInvertible,
      object runtimeSourceIdentity,
      int sourceSortOrder)
   {
      EventBoxGroupId = eventBoxGroupId;
      EventBoxId = eventBoxId;
      BaseEventId = baseEventId;
      GroupType = groupType;
      ValueType = valueType;
      Axis = axis;
      ElementId = elementId;
      DistributionOrder = distributionOrder;
      AuthoredValue = authoredValue;
      FlipSign = flipSign;
      Mirrored = mirrored;
      RuntimeDistribution = runtimeDistribution;
      TranslationLimits = translationLimits;
      DistributionLimits = distributionLimits;
      SourceLocalPosition = sourceLocalPosition;
      ParentWorldMatrix = parentWorldMatrix;
      IsInvertible = isInvertible;
      RuntimeSourceIdentity = runtimeSourceIdentity;
      SourceSortOrder = sourceSortOrder;
   }

   public LightAxis Axis { get; }
   public float AuthoredValue { get; }
   public BeatmapEditorObjectId BaseEventId { get; }
   public Vector2 DistributionLimits { get; }
   public int DistributionOrder { get; }
   public int ElementId { get; }
   public BeatmapEditorObjectId EventBoxGroupId { get; }
   public BeatmapEditorObjectId EventBoxId { get; }
   public float FlipSign { get; }
   public EventBoxGroupType GroupType { get; }
   public bool IsInvertible { get; }
   public bool Mirrored { get; }
   public Matrix4x4 ParentWorldMatrix { get; }
   public float RuntimeDistribution { get; }
   public object RuntimeSourceIdentity { get; }
   public Vector3 SourceLocalPosition { get; }
   public int SourceSortOrder { get; }
   public Vector2 TranslationLimits { get; }
   public MotionPathEventValueType ValueType { get; }

   public Vector3 GetLocalAxis()
   {
      return Axis switch
      {
         LightAxis.X => Vector3.right,
         LightAxis.Y => Vector3.up,
         _ => Vector3.forward
      };
   }
}

internal readonly struct MotionPathSample
{
   public MotionPathSample(float beat, Vector3 position)
   {
      Beat = beat;
      Position = position;
   }

   public float Beat { get; }
   public Vector3 Position { get; }
}
