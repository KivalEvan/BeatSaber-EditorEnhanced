using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.Types;
using EditorEnhanced.Gizmo;
using EditorEnhanced.Helpers;
using EditorEnhanced.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
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

    private readonly List<GameObject> _gizmos = [];
    private LightColorGroupEffectManager _colorManager;
    private FloatFxGroupEffectManager _fxManager;
    private LightRotationGroupEffectManager _rotationManager;
    private LightTranslationGroupEffectManager _translationManager;

    private GizmoDragInputSystem _gizmoDragInputSystem;

    public GizmoManager(GizmoAssets gizmoAssets,
        SignalBus signalBus,
        EventBoxGroupsState ebgs,
        BeatmapEventBoxGroupsDataModel bebgdm)
    {
        _gizmoAssets = gizmoAssets;
        _signalBus = signalBus;
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

        _gizmos.Clear();
        _colorManager = null;
        _rotationManager = null;
        _translationManager = null;
        _fxManager = null;
        _gizmoDragInputSystem = null;
    }

    public void Initialize()
    {
        var go = new GameObject();
        go.name = "GizmoDragInputSystem";
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
        foreach (var gizmo in _gizmos)
            gizmo.SetActive(false);
        _gizmos.Clear();
        _gizmoDragInputSystem.gameObject.SetActive(false);
    }

    private void UpdateGizmoWithSignal(BeatmapEditingModeSwitchedSignal signal)
    {
        if (signal.mode == BeatmapEditingMode.EventBoxes) AddGizmo();
        else RemoveGizmo();
    }

    private void UpdateGizmo()
    {
        RemoveGizmo();
        AddGizmo();
    }

    private void DoTheFunny(
        IEnumerable<LightTransformData> list,
        EventBoxGroupType groupType,
        LightAxis axis,
        bool mirror, int maxCount, LightGroupSubsystem subsystemContext)
    {
        var highlighterMap = new Dictionary<(LightAxis, int), GizmoHighlighterGroup>();
        foreach (var data in list)
        {
            var idx = data.Index;
            var globalBoxIdx = data.GlobalBoxIndex;
            var axisBoxIdx = data.AxisBoxIndex;
            var eventBoxContext = data.EventBoxContext;
            var distributed = data.Distributed;
            var transform = data.Transform;

            var colorIdx = ColorAssignment.GetColorIndexEventBox(axisBoxIdx, idx, distributed);
            Vector3 localScale;
            Vector3 lossyScale;

            if (!highlighterMap.ContainsKey((axis, globalBoxIdx)))
            {
                var laneGizmo = _gizmoAssets.GetOrCreate(distributed ? GizmoType.Sphere : GizmoType.Cube, colorIdx);
                laneGizmo.transform.SetParent(_colorManager.transform.root, false);
                laneGizmo.transform.localPosition = new Vector3((globalBoxIdx - (maxCount - 1) / 2f) / 2f, -0.1f, 0f);
                laneGizmo.transform.rotation = Quaternion.identity;

                var groupHighlighter = laneGizmo.GetComponent<GizmoHighlighterGroup>();
                groupHighlighter.Clear();
                groupHighlighter.Add(laneGizmo);

                localScale = laneGizmo.transform.localScale;
                lossyScale = laneGizmo.transform.lossyScale;
                laneGizmo.transform.localScale = new Vector3(localScale.x / lossyScale.x * 0.333f,
                    localScale.y / lossyScale.y * 0.1f, localScale.z / lossyScale.z * 0.1f);

                _gizmos.Add(laneGizmo);
                highlighterMap.Add((axis, globalBoxIdx), groupHighlighter);
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
            highlighterMap[(axis, globalBoxIdx)].Add(baseGizmo);

            baseGizmo.transform.SetParent(transform.parent.transform, false);
            baseGizmo.transform.position = transform.position;
            baseGizmo.transform.rotation = transform.parent.rotation;
            localScale = baseGizmo.transform.localScale;
            lossyScale = baseGizmo.transform.lossyScale;
            baseGizmo.transform.localScale = new Vector3(localScale.x / lossyScale.x * 0.5f,
                localScale.y / lossyScale.y * 0.5f, localScale.z / lossyScale.z * 0.5f);
            baseGizmo.transform.SetParent(transform.transform, true);

            var modGizmo = groupType switch
            {
                EventBoxGroupType.Rotation => _gizmoAssets.GetOrCreate(GizmoType.Rotation, axisIdx),
                EventBoxGroupType.Translation => _gizmoAssets.GetOrCreate(GizmoType.Translation, axisIdx),
                _ => null
            };
            if (modGizmo != null)
            {
                var modGroupHighlighter = modGizmo.GetComponent<GizmoHighlighterGroup>();
                modGroupHighlighter.SharedWith(highlighterMap[(axis, globalBoxIdx)]);
                highlighterMap[(axis, globalBoxIdx)].Add(modGizmo);
                modGizmo.transform.SetParent(baseGizmo.transform, false);

                localScale = modGizmo.transform.localScale;
                lossyScale = modGizmo.transform.lossyScale;
                modGizmo.transform.localScale = new Vector3(localScale.x / lossyScale.x * 2f,
                    localScale.y / lossyScale.y * 2f, localScale.z / lossyScale.z * 2f);

                if (groupType == EventBoxGroupType.Translation)
                {
                    modGizmo.transform.localPosition = axis switch
                    {
                        LightAxis.X => mirror ? new Vector3(-.75f, 0f, 0f) : new Vector3(.75f, 0f, 0f),
                        LightAxis.Y => mirror ? new Vector3(0f, -.75f, 0f) : new Vector3(0f, .75f, 0f),
                        LightAxis.Z => mirror ? new Vector3(0f, 0f, -.75f) : new Vector3(0f, 0f, .75f),
                        _ => Vector3.zero
                    };
                }

                modGizmo.transform.localRotation = groupType switch
                {
                    EventBoxGroupType.Translation or EventBoxGroupType.Rotation => axis switch
                    {
                        LightAxis.X => mirror ? Quaternion.Euler(0, 270, 0) : Quaternion.Euler(0, 90, 0),
                        LightAxis.Y => mirror ? Quaternion.Euler(90, 0, 0) : Quaternion.Euler(270, 0, 0),
                        LightAxis.Z => mirror ? Quaternion.Euler(180, 0, 0) : Quaternion.Euler(0, 0, 0),
                        _ => Quaternion.identity
                    },
                    _ => modGizmo.transform.localRotation
                };

                var gizmoDraggable = modGizmo.GetComponent<GizmoDraggable>();
                gizmoDraggable.EventBoxEditorDataContext = eventBoxContext;
                gizmoDraggable.LightGroupSubsystemContext = subsystemContext;
                gizmoDraggable.Axis = axis;
                gizmoDraggable.TargetTransform = transform;
                gizmoDraggable.Mirror = mirror;

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

            _gizmos.Add(baseGizmo);
            if (modGizmo != null) _gizmos.Add(modGizmo);
        }
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
                {
                    markId.Add(idx, new LightTransformData
                    {
                        GlobalBoxIndex = boxIdx, AxisBoxIndex = boxIdx, EventBoxContext = eventBox,
                        Distributed = distributed
                    });
                }
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
            {
                foreach (var lightWithId in item.LightWithIds)
                {
                    switch (lightWithId)
                    {
                        case MaterialLightWithId matLightWithId:
                            list.Add(item.data with { Transform = matLightWithId.transform });
                            break;
                        case TubeBloomPrePassLightWithId tubeBloomPrePassLightWithId:
                            list.Add(item.data with { Transform = tubeBloomPrePassLightWithId.transform });
                            break;
                    }
                }
            }

            DoTheFunny(list, _ebgs.eventBoxGroupContext.type, LightAxis.X, false, max, null);
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
                {
                    putTo.Add(i, new LightTransformData
                    {
                        GlobalBoxIndex = boxIdx, AxisBoxIndex = axisCount[axis], EventBoxContext = eventBox,
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
                DoTheFunny(list, _ebgs.eventBoxGroupContext.type, axis, mirror, max, l);
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
                {
                    putTo.Add(i, new LightTransformData
                    {
                        GlobalBoxIndex = boxIdx, AxisBoxIndex = axisCount[axis], EventBoxContext = eventBox,
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
                DoTheFunny(list, _ebgs.eventBoxGroupContext.type, axis, mirror, max, l);
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
                {
                    markId.Add(idx, new LightTransformData
                    {
                        GlobalBoxIndex = boxIdx, AxisBoxIndex = boxIdx, EventBoxContext = eventBox,
                        Distributed = distributed
                    });
                }
            }

            var list = markId
                .Select(item =>
                {
                    var data = item.Value;
                    data.Index = item.Key;
                    data.Transform = l.targets.Select(t => t.transform).ElementAtOrDefault(item.Key);
                    return data;
                });
            DoTheFunny(list, _ebgs.eventBoxGroupContext.type, LightAxis.X, false, max, l);
        }
    }
}

internal record struct LightTransformData
{
    public int Index;
    public int GlobalBoxIndex;
    public int AxisBoxIndex;
    public bool Distributed;
    public EventBoxEditorData EventBoxContext;
    public Transform Transform;
}