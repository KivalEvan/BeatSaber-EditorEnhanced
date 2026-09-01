using Tweening;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace EditorEnhanced.MotionPath;

internal struct MotionPathSamplingNode
{
   public int ParentIndex;
   public Vector3 LocalPosition;
   public Quaternion LocalRotation;
   public Vector3 LocalScale;
   public int RotationXBindingIndex;
   public int RotationYBindingIndex;
   public int RotationZBindingIndex;
   public int TranslationXBindingIndex;
   public int TranslationYBindingIndex;
   public int TranslationZBindingIndex;
   public byte HasRotation;
   public byte HasTranslation;
}

internal struct MotionPathSamplingRotationBinding
{
   public int EventStartIndex;
   public int EventCount;
   public byte Mirrored;
}

internal struct MotionPathSamplingTranslationBinding
{
   public int EventStartIndex;
   public int EventCount;
   public byte Mirrored;
   public Vector2 TranslationLimits;
   public Vector2 DistributionLimits;
}

internal struct MotionPathSamplingRotationEvent
{
   public float Time;
   public float Value;
   public EaseType EaseType;
   public int NextEventIndex;
   public int LoopCount;
   public LightRotationDirection RotationDirection;
}

internal struct MotionPathSamplingTranslationEvent
{
   public float Time;
   public float Value;
   public float Distribution;
   public EaseType EaseType;
   public int NextEventIndex;
}

internal struct MotionPathSamplingJob : IJobParallelFor
{
   [ReadOnly] public NativeArray<MotionPathSamplingNode> Nodes;
   [ReadOnly] public NativeArray<MotionPathSamplingRotationBinding> RotationBindings;
   [ReadOnly] public NativeArray<MotionPathSamplingTranslationBinding> TranslationBindings;
   [ReadOnly] public NativeArray<MotionPathSamplingRotationEvent> RotationEvents;
   [ReadOnly] public NativeArray<MotionPathSamplingTranslationEvent> TranslationEvents;
    [ReadOnly] public NativeArray<int> TrackNodeIndices;
    [ReadOnly] public NativeArray<float> SampleTimes;
    public int SampleStartIndex;

   [NativeDisableParallelForRestriction]
   public NativeArray<Vector3> OutputPositions;

   [NativeDisableParallelForRestriction]
   public NativeArray<Matrix4x4> WorldMatrixScratch;

    public void Execute(int sampleBeatIndex)
    {
       var time = SampleTimes[SampleStartIndex + sampleBeatIndex];
      var nodeCount = Nodes.Length;
      var matrixOffset = sampleBeatIndex * nodeCount;

      for (var nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
      {
         var node = Nodes[nodeIndex];
         var position = EvaluatePosition(node, time);
         var rotation = EvaluateRotation(node, time);
         var matrix = Matrix4x4.TRS(position, rotation, node.LocalScale);
         if (node.ParentIndex >= 0)
            matrix = WorldMatrixScratch[matrixOffset + node.ParentIndex] * matrix;
         WorldMatrixScratch[matrixOffset + nodeIndex] = matrix;
      }

      var outputOffset = sampleBeatIndex * TrackNodeIndices.Length;
      for (var trackIndex = 0; trackIndex < TrackNodeIndices.Length; trackIndex++)
      {
         var matrix = WorldMatrixScratch[matrixOffset + TrackNodeIndices[trackIndex]];
         OutputPositions[outputOffset + trackIndex] = matrix.MultiplyPoint3x4(Vector3.zero);
      }
   }

   private Vector3 EvaluatePosition(MotionPathSamplingNode node, float time)
   {
      var position = node.HasTranslation == 0 ? node.LocalPosition : Vector3.zero;
      if (node.TranslationXBindingIndex >= 0)
         position.x = EvaluateTranslation(node.TranslationXBindingIndex, time);
      if (node.TranslationYBindingIndex >= 0)
         position.y = EvaluateTranslation(node.TranslationYBindingIndex, time);
      if (node.TranslationZBindingIndex >= 0)
         position.z = EvaluateTranslation(node.TranslationZBindingIndex, time);
      return position;
   }

   private Quaternion EvaluateRotation(MotionPathSamplingNode node, float time)
   {
      if (node.HasRotation == 0) return node.LocalRotation;

      var angles = Vector3.zero;
      if (node.RotationXBindingIndex >= 0)
         angles.x = EvaluateRotation(node.RotationXBindingIndex, time);
      if (node.RotationYBindingIndex >= 0)
         angles.y = EvaluateRotation(node.RotationYBindingIndex, time);
      if (node.RotationZBindingIndex >= 0)
         angles.z = EvaluateRotation(node.RotationZBindingIndex, time);
      return Quaternion.AngleAxis(angles.x, Vector3.right)
             * Quaternion.AngleAxis(angles.y, Vector3.up)
             * Quaternion.AngleAxis(angles.z, Vector3.forward);
   }

   private float EvaluateRotation(int bindingIndex, float time)
   {
      var binding = RotationBindings[bindingIndex];
      var currentIndex = FindCurrentRotationEvent(binding, time);
      if (currentIndex < 0) return 0f;

      var current = RotationEvents[currentIndex];
      var value = Mathf.Repeat(current.Value, 360f);
      if (current.NextEventIndex >= 0)
      {
         var next = RotationEvents[current.NextEventIndex];
         if (next.EaseType != EaseType.None && next.Time > current.Time)
         {
            var target = LightRotationEventHandler.ComputeTargetAngle(
               value,
               Mathf.Repeat(next.Value, 360f),
               next.LoopCount,
               next.RotationDirection);
            value = Mathf.LerpUnclamped(
               value,
               target,
               Interpolation.Interpolate(
                  Mathf.InverseLerp(current.Time, next.Time, time),
                  next.EaseType));
         }
      }

      return binding.Mirrored == 0 ? value : -value;
   }

   private float EvaluateTranslation(int bindingIndex, float time)
   {
      var binding = TranslationBindings[bindingIndex];
      var currentIndex = FindCurrentTranslationEvent(binding, time);
      if (currentIndex < 0) return 0f;

      var current = TranslationEvents[currentIndex];
      var value = EvaluateTranslation(current, binding);
      if (current.NextEventIndex >= 0)
      {
         var next = TranslationEvents[current.NextEventIndex];
         if (next.EaseType != EaseType.None && next.Time > current.Time)
            value = Mathf.LerpUnclamped(
               value,
               EvaluateTranslation(next, binding),
               Interpolation.Interpolate(
                  Mathf.InverseLerp(current.Time, next.Time, time),
                  next.EaseType));
      }

      return value;
   }

   private int FindCurrentRotationEvent(MotionPathSamplingRotationBinding binding, float time)
   {
      var low = binding.EventStartIndex;
      var high = low + binding.EventCount;
      while (low < high)
      {
         var middle = low + (high - low) / 2;
         if (RotationEvents[middle].Time <= time) low = middle + 1;
         else high = middle;
      }

      return low == binding.EventStartIndex ? -1 : low - 1;
   }

   private int FindCurrentTranslationEvent(MotionPathSamplingTranslationBinding binding, float time)
   {
      var low = binding.EventStartIndex;
      var high = low + binding.EventCount;
      while (low < high)
      {
         var middle = low + (high - low) / 2;
         if (TranslationEvents[middle].Time <= time) low = middle + 1;
         else high = middle;
      }

      return low == binding.EventStartIndex ? -1 : low - 1;
   }

   private static float EvaluateTranslation(
      MotionPathSamplingTranslationEvent animationEvent,
      MotionPathSamplingTranslationBinding binding)
   {
      return LightTranslationEventHandler.ComputeTranslation(
         animationEvent.Value,
         binding.TranslationLimits,
         animationEvent.Distribution,
         binding.DistributionLimits,
         binding.Mirrored != 0);
   }
}
