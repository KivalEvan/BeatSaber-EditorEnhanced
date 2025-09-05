using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.Types;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.Gizmo.Components;
using EditorEnhanced.UI;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
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
   private readonly DiContainer _container;
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
      DiContainer container,
      UIBuilder uiBuilder,
      EditBeatmapViewController ebvc)
   {
      _gizmoAssets = gizmoAssets;
      _signalBus = signalBus;
      _beatmapState = beatmapState;
      _config = config;
      _ebgs = ebgs;
      _bebgdm = bebgdm;
      _ebvc = ebvc;
      _container = container;
      _uiBuilder = uiBuilder;
   }

   public void Dispose()
   {
      _signalBus.TryUnsubscribe<BeatmapEditingModeSwitchedSignal>(HandleGizmoSignalWithSignal);
      _signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<EventBoxModifiedSignal>(HandleGizmoSignal);
      _signalBus.TryUnsubscribe<GizmoRefreshSignal>(HandleGizmoSignal);

      Object.Destroy(_gizmoInfo);
      Object.Destroy(_gizmoDragInputSystem);
      _activeGizmos.Clear();
      _colorManager = null;
      _rotationManager = null;
      _translationManager = null;
      _fxManager = null;
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
         .LayoutStack.Instantiate()
         .SetName("GizmoInfo")
         .SetPreferredWidth(0)
         .SetPreferredHeight(0)
         .SetAnchorMin(new Vector2(0f, 1f))
         .SetAnchorMax(new Vector2(0f, 1f))
         .Create(_ebvc.transform);
      go.SetActive(false);
      _gizmoInfo = _container.InstantiateComponent<GizmoInfo>(go);
      _uiBuilder
         .Text.Instantiate()
         .SetColor(Color.white)
         .SetTextAlignment(TextAlignmentOptions.TopLeft)
         .SetFontSize(12f)
         .SetCharacterSpacing(16f)
         .Create(go.transform);

      _colorManager = Object.FindAnyObjectByType<LightColorGroupEffectManager>();
      _rotationManager =
         Object.FindAnyObjectByType<LightRotationGroupEffectManager>();
      _translationManager =
         Object.FindAnyObjectByType<LightTranslationGroupEffectManager>();
      _fxManager =
         Object.FindAnyObjectByType<FloatFxGroupEffectManager>();

      _signalBus.Subscribe<BeatmapEditingModeSwitchedSignal>(HandleGizmoSignalWithSignal);
      _signalBus.Subscribe<EventBoxesUpdatedSignal>(HandleGizmoSignal);
      _signalBus.Subscribe<EventBoxModifiedSignal>(HandleGizmoSignal);
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
            break;
         case EventBoxGroupType.FloatFx:
            AddFxGizmo();
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      _gizmoInfo.gameObject.SetActive(_config.Gizmo.ShowInfo);
      _gizmoDragInputSystem.gameObject.SetActive(true);
   }

   private void RemoveGizmo()
   {
      foreach (var gizmo in _activeGizmos) gizmo.SetActive(false);
      _activeGizmos.Clear();
      _gizmoInfo.gameObject.SetActive(false);
      _gizmoInfo.Clear();
      _gizmoDragInputSystem.gameObject.SetActive(false);
   }

   private void DistributeGizmo(
      LightTransformData[] ltd,
      EventBoxGroupType groupType,
      LightAxis axis,
      LightGroupSubsystem subsystemContext)
   {
      var onlyUnique = ltd.Select(d => d.AxisBoxIndex).ToHashSet().Count == 1;

      var highlighterMap = new Dictionary<int, GizmoHighlightController>();
      foreach (var data in ltd)
      {
         var globalBoxIdx = data.GlobalBoxIndex;
         var axisBoxIdx = data.AxisBoxIndex;
         var chunkIdx = data.ChunkIndex;
         var eventBoxContext = data.EventBoxContext;
         var distributed = data.Distributed;
         var transform = data.Transform;

         var colorIdx = onlyUnique && !distributed
            ? ColorAssignment.WhiteIndex
            : _config.Gizmo.MulticolorId
               ? ColorAssignment.GetColorIndexEventBox(
                  axisBoxIdx * _config.Gizmo.ColorIdStep,
                  chunkIdx * _config.Gizmo.ColorGradientStep,
                  distributed)
               : ColorAssignment.WhiteIndex;

         if (!highlighterMap.ContainsKey(globalBoxIdx))
         {
            var laneGizmo = _gizmoAssets.GetOrCreate(GizmoType.Lane, colorIdx);
            laneGizmo.transform.SetParent(_colorManager.transform.root, false);
            laneGizmo.GetComponent<GizmoSwappable>().EventBoxEditorDataContext = eventBoxContext;

            var groupHighlightController = laneGizmo.GetComponent<GizmoHighlightController>();
            groupHighlightController.Init();
            groupHighlightController.Add(laneGizmo);
            highlighterMap.Add(globalBoxIdx, groupHighlightController);

            laneGizmo.SetActive(_config.Gizmo.ShowLane);
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

         if (!_config.Gizmo.ShowBase) continue;

         var baseGizmo =
            _gizmoAssets.GetOrCreate(
               _config.Gizmo.DistributeShape && distributed ? GizmoType.Sphere : GizmoType.Cube,
               colorIdx);
         var baseHighlightController = baseGizmo.GetComponentInChildren<GizmoHighlightController>();
         baseHighlightController.SharedWith(highlighterMap[globalBoxIdx]);
         baseHighlightController.Add(baseGizmo);
         baseGizmo
            .GetComponentInChildren<PositionConstraint>()
            .SetSources(
            [
               new ConstraintSource
               {
                  sourceTransform = transform,
                  weight = 1f
               }
            ]);
         baseGizmo
            .GetComponentInChildren<RotationConstraint>()
            .SetSources(
            [
               new ConstraintSource
               {
                  sourceTransform = subsystemContext is LightTranslationGroup ? transform.parent : transform,
                  weight = 1f
               }
            ]);

         var modGizmo = groupType switch
         {
            EventBoxGroupType.Rotation => _gizmoAssets.GetOrCreate(GizmoType.Rotation, axisIdx),
            EventBoxGroupType.Translation => _gizmoAssets.GetOrCreate(GizmoType.Translation, axisIdx),
            _ => null
         };
         if (_config.Gizmo.ShowModifier && modGizmo != null)
         {
            modGizmo.transform.SetParent(baseGizmo.transform.GetChild(0), false);
            var modHighlightController = modGizmo.GetComponent<GizmoHighlightController>();
            modHighlightController.SharedWith(highlighterMap[globalBoxIdx]);
            modHighlightController.Add(modGizmo);

            var gizmoDraggable = modGizmo.GetComponent<GizmoDraggable>();
            gizmoDraggable.EventBoxEditorDataContext = eventBoxContext;
            gizmoDraggable.LightGroupSubsystemContext = subsystemContext;
            gizmoDraggable.Axis = axis;
            gizmoDraggable.TargetTransform = transform;

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

      var selection = _gizmoAssets.GetOrCreate(GizmoType.Selection, ColorAssignment.WhiteIndex);
      selection.SetActive(_config.Gizmo.ShowLane);
      _activeGizmos.Add(selection);
   }

   private void AddColorGizmo()
   {
      var ebg = _bebgdm
         .GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
         .Cast<LightColorEventBoxEditorData>()
         .ToArray();

      foreach (var l in _colorManager.lightGroups
         .Where(x => x.groupId == _ebgs.eventBoxGroupContext.groupId))
      {
         var markId = new Dictionary<int, LightTransformData>();
         foreach (var (boxIdx, eventBox, distributed, indexFilterIds)
            in ebg.Select((eventBox, boxIdx) =>
               (boxIdx, eventBox, eventBox.beatDistributionParam > 0,
                  IndexFilterHelpers.GetIndexFilterRange(
                     eventBox.indexFilter,
                     l.numberOfElements))))
         {
            foreach (var (index, chunkIndex) in indexFilterIds.Where(element => !markId.ContainsKey(element.index)))
            {
               markId.Add(
                  index,
                  new LightTransformData
                  {
                     GlobalBoxIndex = boxIdx,
                     AxisBoxIndex = boxIdx,
                     ChunkIndex = chunkIndex,
                     EventBoxContext = eventBox,
                     Distributed = distributed
                  });
            }
         }

         var ltd = new List<LightTransformData>();
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
                  ltd.Add(item.data with { Transform = matLightWithId.transform });
                  break;
               case TubeBloomPrePassLightWithId tubeBloomPrePassLightWithId:
                  ltd.Add(item.data with { Transform = tubeBloomPrePassLightWithId.transform });
                  break;
            }

         DistributeGizmo(ltd.ToArray(), _ebgs.eventBoxGroupContext.type, LightAxis.X, null);
      }
   }

   private void AddRotationGizmo()
   {
      var ebg = _bebgdm
         .GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
         .Cast<LightRotationEventBoxEditorData>()
         .ToArray();

      foreach (var l in _rotationManager._lightRotationGroups.Where(x =>
         x.groupId == _ebgs.eventBoxGroupContext.groupId))
      {
         var axisCount = new Dictionary<LightAxis, int>
            { { LightAxis.X, 0 }, { LightAxis.Y, 0 }, { LightAxis.Z, 0 } };
         var markXIdx = new Dictionary<int, LightTransformData>();
         var markYIdx = new Dictionary<int, LightTransformData>();
         var markZIdx = new Dictionary<int, LightTransformData>();
         foreach (var (boxIdx, eventBox, distributed, indexFilterIds, axis)
            in ebg.Select((eventBox, boxIdx) =>
               (boxIdx, eventBox, eventBox.beatDistributionParam > 0,
                  IndexFilterHelpers.GetIndexFilterRange(
                     eventBox.indexFilter,
                     l.lightGroup.numberOfElements), eventBox.axis)))
         {
            var putTo = axis switch
            {
               LightAxis.X => markXIdx,
               LightAxis.Y => markYIdx,
               LightAxis.Z => markZIdx,
               _ => throw new ArgumentOutOfRangeException()
            };
            foreach (var (index, chunkIndex) in indexFilterIds.Where(element => !putTo.ContainsKey(element.index)))
            {
               putTo.Add(
                  index,
                  new LightTransformData
                  {
                     GlobalBoxIndex = boxIdx,
                     AxisBoxIndex = axisCount[axis],
                     ChunkIndex = chunkIndex,
                     EventBoxContext = eventBox,
                     Distributed = distributed
                  });
            }

            axisCount[axis]++;
         }

         var transformsXYZ = new[] { l.xTransforms, l.yTransforms, l.zTransforms };
         var markIdXYZ = new[] { markXIdx, markYIdx, markZIdx };
         foreach (LightAxis axis in Enum.GetValues(typeof(LightAxis)))
         {
            var transforms = transformsXYZ[(int)axis];
            var markId = markIdXYZ[(int)axis];
            var ltd = markId
               .Select(item =>
               {
                  var data = item.Value;
                  data.Index = item.Key;
                  data.Transform = transforms.ElementAtOrDefault(item.Key);
                  return data;
               })
               .ToArray();
            DistributeGizmo(ltd, _ebgs.eventBoxGroupContext.type, axis, l);
         }
      }
   }

   private void AddTranslationGizmo()
   {
      var ebg = _bebgdm
         .GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
         .Cast<LightTranslationEventBoxEditorData>()
         .ToArray();

      foreach (var l in _translationManager._lightTranslationGroups.Where(x =>
         x.groupId
         == _ebgs.eventBoxGroupContext.groupId))
      {
         var axisCount = new Dictionary<LightAxis, int>
            { { LightAxis.X, 0 }, { LightAxis.Y, 0 }, { LightAxis.Z, 0 } };
         var markXIdx = new Dictionary<int, LightTransformData>();
         var markYIdx = new Dictionary<int, LightTransformData>();
         var markZIdx = new Dictionary<int, LightTransformData>();
         foreach (var (boxIdx, eventBox, distributed, indexFilterIds, axis)
            in ebg.Select((eventBox, boxIdx) =>
               (boxIdx, eventBox, eventBox.beatDistributionParam > 0,
                  IndexFilterHelpers.GetIndexFilterRange(
                     eventBox.indexFilter,
                     l.lightGroup.numberOfElements), eventBox.axis)))
         {
            var putTo = axis switch
            {
               LightAxis.X => markXIdx,
               LightAxis.Y => markYIdx,
               LightAxis.Z => markZIdx,
               _ => throw new ArgumentOutOfRangeException()
            };
            foreach (var (index, chunkIndex) in indexFilterIds.Where(element => !putTo.ContainsKey(element.index)))
            {
               putTo.Add(
                  index,
                  new LightTransformData
                  {
                     GlobalBoxIndex = boxIdx,
                     AxisBoxIndex = axisCount[axis],
                     ChunkIndex = chunkIndex,
                     EventBoxContext = eventBox,
                     Distributed = distributed
                  });
            }

            axisCount[axis]++;
         }

         var transformsXYZ = new[] { l.xTransforms, l.yTransforms, l.zTransforms };
         var markIdXYZ = new[] { markXIdx, markYIdx, markZIdx };
         foreach (LightAxis axis in Enum.GetValues(typeof(LightAxis)))
         {
            var transforms = transformsXYZ[(int)axis];
            var markId = markIdXYZ[(int)axis];
            var ltd = markId
               .Select(item =>
               {
                  var data = item.Value;
                  data.Index = item.Key;
                  data.Transform = transforms.ElementAtOrDefault(item.Key);
                  return data;
               })
               .ToArray();
            DistributeGizmo(ltd, _ebgs.eventBoxGroupContext.type, axis, l);
         }
      }
   }

   private void AddFxGizmo()
   {
      var eventBoxGroup = _bebgdm
         .GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
         .Cast<FxEventBoxEditorData>()
         .ToArray();

      foreach (var l in _fxManager._floatFxGroups.Where(x =>
         x.groupId == _ebgs.eventBoxGroupContext.groupId))
      {
         var markId = new Dictionary<int, LightTransformData>();
         foreach (var (boxIdx, eventBox, distributed, indexFilterIds)
            in eventBoxGroup.Select((eventBox, boxIdx) =>
               (boxIdx, eventBox, eventBox.beatDistributionParam > 0,
                  IndexFilterHelpers.GetIndexFilterRange(
                     eventBox.indexFilter,
                     l.lightGroup.numberOfElements))))
         {
            foreach (var (index, chunkIndex) in indexFilterIds.Where(element => !markId.ContainsKey(element.index)))
               markId.Add(
                  index,
                  new LightTransformData
                  {
                     GlobalBoxIndex = boxIdx,
                     AxisBoxIndex = boxIdx,
                     ChunkIndex = chunkIndex,
                     EventBoxContext = eventBox,
                     Distributed = distributed
                  });
         }

         var ltd = markId
            .Select(item =>
            {
               var data = item.Value;
               data.Transform = l.targets.Select(t => t.transform).ElementAtOrDefault(item.Key);
               return data;
            })
            .ToArray();
         DistributeGizmo(ltd, _ebgs.eventBoxGroupContext.type, LightAxis.X, l);
      }
   }
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