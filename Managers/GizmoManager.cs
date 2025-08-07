using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.Types;
using EditorEnhanced.Commands;
using EditorEnhanced.Configurations;
using EditorEnhanced.Gizmo;
using EditorEnhanced.Helpers;
using EditorEnhanced.Utils;
using UnityEngine;
using Zenject;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;
using Object = UnityEngine.Object;

namespace EditorEnhanced.Managers;

internal class GizmoManager : IInitializable, IDisposable
{
    private readonly BeatmapEventBoxGroupsDataModel _bebgdm;
    private readonly EventBoxGroupsState _ebgs;
    private readonly GizmoAssets _gizmoAssets;
    private readonly SignalBus _signalBus;
    private readonly PluginConfig _config;

    private readonly List<GameObject> _activeGizmos = [];
    private LightColorGroupEffectManager _colorManager;
    private FloatFxGroupEffectManager _fxManager;

    private GizmoDragInputSystem _gizmoDragInputSystem;
    private LightRotationGroupEffectManager _rotationManager;
    private LightTranslationGroupEffectManager _translationManager;
    
    public GizmoManager(GizmoAssets gizmoAssets,
        SignalBus signalBus,
        PluginConfig config,
        EventBoxGroupsState ebgs,
        BeatmapEventBoxGroupsDataModel bebgdm)
    {
        _gizmoAssets = gizmoAssets;
        _signalBus = signalBus;
        _config = config;
        _ebgs = ebgs;
        _bebgdm = bebgdm;
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<BeatmapEditingModeSwitchedSignal>(UpdateGizmoWithSignal);
        _signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(UpdateGizmo);
        _signalBus.TryUnsubscribe<ModifyEventBoxSignal>(UpdateGizmo);
        _signalBus.TryUnsubscribe<InsertEventBoxSignal>(UpdateGizmo);
        _signalBus.TryUnsubscribe<InsertEventBoxesForAllAxesSignal>(UpdateGizmo);
        _signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsSignal>(UpdateGizmo);
        _signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsAndAxisSignal>(UpdateGizmo);
        _signalBus.TryUnsubscribe<DeleteEventBoxSignal>(UpdateGizmo);
        _signalBus.TryUnsubscribe<SortAxisEventBoxGroupSignal>(UpdateGizmo);
        _signalBus.TryUnsubscribe<SortIdEventBoxGroupSignal>(UpdateGizmo);
        _signalBus.TryUnsubscribe<GizmoUpdateSignal>(UpdateGizmo);

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

        _colorManager = Object.FindAnyObjectByType<LightColorGroupEffectManager>();
        _rotationManager =
            Object.FindAnyObjectByType<LightRotationGroupEffectManager>();
        _translationManager =
            Object.FindAnyObjectByType<LightTranslationGroupEffectManager>();
        _fxManager =
            Object.FindAnyObjectByType<FloatFxGroupEffectManager>();

        _signalBus.Subscribe<BeatmapEditingModeSwitchedSignal>(UpdateGizmoWithSignal);
        _signalBus.Subscribe<EventBoxesUpdatedSignal>(UpdateGizmo);
        _signalBus.Subscribe<ModifyEventBoxSignal>(UpdateGizmo);
        _signalBus.Subscribe<InsertEventBoxSignal>(UpdateGizmo);
        _signalBus.Subscribe<InsertEventBoxesForAllAxesSignal>(UpdateGizmo);
        _signalBus.Subscribe<InsertEventBoxesForAllIdsSignal>(UpdateGizmo);
        _signalBus.Subscribe<InsertEventBoxesForAllIdsAndAxisSignal>(UpdateGizmo);
        _signalBus.Subscribe<DeleteEventBoxSignal>(UpdateGizmo);
        _signalBus.Subscribe<SortAxisEventBoxGroupSignal>(UpdateGizmo);
        _signalBus.Subscribe<SortIdEventBoxGroupSignal>(UpdateGizmo);
        _signalBus.Subscribe<GizmoUpdateSignal>(UpdateGizmo);
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
            default: throw new ArgumentOutOfRangeException();
        }

        _gizmoDragInputSystem.gameObject.SetActive(true);
    }

    private void RemoveGizmo()
    {
        foreach (var gizmo in _activeGizmos)
            gizmo.SetActive(false);
        _activeGizmos.Clear();
        _gizmoDragInputSystem.gameObject.SetActive(false);
    }

    private void UpdateGizmoWithSignal(BeatmapEditingModeSwitchedSignal signal)
    {
        if (_config.GizmoEnabled && signal.mode == BeatmapEditingMode.EventBoxes) AddGizmo();
        else RemoveGizmo();
    }

    private void UpdateGizmo()
    {
        RemoveGizmo();
        if (!_config.GizmoEnabled) return;
        AddGizmo();
    }

    private void DistributeGizmo(
        IEnumerable<LightTransformData> list,
        EventBoxGroupType groupType,
        LightAxis axis,
        bool mirror, int maxCount, LightGroupSubsystem subsystemContext)
    {
        var onlyUnique = list.Select(d => d.AxisBoxIndex).ToHashSet().Count == 1;

        var highlighterMap = new Dictionary<(LightAxis, int), GizmoHighlighterGroup>();
        foreach (var data in list)
        {
            var idx = data.Index;
            var globalBoxIdx = data.GlobalBoxIndex;
            var axisBoxIdx = data.AxisBoxIndex;
            var eventBoxContext = data.EventBoxContext;
            var distributed = data.Distributed;
            var transform = data.Transform;

            if (!onlyUnique) axisBoxIdx += 1;
            var colorIdx = ColorAssignment.GetColorIndexEventBox(axisBoxIdx, idx, distributed);
            Vector3 localScale;
            Vector3 lossyScale;

            if (!highlighterMap.ContainsKey((axis, globalBoxIdx)))
            {
                var laneGizmo = _gizmoAssets.GetOrCreate(GizmoType.Lane, colorIdx);
                laneGizmo.transform.SetParent(_colorManager.transform.root, false);
                laneGizmo.GetComponent<GizmoSwappable>().EventBoxEditorDataContext = eventBoxContext;
                
                var groupHighlighter = laneGizmo.GetComponent<GizmoHighlighterGroup>();
                groupHighlighter.Init();
                groupHighlighter.Add(laneGizmo);
                highlighterMap.Add((axis, globalBoxIdx), groupHighlighter);

                laneGizmo.SetActive(true);
                _activeGizmos.Add(laneGizmo);
            }

            if (transform == null) continue;
            var axisIdx = axis switch
            {
                LightAxis.X => ColorAssignment.RedIndex,
                LightAxis.Y => ColorAssignment.GreenIndex,
                LightAxis.Z => ColorAssignment.BlueIndex,
                _ => ColorAssignment.WhiteIndex
            };

            var baseGizmo = _gizmoAssets.GetOrCreate(distributed ? GizmoType.Sphere : GizmoType.Cube, colorIdx);
            var baseGroupHighlighter = baseGizmo.GetComponent<GizmoHighlighterGroup>();
            baseGroupHighlighter.SharedWith(highlighterMap[(axis, globalBoxIdx)]);
            baseGroupHighlighter.Add(baseGizmo);
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
                var modGroupHighlighter = modGizmo.GetComponent<GizmoHighlighterGroup>();
                modGroupHighlighter.SharedWith(highlighterMap[(axis, globalBoxIdx)]);
                modGroupHighlighter.Add(modGizmo);

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
        var ebg = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
            .Cast<LightColorEventBoxEditorData>();
        var max = ebg.Count();

        foreach (var l in _colorManager.lightGroups
                     .Where(x => x.groupId == _ebgs.eventBoxGroupContext.groupId))
        {
            var markId = new Dictionary<int, LightTransformData>();
            foreach (var item in ebg.Select((eb, boxIdx) =>
                         (boxIdx, eb, eb.beatDistributionParam > 0,
                             IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.numberOfElements))))
            {
                var (boxIdx, eventBox, distributed, indexFilterIds) = item;
                foreach (var idx in indexFilterIds.Where(i => !markId.ContainsKey(i)))
                    markId.Add(idx, new LightTransformData
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

            DistributeGizmo(list, _ebgs.eventBoxGroupContext.type, LightAxis.X, false, max, null);
        }
    }

    private void AddRotationGizmo()
    {
        var ebg = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
            .Cast<LightRotationEventBoxEditorData>();
        var max = ebg.Count();

        foreach (var l in _rotationManager._lightRotationGroups.Where(x =>
                     x.groupId == _ebgs.eventBoxGroupContext.groupId))
        {
            var axisCount = new Dictionary<LightAxis, int>
                { { LightAxis.X, 0 }, { LightAxis.Y, 0 }, { LightAxis.Z, 0 } };
            var markXIdx = new Dictionary<int, LightTransformData>();
            var markYIdx = new Dictionary<int, LightTransformData>();
            var markZIdx = new Dictionary<int, LightTransformData>();
            foreach (var item in ebg.Select((eb, boxIdx) =>
                         (boxIdx, eb, eb.beatDistributionParam > 0, IndexFilterHelpers.GetIndexFilterRange(
                             eb.indexFilter, l.lightGroup.numberOfElements), eb.axis)))
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
                    putTo.Add(i, new LightTransformData
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
                    });

                var mirror = axis switch
                {
                    LightAxis.X => l.mirrorX,
                    LightAxis.Y => l.mirrorY,
                    LightAxis.Z => l.mirrorZ,
                    _ => false
                };
                DistributeGizmo(list, _ebgs.eventBoxGroupContext.type, axis, mirror, max, l);
            }
        }
    }

    private void AddTranslationGizmo()
    {
        var ebg = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
            .Cast<LightTranslationEventBoxEditorData>();
        var max = ebg.Count();

        foreach (var l in _translationManager._lightTranslationGroups.Where(x =>
                     x.groupId == _ebgs.eventBoxGroupContext.groupId))
        {
            var axisCount = new Dictionary<LightAxis, int>
                { { LightAxis.X, 0 }, { LightAxis.Y, 0 }, { LightAxis.Z, 0 } };
            var markXIdx = new Dictionary<int, LightTransformData>();
            var markYIdx = new Dictionary<int, LightTransformData>();
            var markZIdx = new Dictionary<int, LightTransformData>();
            foreach (var item in ebg.Select((eb, boxIdx) =>
                         (boxIdx, eb, eb.beatDistributionParam > 0, IndexFilterHelpers.GetIndexFilterRange(
                             eb.indexFilter, l.lightGroup.numberOfElements), eb.axis)))
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
                    putTo.Add(i, new LightTransformData
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
                    });

                var mirror = axis switch
                {
                    LightAxis.X => l.mirrorX,
                    LightAxis.Y => l.mirrorY,
                    LightAxis.Z => l.mirrorZ,
                    _ => false
                };
                DistributeGizmo(list, _ebgs.eventBoxGroupContext.type, axis, mirror, max, l);
            }
        }
    }

    private void AddFxGizmo()
    {
        var ebg = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
            .Cast<FxEventBoxEditorData>();
        var max = ebg.Count();

        foreach (var l in _fxManager._floatFxGroups.Where(x =>
                     x.groupId == _ebgs.eventBoxGroupContext.groupId))
        {
            var markId = new Dictionary<int, LightTransformData>();
            foreach (var item in ebg.Select((eb, boxIdx) =>
                         (boxIdx, eb, eb.beatDistributionParam > 0,
                             IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.lightGroup.numberOfElements))))
            {
                var (boxIdx, eventBox, distributed, indexFilterIds) = item;
                foreach (var idx in indexFilterIds.Where(i => !markId.ContainsKey(i)))
                    markId.Add(idx, new LightTransformData
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
                });
            DistributeGizmo(list, _ebgs.eventBoxGroupContext.type, LightAxis.X, false, max, l);
        }
    }
}

internal record struct LightTransformData
{
    public int AxisBoxIndex;
    public bool Distributed;
    public EventBoxEditorData EventBoxContext;
    public int GlobalBoxIndex;
    public int Index;
    public Transform Transform;
}