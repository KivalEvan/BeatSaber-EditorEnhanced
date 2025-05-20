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
        _signalBus.TryUnsubscribe<BeatmapEditingModeSwitched>(UpdateGizmoWithSignal);
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
    }

    public void Initialize()
    {
        _colorManager = Object.FindObjectOfType<LightColorGroupEffectManager>();
        _rotationManager =
            Object.FindObjectOfType<LightRotationGroupEffectManager>();
        _translationManager =
            Object.FindObjectOfType<LightTranslationGroupEffectManager>();
        _fxManager =
            Object.FindObjectOfType<FloatFxGroupEffectManager>();

        _signalBus.Subscribe<BeatmapEditingModeSwitched>(UpdateGizmoWithSignal);
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
        foreach (var gizmo in _gizmos)
            gizmo.SetActive(false);
        _gizmos.Clear();
    }

    private void UpdateGizmoWithSignal(BeatmapEditingModeSwitched signal)
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
        IEnumerable<(int index, int groupIndex, EventBoxEditorData eventBoxContext, bool distributed, Transform
            transform)> data,
        EventBoxGroupType groupType,
        LightAxis axis,
        bool mirror, int maxCount, LightGroupSubsystem subsystemContext)
    {
        HashSet<int> ids = [];
        if (!data.Any()) return;
        foreach (var item in data)
        {
            var (idx, groupIndex, eventBoxContext, distributed, transform) = item;

            var colorIdx = ColorAssignment.GetColorIndexEventBox(groupIndex, idx, distributed);
            Vector3 localScale;
            Vector3 lossyScale;
            
            if (!ids.Contains(groupIndex))
            {
                var laneGizmo = _gizmoAssets.GetOrCreate(distributed ? GizmoType.Sphere : GizmoType.Cube, colorIdx);
                laneGizmo.transform.SetParent(_colorManager.transform.root, false);
                laneGizmo.transform.localPosition = new Vector3((groupIndex - (maxCount - 1) / 2f) / 2f, -0.1f, 0f);
                
                localScale = laneGizmo.transform.localScale;
                lossyScale = laneGizmo.transform.lossyScale;
                laneGizmo.transform.localScale = new Vector3(localScale.x / lossyScale.x * 0.333f, localScale.y / lossyScale.y * 0.1f, localScale.z / lossyScale.z * 0.1f);

                _gizmos.Add(laneGizmo);
                ids.Add(groupIndex);
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
            
            baseGizmo.transform.localPosition = Vector3.zero;
            baseGizmo.transform.SetParent(transform.transform, false);

            localScale = baseGizmo.transform.localScale;
            lossyScale = baseGizmo.transform.lossyScale;
            baseGizmo.transform.localScale = new Vector3(localScale.x / lossyScale.x * 0.5f, localScale.y / lossyScale.y * 0.5f, localScale.z / lossyScale.z * 0.5f);

            var modGizmo = groupType switch
            {
                EventBoxGroupType.Rotation => _gizmoAssets.GetOrCreate(GizmoType.Rotation, axisIdx),
                EventBoxGroupType.Translation => _gizmoAssets.GetOrCreate(GizmoType.Translation, axisIdx),
                _ => null
            };
            if (modGizmo != null)
            {
                modGizmo.transform.SetParent(baseGizmo.transform, false);
                
                localScale = modGizmo.transform.localScale;
                lossyScale = modGizmo.transform.lossyScale;
                modGizmo.transform.localScale = new Vector3(localScale.x / lossyScale.x * 2f, localScale.y / lossyScale.y * 2f, localScale.z / lossyScale.z * 2f);
                
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

            if (groupType is EventBoxGroupType.Translation || groupType is EventBoxGroupType.Rotation)
            {
                var gizmoDraggable = modGizmo.GetComponent<GizmoDraggable>();
                gizmoDraggable.EventBoxEditorDataContext = eventBoxContext;
                gizmoDraggable.LightGroupSubsystemContext = subsystemContext;
                gizmoDraggable.Axis = axis;

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

        foreach (var l in _colorManager.lightGroups
                     .Where(x => x.groupId == _ebgs.eventBoxGroupContext.groupId))
        {
            var markId = new Dictionary<int, (int groupIndex, EventBoxEditorData eventBox, bool distributed)>();
            foreach (var item in ebg.Select((eb, i) =>
                         (i, eb, eb.beatDistributionParam > 0,
                             IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.numberOfElements))))
            {
                var (groupIndex, eventBox, distributed, list) = item;
                foreach (var i in list.Where(i => !markId.ContainsKey(i)))
                    markId.Add(i, (groupIndex, eventBox, distributed));
            }

            var data = new List<(int index, int groupIndex, EventBoxEditorData eventBox, bool distributed, Transform transform)>();
            foreach (var item in markId
                         .Select(i => (groupIndex: i.Value.groupIndex, i.Value.eventBox, i.Value.distributed,
                             _colorManager
                                 ._lightColorGroupEffects
                                 .FirstOrDefault(x => x._lightId == l.startLightId + i.Key)
                                 ?._lightManager._lights.ElementAt(l.startLightId + i.Key)))
                         .Where(item => item.Item4 != null)
                         .Select((l, i) => (i, l.groupIndex, l.eventBox, l.distributed, l.Item4)))
            foreach (var lightWithId in item.Item5)
                switch (lightWithId)
                {
                    case MaterialLightWithId matLightWithId:
                        data.Add((item.i, item.groupIndex, item.eventBox, item.distributed,
                            matLightWithId.transform));
                        break;
                    case TubeBloomPrePassLightWithId tubeBloomPrePassLightWithId:
                        data.Add((item.i, item.groupIndex, item.eventBox, item.distributed,
                            tubeBloomPrePassLightWithId.transform));
                        break;
                }

            DoTheFunny(data, _ebgs.eventBoxGroupContext.type, LightAxis.X, false,
                ebg.Count(), null);
        }
    }

    private void AddRotationGizmo()
    {
        var ebg = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
            .Cast<LightRotationEventBoxEditorData>();

        // foreach (var l in manager._lightTranslationGroups)
        foreach (var l in _rotationManager._lightRotationGroups.Where(x =>
                     x.groupId == _ebgs.eventBoxGroupContext.groupId))
        {
            var markXIdx = new Dictionary<int, (int groupIndex, EventBoxEditorData eventBox, bool distributed)>();
            var markYIdx = new Dictionary<int, (int groupIndex, EventBoxEditorData eventBox, bool distributed)>();
            var markZIdx = new Dictionary<int, (int groupIndex, EventBoxEditorData eventBox, bool distributed)>();
            foreach (var item in ebg.Select((eb, i) =>
                         (i, eb, eb.beatDistributionParam > 0,
                             IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter,
                                 l.lightGroup.numberOfElements), eb.axis)))
            {
                var (groupIndex, eventBox, distributed, list, axis) = item;
                var putTo = axis switch
                {
                    LightAxis.X => markXIdx,
                    LightAxis.Y => markYIdx,
                    LightAxis.Z => markZIdx,
                    _ => throw new ArgumentOutOfRangeException()
                };
                foreach (var i in list.Where(i => !putTo.ContainsKey(i)))
                    putTo.Add(i, (groupIndex, eventBox, distributed));
            }

            var transformsXYZ = new[] { l.xTransforms, l.yTransforms, l.zTransforms };
            var markIdXYZ = new[] { markXIdx, markYIdx, markZIdx };
            foreach (LightAxis axis in Enum.GetValues(typeof(LightAxis)))
            {
                var transforms = transformsXYZ[(int)axis];
                var markId = markIdXYZ[(int)axis];
                var data = markId
                    .Select(item => (item.Key, item.Value.groupIndex, item.Value.eventBox, item.Value.distributed,
                        transforms.ElementAtOrDefault(item.Key)));

                var mirror = axis switch
                {
                    LightAxis.X => l.mirrorX,
                    LightAxis.Y => l.mirrorY,
                    LightAxis.Z => l.mirrorZ,
                    _ => false
                };
                DoTheFunny(data, _ebgs.eventBoxGroupContext.type, axis, mirror,
                    ebg.Count(), l);
            }
        }
    }

    private void AddTranslationGizmo()
    {
        var eventBoxGroup = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
            .Cast<LightTranslationEventBoxEditorData>();

        // foreach (var l in manager._lightTranslationGroups)
        foreach (var l in _translationManager._lightTranslationGroups.Where(x =>
                     x.groupId == _ebgs.eventBoxGroupContext.groupId))
        {
            var markXIdx = new Dictionary<int, (int groupIndex, EventBoxEditorData eventBox, bool distributed)>();
            var markYIdx = new Dictionary<int, (int groupIndex, EventBoxEditorData eventBox, bool distributed)>();
            var markZIdx = new Dictionary<int, (int groupIndex, EventBoxEditorData eventBox, bool distributed)>();
            foreach (var item in eventBoxGroup.Select((eb, i) =>
                         (i, eb, eb.beatDistributionParam > 0,
                             IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter,
                                 l.lightGroup.numberOfElements), eb.axis)))
            {
                var (groupIndex, eventBox, distributed, list, axis) = item;
                var putTo = axis switch
                {
                    LightAxis.X => markXIdx,
                    LightAxis.Y => markYIdx,
                    LightAxis.Z => markZIdx,
                    _ => throw new ArgumentOutOfRangeException()
                };
                foreach (var i in list.Where(i => !putTo.ContainsKey(i)))
                    putTo.Add(i, (groupIndex, eventBox, distributed));
            }

            var transformsXYZ = new[] { l.xTransforms, l.yTransforms, l.zTransforms };
            var markIdXYZ = new[] { markXIdx, markYIdx, markZIdx };
            foreach (LightAxis axis in Enum.GetValues(typeof(LightAxis)))
            {
                var transforms = transformsXYZ[(int)axis];
                var markId = markIdXYZ[(int)axis];
                var data = markId
                    .Select(item => (item.Key, item.Value.groupIndex, item.Value.eventBox, item.Value.distributed,
                        transforms.ElementAtOrDefault(item.Key)));

                var mirror = axis switch
                {
                    LightAxis.X => l.mirrorX,
                    LightAxis.Y => l.mirrorY,
                    LightAxis.Z => l.mirrorZ,
                    _ => false
                };
                DoTheFunny(data, _ebgs.eventBoxGroupContext.type, axis, mirror,
                    eventBoxGroup.Count(), l);
            }
        }
    }

    private void AddFxGizmo()
    {
        var ebg = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id)
            .Cast<FxEventBoxEditorData>();

        // foreach (var l in manager._lightTranslationGroups)
        foreach (var l in _fxManager._floatFxGroups.Where(x =>
                     x.groupId == _ebgs.eventBoxGroupContext.groupId))
        {
            var markId = new Dictionary<int, (int groupIndex, EventBoxEditorData eventBox, bool distributed)>();
            foreach (var item in ebg.Select((eb, i) =>
                         (i, eb, eb.beatDistributionParam > 0,
                             IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.lightGroup.numberOfElements))))
            {
                var (groupIndex, eventBox, distributed, list) = item;
                foreach (var i in list.Where(i => !markId.ContainsKey(i)))
                    markId.Add(i, (groupIndex, eventBox, distributed));
            }

            var data = markId
                .Select(item => (item.Key, item.Value.groupIndex, item.Value.eventBox, item.Value.distributed,
                    l.targets.Select(t => t.transform).ElementAtOrDefault(item.Key)));
            DoTheFunny(data, _ebgs.eventBoxGroupContext.type, LightAxis.X, false,
                ebg.Count(), l);
        }
    }
}