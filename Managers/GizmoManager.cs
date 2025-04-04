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
using UnityEngine;
using Zenject;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.Managers;

internal class GizmoManager(
    GizmoAssets gizmoAssets,
    SignalBus signalBus,
    EventBoxGroupsState ebgs,
    BeatmapEventBoxGroupsDataModel bebgdm)
    : IInitializable, IDisposable
{
    private LightColorGroupEffectManager _colorManager;
    private LightRotationGroupEffectManager _rotationManager;
    private LightTranslationGroupEffectManager _translationManager;
    private FloatFxGroupEffectManager _fxManager;
    private readonly List<GameObject> _gameObjects = [];

    private static T TryGetManager<T>(string[] objNames) where T : MonoBehaviour
    {
        return (from name in objNames
            select GameObject.Find(name)
            into gameObject
            where gameObject != null
            select gameObject.GetComponent<T>()).FirstOrDefault();
    }

    public void Initialize()
    {
        _colorManager = TryGetManager<LightColorGroupEffectManager>(["Environment/LightColorGroupEffectManager"]);
        _rotationManager =
            TryGetManager<LightRotationGroupEffectManager>(["Environment/LightRotationGroupEffectManager"]);
        _translationManager =
            TryGetManager<LightTranslationGroupEffectManager>(["Environment/LightTranslationGroupEffectManager"]);
        _fxManager =
            TryGetManager<FloatFxGroupEffectManager>([
                "Environment/VfxGroupEffectManager", "Environment/FloatFxGroupEffectManager"
            ]);

        signalBus.Subscribe<BeatmapEditingModeSwitched>(UpdateGizmoWithSignal);
        signalBus.Subscribe<EventBoxesUpdatedSignal>(UpdateGizmo);
        signalBus.Subscribe<ModifyEventBoxSignal>(UpdateGizmo);
        signalBus.Subscribe<InsertEventBoxSignal>(UpdateGizmo);
        signalBus.Subscribe<InsertEventBoxesForAllAxesSignal>(UpdateGizmo);
        signalBus.Subscribe<InsertEventBoxesForAllIdsSignal>(UpdateGizmo);
        signalBus.Subscribe<InsertEventBoxesForAllIdsAndAxisSignal>(UpdateGizmo);
        signalBus.Subscribe<DeleteEventBoxSignal>(UpdateGizmo);
    }

    public void Dispose()
    {
        signalBus.TryUnsubscribe<BeatmapEditingModeSwitched>(UpdateGizmoWithSignal);
        signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(UpdateGizmo);
        signalBus.TryUnsubscribe<ModifyEventBoxSignal>(UpdateGizmo);
        signalBus.TryUnsubscribe<InsertEventBoxSignal>(UpdateGizmo);
        signalBus.TryUnsubscribe<InsertEventBoxesForAllAxesSignal>(UpdateGizmo);
        signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsSignal>(UpdateGizmo);
        signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsAndAxisSignal>(UpdateGizmo);
        signalBus.TryUnsubscribe<DeleteEventBoxSignal>(UpdateGizmo);
    }

    private void AddGizmo()
    {
        switch (ebgs.eventBoxGroupContext.type)
        {
            case EventBoxGroupType.Color:
                AddColorGizmo();
                return;
            case EventBoxGroupType.Rotation:
                AddRotationGizmo();
                return;
            case EventBoxGroupType.Translation:
                AddTranslationGizmo();
                return;
            case EventBoxGroupType.FloatFx:
                AddFxGizmo();
                return;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private void RemoveGizmo()
    {
        foreach (var gameObject in _gameObjects)
            gameObject.SetActive(false);
        _gameObjects.Clear();
    }

    private void UpdateGizmoWithSignal(BeatmapEditingModeSwitched signal)
    {
        if (signal.mode != BeatmapEditingMode.EventBoxes) RemoveGizmo();
        if (signal.mode == BeatmapEditingMode.EventBoxes) AddGizmo();
    }

    private void UpdateGizmo()
    {
        RemoveGizmo();
        AddGizmo();
    }

    private void DoTheFunny(IEnumerable<(int index, int offset, bool distributed, Transform transform)> data,
        EventBoxGroupType groupType,
        LightAxis axis,
        bool mirror)
    {
        foreach (var items in data)
        {
            var (idx, offset, distributed, transform) = items;
            var colorIdx = distributed
                ? GizmoAssets.HUE_RANGE / 2 * offset + idx * 2 % GizmoAssets.HUE_RANGE
                : (GizmoAssets.WHITE_INDEX + offset * GizmoAssets.HUE_RANGE / 12) % GizmoAssets.HUE_RANGE;

            var axisIdx = axis switch
            {
                LightAxis.X => GizmoAssets.RED_INDEX,
                LightAxis.Y => GizmoAssets.GREEN_INDEX,
                LightAxis.Z => GizmoAssets.BLUE_INDEX,
                _ => GizmoAssets.WHITE_INDEX
            };

            var baseGizmo = gizmoAssets.GetOrCreate(distributed ? GizmoType.Sphere : GizmoType.Cube, colorIdx);
            baseGizmo.transform.SetParent(transform.transform, false);
            var modGizmo = groupType switch
            {
                EventBoxGroupType.Rotation => gizmoAssets.GetOrCreate(GizmoType.Rotation, axisIdx),
                EventBoxGroupType.Translation => gizmoAssets.GetOrCreate(GizmoType.Translation, axisIdx),
                _ => null
            };
            if (modGizmo != null)
            {
                modGizmo.transform.SetParent(baseGizmo.transform, false);
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
            }

            _gameObjects.Add(baseGizmo);
            if (modGizmo != null) _gameObjects.Add(modGizmo);
        }
    }

    private void AddColorGizmo()
    {
        var ebg = bebgdm.GetEventBoxesByEventBoxGroupId(ebgs.eventBoxGroupContext.id)
            .Cast<LightColorEventBoxEditorData>().ToList();

        foreach (var l in _colorManager.lightGroups
                     .Where(x => x.groupId == ebgs.eventBoxGroupContext.groupId))
        {
            var markId = new Dictionary<int, (int offset, bool distributed)>();
            foreach (var item in ebg.Select((eb, i) =>
                         (i, eb.beatDistributionParam > 0,
                             IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.numberOfElements))))
            {
                var (offset, distributed, list) = item;
                foreach (var i in list.Where(i => !markId.ContainsKey(i)))
                {
                    markId.Add(i, (offset, distributed));
                }
            }

            var data = new List<(int index, int offset, bool distributed, Transform transform)>();
            foreach (var item in markId
                         .Select(i => (i.Value.offset, i.Value.distributed, _colorManager
                             ._lightColorGroupEffects
                             .FirstOrDefault(x => x._lightId == l.startLightId + i.Key)
                             ?._lightManager._lights.ElementAt(l.startLightId + i.Key)))
                         .Where(item => item.Item3 != null)
                         .Select((l, i) => (i, l.Item1, l.Item2, l.Item3)))
            {
                foreach (var lightWithId in item.Item4)
                {
                    switch (lightWithId)
                    {
                        case MaterialLightWithId matLightWithId:
                            data.Add((item.Item1, item.Item2, item.Item3, matLightWithId.transform));
                            break;
                        case TubeBloomPrePassLightWithId tubeBloomPrePassLightWithId:
                            data.Add((item.Item1, item.Item2, item.Item3, tubeBloomPrePassLightWithId.transform));
                            break;
                    }
                }
            }

            DoTheFunny(data, ebgs.eventBoxGroupContext.type, LightAxis.X, false);
        }
    }

    private void AddRotationGizmo()
    {
        var ebg = bebgdm.GetEventBoxesByEventBoxGroupId(ebgs.eventBoxGroupContext.id)
            .Cast<LightRotationEventBoxEditorData>().ToList();

        // foreach (var l in manager._lightTranslationGroups)
        foreach (var l in _rotationManager._lightRotationGroups.Where(x =>
                     x.groupId == ebgs.eventBoxGroupContext.groupId))
        {
            var markXIdx = new Dictionary<int, (int offset, bool distributed)>();
            var markYIdx = new Dictionary<int, (int offset, bool distributed)>();
            var markZIdx = new Dictionary<int, (int offset, bool distributed)>();
            foreach (var eb in ebg)
            {
                var putTo = eb.axis switch
                {
                    LightAxis.X => markXIdx,
                    LightAxis.Y => markYIdx,
                    LightAxis.Z => markZIdx,
                    _ => throw new ArgumentOutOfRangeException()
                };
                foreach (var item in ebg.Select((eb, i) =>
                             (i, eb.beatDistributionParam > 0,
                                 IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter,
                                     l.lightGroup.numberOfElements))))
                {
                    var (offset, distributed, list) = item;
                    foreach (var i in list.Where(i => !putTo.ContainsKey(i)))
                    {
                        putTo.Add(i, (offset, distributed));
                    }
                }
            }

            var transformsXYZ = new[] { l.xTransforms, l.yTransforms, l.zTransforms };
            var markIdXYZ = new[] { markXIdx, markYIdx, markZIdx };
            foreach (LightAxis axis in Enum.GetValues(typeof(LightAxis)))
            {
                var transforms = transformsXYZ[(int)axis];
                var markId = markIdXYZ[(int)axis];
                var data = markId
                    .Select(item => (item.Key, item.Value.offset, item.Value.distributed,
                        transforms.ElementAtOrDefault(item.Key))).ToList();

                var mirror = axis switch
                {
                    LightAxis.X => l.mirrorX,
                    LightAxis.Y => l.mirrorY,
                    LightAxis.Z => l.mirrorZ,
                    _ => false
                };
                DoTheFunny(data, ebgs.eventBoxGroupContext.type, axis, mirror);
            }
        }
    }

    private void AddTranslationGizmo()
    {
        var ebg = bebgdm.GetEventBoxesByEventBoxGroupId(ebgs.eventBoxGroupContext.id)
            .Cast<LightTranslationEventBoxEditorData>().ToList();

        // foreach (var l in manager._lightTranslationGroups)
        foreach (var l in _translationManager._lightTranslationGroups.Where(x =>
                     x.groupId == ebgs.eventBoxGroupContext.groupId))
        {
            var markXIdx = new Dictionary<int, (int offset, bool distributed)>();
            var markYIdx = new Dictionary<int, (int offset, bool distributed)>();
            var markZIdx = new Dictionary<int, (int offset, bool distributed)>();
            foreach (var eb in ebg)
            {
                var r = IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.lightGroup.numberOfElements);
                var putTo = eb.axis switch
                {
                    LightAxis.X => markXIdx,
                    LightAxis.Y => markYIdx,
                    LightAxis.Z => markZIdx,
                    _ => throw new ArgumentOutOfRangeException()
                };
                foreach (var item in ebg.Select((eb, i) =>
                             (i, eb.beatDistributionParam > 0,
                                 IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter,
                                     l.lightGroup.numberOfElements))))
                {
                    var (offset, distributed, list) = item;
                    foreach (var i in list.Where(i => !putTo.ContainsKey(i)))
                    {
                        putTo.Add(i, (offset, distributed));
                    }
                }
            }

            var transformsXYZ = new[] { l.xTransforms, l.yTransforms, l.zTransforms };
            var markIdXYZ = new[] { markXIdx, markYIdx, markZIdx };
            foreach (LightAxis axis in Enum.GetValues(typeof(LightAxis)))
            {
                var transforms = transformsXYZ[(int)axis];
                var markId = markIdXYZ[(int)axis];
                var data = markId
                    .Select(item => (item.Key, item.Value.offset, item.Value.distributed,
                        transforms.ElementAtOrDefault(item.Key))).ToList();

                var mirror = axis switch
                {
                    LightAxis.X => l.mirrorX,
                    LightAxis.Y => l.mirrorY,
                    LightAxis.Z => l.mirrorZ,
                    _ => false
                };
                DoTheFunny(data, ebgs.eventBoxGroupContext.type, axis, mirror);
            }
        }
    }

    private void AddFxGizmo()
    {
        var ebg = bebgdm.GetEventBoxesByEventBoxGroupId(ebgs.eventBoxGroupContext.id)
            .Cast<FxEventBoxEditorData>().ToList();

        // foreach (var l in manager._lightTranslationGroups)
        foreach (var l in _fxManager._floatFxGroups.Where(x =>
                     x.groupId == ebgs.eventBoxGroupContext.groupId))
        {
            var markId = new Dictionary<int, (int offset, bool distributed)>();
            foreach (var item in ebg.Select((eb, i) =>
                         (i, eb.beatDistributionParam > 0,
                             IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.lightGroup.numberOfElements))))
            {
                var (offset, distributed, list) = item;
                foreach (var i in list.Where(i => !markId.ContainsKey(i)))
                {
                    markId.Add(i, (offset, distributed));
                }
            }

            var data = markId
                .Select(item => (item.Key, item.Value.offset, item.Value.distributed,
                    l.targets.Select(t => t.transform).ElementAtOrDefault(item.Key))).ToList();
            DoTheFunny(data, ebgs.eventBoxGroupContext.type, LightAxis.X, false);
        }
    }
}