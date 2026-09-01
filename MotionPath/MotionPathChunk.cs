using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BeatmapEditor3D;
using UnityEngine;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.MotionPath;

internal readonly struct MotionPathChunkKey : IEquatable<MotionPathChunkKey>
{
   public MotionPathChunkKey(int chunkIndex, long geometryGeneration, int samplesPerBeat)
   {
      ChunkIndex = chunkIndex;
      GeometryGeneration = geometryGeneration;
      SamplesPerBeat = samplesPerBeat;
   }

   public int ChunkIndex { get; }
   public long GeometryGeneration { get; }
   public int SamplesPerBeat { get; }

   public bool Equals(MotionPathChunkKey other) =>
      ChunkIndex == other.ChunkIndex
      && GeometryGeneration == other.GeometryGeneration
      && SamplesPerBeat == other.SamplesPerBeat;

   public override bool Equals(object obj) =>
      obj is MotionPathChunkKey other && Equals(other);

   public override int GetHashCode()
   {
      unchecked
      {
         var hash = ChunkIndex;
         hash = hash * 397 ^ GeometryGeneration.GetHashCode();
         return hash * 397 ^ SamplesPerBeat;
      }
   }

   public static bool operator ==(MotionPathChunkKey left, MotionPathChunkKey right) => left.Equals(right);
   public static bool operator !=(MotionPathChunkKey left, MotionPathChunkKey right) => !left.Equals(right);
}

internal sealed class MotionPathChunk
{
   public const int SizeInBeats = 4;

   public MotionPathChunk(
      MotionPathChunkKey key,
      float startBeat,
      float endBeat,
      float songEndBeat,
      IReadOnlyList<MotionPathTrack> tracks)
   {
      var trackSnapshot = new MotionPathTrack[tracks.Count];
      for (var trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
         trackSnapshot[trackIndex] = tracks[trackIndex];

      Key = key;
      StartBeat = startBeat;
      EndBeat = endBeat;
      SongEndBeat = songEndBeat;
      Tracks = Array.AsReadOnly(trackSnapshot);
   }

   public MotionPathChunkKey Key { get; }
   public float StartBeat { get; }
   public float EndBeat { get; }
   public float SongEndBeat { get; }
   public IReadOnlyList<MotionPathTrack> Tracks { get; }
}

internal sealed class MotionPathTrack
{
   private MotionPathTrack(
      Transform source,
      bool isFocused,
      ReadOnlyCollection<MotionPathSample> ownedSampleSnapshot,
      IReadOnlyList<MotionPathEventPoint> eventPoints,
      IReadOnlyList<float> stepBeats,
      int[] visualSampleIndices)
   {
      Source = source;
      IsFocused = isFocused;
      Samples = ownedSampleSnapshot;
      EventPoints = Snapshot(eventPoints);
      StepBeats = Snapshot(stepBeats);
      VisualSampleIndices = Array.AsReadOnly((int[])visualSampleIndices.Clone());
   }

   internal static MotionPathTrack CreateWithOwnedSampleSnapshot(
      Transform source,
      bool isFocused,
      ReadOnlyCollection<MotionPathSample> ownedSampleSnapshot,
      IReadOnlyList<MotionPathEventPoint> eventPoints,
      IReadOnlyList<float> stepBeats,
      int[] visualSampleIndices) =>
      new(
         source,
         isFocused,
         ownedSampleSnapshot,
         eventPoints,
         stepBeats,
         visualSampleIndices);

   public IReadOnlyList<MotionPathEventPoint> EventPoints { get; }
   public Transform Source { get; }
   public bool IsFocused { get; }
   public IReadOnlyList<MotionPathSample> Samples { get; }
   public IReadOnlyList<float> StepBeats { get; }
   public IReadOnlyList<int> VisualSampleIndices { get; }

   private static IReadOnlyList<T> Snapshot<T>(IReadOnlyList<T> values)
   {
      var snapshot = new T[values.Count];
      for (var index = 0; index < values.Count; index++) snapshot[index] = values[index];
      return Array.AsReadOnly(snapshot);
   }
}

internal readonly struct MotionPathTrackPublication
{
   public MotionPathTrackPublication(MotionPathChunkKey key, int trackIndex, MotionPathTrack track)
   {
      Key = key;
      TrackIndex = trackIndex;
      Track = track;
   }

   public MotionPathChunkKey Key { get; }
   public MotionPathTrack Track { get; }
   public int TrackIndex { get; }
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
      var sourceSnapshot = new MotionPathEventSource[sources.Count];
      for (var sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
         sourceSnapshot[sourceIndex] = sources[sourceIndex];
      Sources = Array.AsReadOnly(sourceSnapshot);
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
