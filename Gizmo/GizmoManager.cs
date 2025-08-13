using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.Types;
using EditorEnhanced.Commands;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.Gizmo.Components;
using EditorEnhanced.Helpers;
using EditorEnhanced.UI;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.Utils;
using UnityEngine;
using Zenject;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;
using Object = UnityEngine.Object;

namespace EditorEnhanced.Gizmo;

internal class GizmoManager : IInitializable, IDisposable
{
   private readonly List<GameObject> _activeGizmos = [];
   private readonly BeatmapState _beatmapState;
   private readonly BeatmapEventBoxGroupsDataModel _bebgdm;
   private readonly PluginConfig _config;
   private readonly EventBoxGroupsState _ebgs;
   private readonly EditBeatmapViewController _ebvc;
   private readonly GizmoAssets _gizmoAssets;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;
   private LightColorGroupEffectManager _colorManager;
   private FloatFxGroupEffectManager _fxManager;
   private GizmoDragInputSystem _gizmoDragInputSystem;

   private GizmoInfo _gizmoInfo;
   private LightRotationGroupEffectManager _rotationManager;
   private LightTranslationGroupEffectManager _translationManager;

   public GizmoManager(
      GizmoAssets gizmoAssets,
      SignalBus signalBus,
      PluginConfig config,
      BeatmapState beatmapState,
      EventBoxGroupsState ebgs,
      BeatmapEventBoxGroupsDataModel bebgdm,
      UIBuilder uiBuilder,
      EditBeatmapViewController ebvc)
   {
      _gizmoAssets = gizmoAssets;
      _signalBus = signalBus;
      _beatmapState = beatmapState;
      _config = config;
      _ebgs = ebgs;
      _bebgdm = bebgdm;
      _uiBuilder = uiBuilder;
      _ebvc = ebvc;
   }

   public void Dispose()
   {
      _signalBus.TryUnsubscribe<BeatmapEditingModeSwitchedSignal>(HandleGizmoSignalWithSignal);
      _signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<ModifyEventBoxSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<InsertEventBoxSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<InsertEventBoxesForAllAxesSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsAndAxisSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<DeleteEventBoxSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<SortAxisEventBoxGroupSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<SortIdEventBoxGroupSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<GizmoRefreshSignal>(HandleGizmoSignal);

      _activeGizmos.Clear();
      _colorManager = null;
      _rotationManager = null;
      _translationManager = null;
      _fxManager = null;
      _gizmoDragInputSystem = null;
   }

   public void Initialize()
   {
      var go = new GameObject
      {
         name = "GizmoDragInputSystem"
      };
      go.SetActive(false);
      _gizmoDragInputSystem = go.AddComponent<GizmoDragInputSystem>();

      go = _uiBuilder
         .Text.Instantiate()
         .SetName("GizmoInfo")
         .SetColor(Color.white)
         .SetAnchorMin(new Vector2(0.03f, 0.95f))
         .SetAnchorMax(new Vector2(0.03f, 0.95f))
         .Create(_ebvc.transform);
      go.SetActive(false);
      _gizmoInfo = go.AddComponent<GizmoInfo>();

      _colorManager = Object.FindAnyObjectByType<LightColorGroupEffectManager>();
      _rotationManager =
         Object.FindAnyObjectByType<LightRotationGroupEffectManager>();
      _translationManager =
         Object.FindAnyObjectByType<LightTranslationGroupEffectManager>();
      _fxManager =
         Object.FindAnyObjectByType<FloatFxGroupEffectManager>();

      _signalBus.Subscribe<BeatmapEditingModeSwitchedSignal>(HandleGizmoSignalWithSignal);
      _signalBus.Subscribe<EventBoxesUpdatedSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<ModifyEventBoxSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<InsertEventBoxSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<InsertEventBoxesForAllAxesSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<InsertEventBoxesForAllIdsSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<InsertEventBoxesForAllIdsAndAxisSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<DeleteEventBoxSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<SortAxisEventBoxGroupSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<SortIdEventBoxGroupSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<GizmoRefreshSignal>(HandleGizmoSignal);
   }

   private void HandleGizmoSignalWithSignal(BeatmapEditingModeSwitchedSignal signal)
   {
      if (_config.Gizmo.Enabled && signal.mode == BeatmapEditingMode.EventBoxes)
         AddGizmo();
      else
         RemoveGizmo();
   }

   private void HandleGizmoSignal()
   {
      RemoveGizmo();
      if (!_config.Gizmo.Enabled || _beatmapState.editingMode != BeatmapEditingMode.EventBoxes) return;
      AddGizmo();
   }

   private void AddGizmo()
   {
      switch (_ebgs.eventBoxGroupContext.type)
      {
         case EventBoxGroupType.Color:
            AddColorGizmo();
            break;
         case EventBoxGroupType.Rotation:
            AddRotationGizmo();
            break;
         case EventBoxGroupType.Translation:
            AddTranslationGizmo();
            _gizmoInfo.gameObject.SetActive(true);
            break;
         case EventBoxGroupType.FloatFx:
            AddFxGizmo();
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      _gizmoDragInputSystem.gameObject.SetActive(true);
   }

   private void RemoveGizmo()
   {
      foreach (var gizmo in _activeGizmos) gizmo.SetActive(false);
      _activeGizmos.Clear();
      _gizmoInfo.Clear();
      _gizmoInfo.gameObject.SetActive(false);
      _gizmoDragInputSystem.gameObject.SetActive(false);
   }

   private void DistributeGizmo(
      List<LightTransformData> list,
      EventBoxGroupType groupType,
      LightAxis axis,
      bool mirror,
      LightGroupSubsystem subsystemContext)
   {
      var onlyUnique = list.Select(d => d.AxisBoxIndex).ToHashSet().Count == 1;

      var highlighterMap = new Dictionary<int, GizmoHighlightController>();
      foreach (var data in list)
      {
         var idx = data.Index;
         var globalBoxIdx = data.GlobalBoxIndex;
         var axisBoxIdx = data.AxisBoxIndex;
         var eventBoxContext = data.EventBoxContext;
         var distributed = data.Distributed;
         var transform = data.Transform;

         if (!onlyUnique) axisBoxIdx += 1;
         var colorIdx = _config.Gizmo.MulticolorId
            ? ColorAssignment.GetColorIndexEventBox(axisBoxIdx, idx, distributed)
            : ColorAssignment.GetColorIndexEventBox(0);

         if (!highlighterMap.ContainsKey(globalBoxIdx))
         {
            var laneGizmo = _gizmoAssets.GetOrCreate(GizmoType.Lane, colorIdx);
            laneGizmo.transform.SetParent(_colorManager.transform.root, false);
            laneGizmo.GetComponent<GizmoSwappable>().EventBoxEditorDataContext = eventBoxContext;

            var groupHighlightController = laneGizmo.GetComponent<GizmoHighlightController>();
            groupHighlightController.Init();
            groupHighlightController.Add(laneGizmo);
            highlighterMap.Add(globalBoxIdx, groupHighlightController);

            laneGizmo.SetActive(true);
            _activeGizmos.Add(laneGizmo);
         }

         if (transform == null) continue;
         _gizmoInfo.AddLightTransform(data);
         var axisIdx = axis switch
         {
            LightAxis.X => ColorAssignment.RedIndex,
            LightAxis.Y => ColorAssignment.GreenIndex,
            LightAxis.Z => ColorAssignment.BlueIndex,
            _ => ColorAssignment.WhiteIndex
         };

         var baseGizmo =
            _gizmoAssets.GetOrCreate(
               _config.Gizmo.DistributeShape && distributed ? GizmoType.Sphere : GizmoType.Cube,
               colorIdx);
         var baseHighlightController = baseGizmo.GetComponent<GizmoHighlightController>();
         baseHighlightController.SharedWith(highlighterMap[globalBoxIdx]);
         baseHighlightController.Add(baseGizmo);
         baseGizmo.GetComponent<GizmoNone>().TargetTransform = transform;

         var modGizmo = groupType switch
         {
            EventBoxGroupType.Rotation => _gizmoAssets.GetOrCreate(GizmoType.Rotation, axisIdx),
            EventBoxGroupType.Translation => _gizmoAssets.GetOrCreate(GizmoType.Translation, axisIdx),
            _ => null
         };
         if (modGizmo != null)
         {
            modGizmo.transform.SetParent(baseGizmo.transform, false);
            var modHighlightController = modGizmo.GetComponent<GizmoHighlightController>();
            modHighlightController.SharedWith(highlighterMap[globalBoxIdx]);
            modHighlightController.Add(modGizmo);

            var gizmoDraggable = modGizmo.GetComponent<GizmoDraggable>();
            gizmoDraggable.EventBoxEditorDataContext = eventBoxContext;
            gizmoDraggable.LightGroupSubsystemContext = subsystemContext;
            gizmoDraggable.Axis = axis;
            gizmoDraggable.TargetTransform = transform;
            gizmoDraggable.Mirror = mirror;

            modGizmo.SetActive(true);
            _activeGizmos.Add(modGizmo);

            // var lineRenderController = baseGizmo.GetComponent<LineRenderController>();
            // var current = transform;
            // var transforms = new List<Transform>();
            // while (current != null)
            // {
            //     transforms.Add(current);
            //     if (current.gameObject
            //         .GetComponent<LightTransformGroup<LightGroupTranslationXTransform,
            //             LightGroupTranslationYTransform, LightGroupTranslationZTransform>>())
            //     {
            //         lineRenderController.SetTransforms(transforms.ToArray());
            //         lineRenderController.SetMaterial(_gizmoAssets.GetOrCreateMaterial(axisIdx));
            //         lineRenderController.enabled = true;
            //         break;
            //     }
            //
            //     current = current.parent;
            // }
         }

         baseGizmo.SetActive(true);
         _activeGizmos.Add(baseGizmo);
      }

      var selection = _gizmoAssets.GetOrCreate(GizmoType.Selection, 0);
      selection.SetActive(true);
      _activeGizmos.Add(selection);
   }

   private void AddColorGizmo()
   {
      var ebg = _bebgdm
         .GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
         .Cast<LightColorEventBoxEditorData>();

      foreach (var l in _colorManager.lightGroups
                  .Where(x => x.groupId == _ebgs.eventBoxGroupContext.groupId))
      {
         var markId = new Dictionary<int, LightTransformData>();
         foreach (var item in ebg.Select((eb, boxIdx) =>
                                            (boxIdx, eb, eb.beatDistributionParam > 0,
                                               IndexFilterHelpers.GetIndexFilterRange(
                                                  eb.indexFilter,
                                                  l.numberOfElements))))
         {
            var (boxIdx, eventBox, distributed, indexFilterIds) = item;
            foreach (var idx in indexFilterIds.Where(i => !markId.ContainsKey(i)))
               markId.Add(
                  idx,
                  new LightTransformData
                  {
                     GlobalBoxIndex = boxIdx, AxisBoxIndex = boxIdx, EventBoxContext = eventBox,
                     Distributed = distributed
                  });
         }

         var list = new List<LightTransformData>();
         foreach (var item in markId
                     .Select(i =>
                     {
                        return (i.Value, _colorManager
                           ._lightColorGroupEffects
                           .FirstOrDefault(x => x._lightId == l.startLightId + i.Key)
                           ?._lightManager._lights.ElementAt(l.startLightId + i.Key));
                     })
                     .Where(item => item.Item2 != null)
                     .Select((item, i) =>
                     {
                        var data = item.Value;
                        data.Index = i;
                        return (data, LightWithIds: item.Item2);
                     }))
         foreach (var lightWithId in item.LightWithIds)
            switch (lightWithId)
            {
               case MaterialLightWithId matLightWithId:
                  list.Add(item.data with { Transform = matLightWithId.transform });
                  break;
               case TubeBloomPrePassLightWithId tubeBloomPrePassLightWithId:
                  list.Add(item.data with { Transform = tubeBloomPrePassLightWithId.transform });
                  break;
            }

         DistributeGizmo(list, _ebgs.eventBoxGroupContext.type, LightAxis.X, false, null);
      }
   }

   private void AddRotationGizmo()
   {
      var ebg = _bebgdm
         .GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
         .Cast<LightRotationEventBoxEditorData>();

      foreach (var l in _rotationManager._lightRotationGroups.Where(x =>
                                                                       x.groupId == _ebgs.eventBoxGroupContext.groupId))
      {
         var axisCount = new Dictionary<LightAxis, int>
            { { LightAxis.X, 0 }, { LightAxis.Y, 0 }, { LightAxis.Z, 0 } };
         var markXIdx = new Dictionary<int, LightTransformData>();
         var markYIdx = new Dictionary<int, LightTransformData>();
         var markZIdx = new Dictionary<int, LightTransformData>();
         foreach (var item in ebg.Select((eb, boxIdx) =>
                                            (boxIdx, eb, eb.beatDistributionParam > 0,
                                               IndexFilterHelpers.GetIndexFilterRange(
                                                  eb.indexFilter,
                                                  l.lightGroup.numberOfElements), eb.axis)))
         {
            var (boxIdx, eventBox, distributed, indexFilterIds, axis) = item;
            var putTo = axis switch
            {
               LightAxis.X => markXIdx,
               LightAxis.Y => markYIdx,
               LightAxis.Z => markZIdx,
               _ => throw new ArgumentOutOfRangeException()
            };
            foreach (var i in indexFilterIds.Where(i => !putTo.ContainsKey(i)))
               putTo.Add(
                  i,
                  new LightTransformData
                  {
                     GlobalBoxIndex = boxIdx, AxisBoxIndex = axisCount[axis], EventBoxContext = eventBox,
                     Distributed = distributed
                  });

            axisCount[axis]++;
         }

         var transformsXYZ = new[] { l.xTransforms, l.yTransforms, l.zTransforms };
         var markIdXYZ = new[] { markXIdx, markYIdx, markZIdx };
         foreach (LightAxis axis in Enum.GetValues(typeof(LightAxis)))
         {
            var transforms = transformsXYZ[(int)axis];
            var markId = markIdXYZ[(int)axis];
            var list = markId
               .Select(item =>
               {
                  var data = item.Value;
                  data.Index = item.Key;
                  data.Transform = transforms.ElementAtOrDefault(item.Key);
                  return data;
               })
               .ToList();

            var mirror = axis switch
            {
               LightAxis.X => l.mirrorX,
               LightAxis.Y => l.mirrorY,
               LightAxis.Z => l.mirrorZ,
               _ => false
            };
            DistributeGizmo(list, _ebgs.eventBoxGroupContext.type, axis, mirror, l);
         }
      }
   }

   private void AddTranslationGizmo()
   {
      var ebg = _bebgdm
         .GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
         .Cast<LightTranslationEventBoxEditorData>();

      foreach (var l in _translationManager._lightTranslationGroups.Where(x =>
                                                                             x.groupId
                                                                             == _ebgs.eventBoxGroupContext.groupId))
      {
         var axisCount = new Dictionary<LightAxis, int>
            { { LightAxis.X, 0 }, { LightAxis.Y, 0 }, { LightAxis.Z, 0 } };
         var markXIdx = new Dictionary<int, LightTransformData>();
         var markYIdx = new Dictionary<int, LightTransformData>();
         var markZIdx = new Dictionary<int, LightTransformData>();
         foreach (var item in ebg.Select((eb, boxIdx) =>
                                            (boxIdx, eb, eb.beatDistributionParam > 0,
                                               IndexFilterHelpers.GetIndexFilterRange(
                                                  eb.indexFilter,
                                                  l.lightGroup.numberOfElements), eb.axis)))
         {
            var (boxIdx, eventBox, distributed, indexFilterIds, axis) = item;
            var putTo = axis switch
            {
               LightAxis.X => markXIdx,
               LightAxis.Y => markYIdx,
               LightAxis.Z => markZIdx,
               _ => throw new ArgumentOutOfRangeException()
            };
            foreach (var i in indexFilterIds.Where(i => !putTo.ContainsKey(i)))
               putTo.Add(
                  i,
                  new LightTransformData
                  {
                     GlobalBoxIndex = boxIdx, AxisBoxIndex = axisCount[axis], EventBoxContext = eventBox,
                     Distributed = distributed
                  });

            axisCount[axis]++;
         }

         var transformsXYZ = new[] { l.xTransforms, l.yTransforms, l.zTransforms };
         var markIdXYZ = new[] { markXIdx, markYIdx, markZIdx };
         foreach (LightAxis axis in Enum.GetValues(typeof(LightAxis)))
         {
            var transforms = transformsXYZ[(int)axis];
            var markId = markIdXYZ[(int)axis];
            var list = markId
               .Select(item =>
               {
                  var data = item.Value;
                  data.Index = item.Key;
                  data.Transform = transforms.ElementAtOrDefault(item.Key);
                  return data;
               })
               .ToList();

            var mirror = axis switch
            {
               LightAxis.X => l.mirrorX,
               LightAxis.Y => l.mirrorY,
               LightAxis.Z => l.mirrorZ,
               _ => false
            };
            DistributeGizmo(list, _ebgs.eventBoxGroupContext.type, axis, mirror, l);
         }
      }
   }

   private void AddFxGizmo()
   {
      var ebg = _bebgdm
         .GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
         .Cast<FxEventBoxEditorData>();

      foreach (var l in _fxManager._floatFxGroups.Where(x =>
                                                           x.groupId == _ebgs.eventBoxGroupContext.groupId))
      {
         var markId = new Dictionary<int, LightTransformData>();
         foreach (var item in ebg.Select((eb, boxIdx) =>
                                            (boxIdx, eb, eb.beatDistributionParam > 0,
                                               IndexFilterHelpers.GetIndexFilterRange(
                                                  eb.indexFilter,
                                                  l.lightGroup.numberOfElements))))
         {
            var (boxIdx, eventBox, distributed, indexFilterIds) = item;
            foreach (var idx in indexFilterIds.Where(i => !markId.ContainsKey(i)))
               markId.Add(
                  idx,
                  new LightTransformData
                  {
                     GlobalBoxIndex = boxIdx, AxisBoxIndex = boxIdx, EventBoxContext = eventBox,
                     Distributed = distributed
                  });
         }

         var list = markId
            .Select(item =>
            {
               var data = item.Value;
               data.Index = item.Key;
               data.Transform = l.targets.Select(t => t.transform).ElementAtOrDefault(item.Key);
               return data;
            })
            .ToList();
         DistributeGizmo(list, _ebgs.eventBoxGroupContext.type, LightAxis.X, false, l);
      }
   }
}

public record struct LightTransformData
{
   public int AxisBoxIndex;
   public bool Distributed;
   public EventBoxEditorData EventBoxContext;
   public int GlobalBoxIndex;
   public int Index;
   public Transform Transform;
}