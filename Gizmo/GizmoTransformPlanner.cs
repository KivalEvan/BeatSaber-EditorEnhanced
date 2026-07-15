using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Utils;
using UnityEngine;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.Gizmo;

internal sealed class GizmoTransformPlanner
{
   private static readonly LightAxis[] Axes = [LightAxis.X, LightAxis.Y, LightAxis.Z];

   private readonly BeatmapEventBoxGroupsDataModel _dataModel;
   private readonly GizmoEffectContextResolver _effectContextResolver;

   public GizmoTransformPlanner(
      BeatmapEventBoxGroupsDataModel dataModel,
      GizmoEffectContextResolver effectContextResolver)
   {
      _dataModel = dataModel;
      _effectContextResolver = effectContextResolver;
   }

   public bool TryCreatePlan(EventBoxGroupEditorData group, out GizmoPlan plan)
   {
      plan = null;
      if (!_effectContextResolver.TryResolve(group.type, out var effects)) return false;

      var batches = group.type switch
      {
         EventBoxGroupType.Color => PlanColor(group, effects.ColorManager),
         EventBoxGroupType.Rotation => PlanRotation(group, effects.RotationManager),
         EventBoxGroupType.Translation => PlanTranslation(group, effects.TranslationManager),
         EventBoxGroupType.FloatFx => PlanFx(group, effects.FxManager),
         _ => []
      };
      plan = new GizmoPlan(effects.Root, batches);
      return true;
   }

   private List<GizmoRenderBatch> PlanColor(
      EventBoxGroupEditorData group,
      LightColorGroupEffectManager manager)
   {
      var eventBoxes = _dataModel
         .GetEventBoxesByEventBoxGroupId(group.id)
         .Cast<LightColorEventBoxEditorData>()
         .ToArray();
      var batches = new List<GizmoRenderBatch>();

      foreach (var lightGroup in manager.lightGroups.Where(item => item.groupId == group.groupId))
      {
         var marked = MarkFirstAffectedElements(eventBoxes, lightGroup.numberOfElements, item => LightAxis.X);
         var transforms = new List<LightTransformData>();
         foreach (var item in marked[LightAxis.X]
            .Select(mark =>
            {
               var lights = manager
                  ._lightColorGroupEffects
                  .FirstOrDefault(effect => effect._lightId == lightGroup.startLightId + mark.Key)
                  ?._lightManager._lights.ElementAtOrDefault(lightGroup.startLightId + mark.Key);
               return (mark.Value, Lights: lights);
            })
            .Where(item => item.Lights != null)
            .Select((item, index) => (item.Value with { Index = index }, item.Lights)))
         foreach (var light in item.Lights)
            switch (light)
            {
               case MaterialLightWithId materialLight:
                  transforms.Add(item.Item1 with { Transform = materialLight.transform });
                  break;
               case TubeBloomPrePassLightWithId tubeLight:
                  transforms.Add(item.Item1 with { Transform = tubeLight.transform });
                  break;
            }

         batches.Add(new GizmoRenderBatch(group.type, LightAxis.X, null, transforms.ToArray()));
      }

      return batches;
   }

   private List<GizmoRenderBatch> PlanRotation(
      EventBoxGroupEditorData group,
      LightRotationGroupEffectManager manager)
   {
      var eventBoxes = _dataModel
         .GetEventBoxesByEventBoxGroupId(group.id)
         .Cast<LightRotationEventBoxEditorData>()
         .ToArray();
      return manager
         ._lightRotationGroups
         .Where(item => item.groupId == group.groupId)
         .SelectMany(item => PlanAxisGroups(
            group.type,
            eventBoxes,
            item,
            item.lightGroup.numberOfElements,
            eventBox => eventBox.axis,
            axis => GetTransforms(item, axis)))
         .ToList();
   }

   private List<GizmoRenderBatch> PlanTranslation(
      EventBoxGroupEditorData group,
      LightTranslationGroupEffectManager manager)
   {
      var eventBoxes = _dataModel
         .GetEventBoxesByEventBoxGroupId(group.id)
         .Cast<LightTranslationEventBoxEditorData>()
         .ToArray();
      return manager
         ._lightTranslationGroups
         .Where(item => item.groupId == group.groupId)
         .SelectMany(item => PlanAxisGroups(
            group.type,
            eventBoxes,
            item,
            item.lightGroup.numberOfElements,
            eventBox => eventBox.axis,
            axis => GetTransforms(item, axis)))
         .ToList();
   }

   private List<GizmoRenderBatch> PlanFx(EventBoxGroupEditorData group, FloatFxGroupEffectManager manager)
   {
      var eventBoxes = _dataModel
         .GetEventBoxesByEventBoxGroupId(group.id)
         .Cast<FxEventBoxEditorData>()
         .ToArray();
      var batches = new List<GizmoRenderBatch>();

      foreach (var fxGroup in manager._floatFxGroups.Where(item => item.groupId == group.groupId))
      {
         var marked = MarkFirstAffectedElements(eventBoxes, fxGroup.lightGroup.numberOfElements, item => LightAxis.X);
         var transforms = marked[LightAxis.X]
            .Select(item => item.Value with
            {
               Transform = fxGroup.targets.Select(target => target.transform).ElementAtOrDefault(item.Key)
            })
            .ToArray();
         batches.Add(new GizmoRenderBatch(group.type, LightAxis.X, fxGroup, transforms));
      }

      return batches;
   }

   private static IEnumerable<GizmoRenderBatch> PlanAxisGroups<TEventBox>(
      EventBoxGroupType groupType,
      TEventBox[] eventBoxes,
      LightGroupSubsystem subsystem,
      int elementCount,
      Func<TEventBox, LightAxis> getAxis,
      Func<LightAxis, IEnumerable<Transform>> getTransforms)
      where TEventBox : EventBoxEditorData
   {
      var marked = MarkFirstAffectedElements(eventBoxes, elementCount, getAxis);
      foreach (var axis in Axes)
      {
         var transforms = getTransforms(axis);
         var data = marked[axis]
            .Select(item => item.Value with { Index = item.Key, Transform = transforms.ElementAtOrDefault(item.Key) })
            .ToArray();
         yield return new GizmoRenderBatch(groupType, axis, subsystem, data);
      }
   }

   private static Dictionary<LightAxis, Dictionary<int, LightTransformData>> MarkFirstAffectedElements<TEventBox>(
      TEventBox[] eventBoxes,
      int elementCount,
      Func<TEventBox, LightAxis> getAxis)
      where TEventBox : EventBoxEditorData
   {
      var axisCounts = Axes.ToDictionary(axis => axis, _ => 0);
      var marked = Axes.ToDictionary(axis => axis, _ => new Dictionary<int, LightTransformData>());

      for (var boxIndex = 0; boxIndex < eventBoxes.Length; boxIndex++)
      {
         var eventBox = eventBoxes[boxIndex];
         var axis = getAxis(eventBox);
         if (!marked.TryGetValue(axis, out var axisElements)) continue;

         var distributed = eventBox.beatDistributionParam > 0;
         foreach (var (index, chunkIndex) in IndexFilterHelpers
            .GetIndexFilterRange(eventBox.indexFilter, elementCount)
            .Where(element => !axisElements.ContainsKey(element.index)))
            axisElements.Add(
               index,
               new LightTransformData
               {
                  GlobalBoxIndex = boxIndex,
                  AxisBoxIndex = axisCounts[axis],
                  ChunkIndex = chunkIndex,
                  EventBoxContext = eventBox,
                  Distributed = distributed
               });

         axisCounts[axis]++;
      }

      return marked;
   }

   private static IEnumerable<Transform> GetTransforms(LightRotationGroup group, LightAxis axis)
   {
      return axis switch
      {
         LightAxis.X => group.xTransforms,
         LightAxis.Y => group.yTransforms,
         LightAxis.Z => group.zTransforms,
         _ => []
      };
   }

   private static IEnumerable<Transform> GetTransforms(LightTranslationGroup group, LightAxis axis)
   {
      return axis switch
      {
         LightAxis.X => group.xTransforms,
         LightAxis.Y => group.yTransforms,
         LightAxis.Z => group.zTransforms,
         _ => []
      };
   }
}