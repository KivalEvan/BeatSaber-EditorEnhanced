using System.Collections.Generic;
using BeatmapEditor3D;
using UnityEngine;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.Gizmo;

internal sealed class GizmoPlan
{
   public GizmoPlan(Transform root, IReadOnlyList<GizmoRenderBatch> batches)
   {
      Root = root;
      Batches = batches;
   }

   public Transform Root { get; }
   public IReadOnlyList<GizmoRenderBatch> Batches { get; }
}

internal readonly struct GizmoRenderBatch
{
   public GizmoRenderBatch(
      EventBoxGroupType groupType,
      LightAxis axis,
      LightGroupSubsystem subsystem,
      LightTransformData[] transforms)
   {
      GroupType = groupType;
      Axis = axis;
      Subsystem = subsystem;
      Transforms = transforms;
   }

   public EventBoxGroupType GroupType { get; }
   public LightAxis Axis { get; }
   public LightGroupSubsystem Subsystem { get; }
   public LightTransformData[] Transforms { get; }
}

public record struct LightTransformData
{
   public int AxisBoxIndex;
   public int ChunkIndex;
   public bool Distributed;
   public EventBoxEditorData EventBoxContext;
   public int GlobalBoxIndex;
   public int Index;
   public Transform Transform;
}