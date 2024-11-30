using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.Types;
using BeatmapSaveDataVersion4;
using EditorEnhanced.Helpers;
using UnityEngine;
using Zenject;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;
using Object = UnityEngine.Object;

namespace EditorEnhanced.Gizmo
{
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

        public void Initialize()
        {
            var lightColorGroupEffectManager = GameObject.Find("Environment/LightColorGroupEffectManager");
            if (lightColorGroupEffectManager == null) return;
            _colorManager =
                lightColorGroupEffectManager.GetComponent<LightColorGroupEffectManager>();
            var lightRotationGroupEffectManager = GameObject.Find("Environment/LightRotationGroupEffectManager");
            if (lightRotationGroupEffectManager == null) return;
            _rotationManager =
                lightRotationGroupEffectManager.GetComponent<LightRotationGroupEffectManager>();
            var lightTranslationGroupEffectManager = GameObject.Find("Environment/LightTranslationGroupEffectManager");
            if (lightTranslationGroupEffectManager == null) return;
            _translationManager =
                lightTranslationGroupEffectManager.GetComponent<LightTranslationGroupEffectManager>();
            var floatFxGroupEffectManager = GameObject.Find("Environment/FloatFxGroupEffectManager");
            if (floatFxGroupEffectManager == null) return;
            _fxManager =
                floatFxGroupEffectManager.GetComponent<FloatFxGroupEffectManager>();

            signalBus.Subscribe<EditEventBoxGroupSignal>(AddGizmo);
            signalBus.Subscribe<ExitEditEventBoxGroupSignal>(RemoveGizmo);
            signalBus.Subscribe<ModifyEventBoxSignal>(UpdateGizmo);
            signalBus.Subscribe<InsertEventBoxSignal>(UpdateGizmo);
            signalBus.Subscribe<InsertEventBoxesForAllAxesSignal>(UpdateGizmo);
            signalBus.Subscribe<InsertEventBoxesForAllIdsSignal>(UpdateGizmo);
            signalBus.Subscribe<InsertEventBoxesForAllIdsAndAxisSignal>(UpdateGizmo);
            signalBus.Subscribe<DeleteEventBoxSignal>(UpdateGizmo);
            // signalBus.Subscribe<SwitchBeatmapEditingModeSignal>(new Action(RemoveGizmo));
        }

        public void Dispose()
        {
            RemoveGizmo();
            signalBus.TryUnsubscribe<EditEventBoxGroupSignal>(AddGizmo);
            signalBus.TryUnsubscribe<ExitEditEventBoxGroupSignal>(RemoveGizmo);
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
            // if (switchBeatmapEditingModeSignal.mode == BeatmapEditingMode.EventBoxes) return;
            foreach (var gameObject in _gameObjects)
                gameObject.SetActive(false);
            _gameObjects.Clear();
        }

        private void UpdateGizmo()
        {
            RemoveGizmo();
            AddGizmo();
        }

        private void DoTheFunny(IEnumerable<Transform> transforms, EventBoxGroupType groupType, int axis,
            bool mirror)
        {
            foreach (var t in transforms)
            {
                var go = groupType switch
                {
                    EventBoxGroupType.Color => gizmoAssets.GetOrCreate(GizmoType.Color, axis),
                    EventBoxGroupType.Rotation => gizmoAssets.GetOrCreate(GizmoType.Rotation, axis),
                    EventBoxGroupType.Translation => gizmoAssets.GetOrCreate(GizmoType.Translation, axis),
                    EventBoxGroupType.FloatFx => gizmoAssets.GetOrCreate(GizmoType.Fx, axis),
                    _ => throw new ArgumentOutOfRangeException(nameof(groupType), groupType, null)
                };
                go.transform.SetParent(t.transform, false);
                if (groupType == EventBoxGroupType.Rotation)
                {
                    go.transform.localRotation = axis switch
                    {
                        0 => mirror
                            ? Quaternion.Euler(180, 0, 90)
                            : Quaternion.Euler(0, 0, 90),
                        1 => mirror
                            ? Quaternion.Euler(0, 270, 0)
                            : Quaternion.Euler(0, 90, 0),
                        2 => mirror
                            ? Quaternion.Euler(90, 0, 0)
                            : Quaternion.Euler(270, 0, 0),
                        _ => Quaternion.identity
                    };
                }
                else
                {
                    go.transform.localRotation = axis switch
                    {
                        0 => mirror
                            ? Quaternion.Euler(0, 270, 0)
                            : Quaternion.Euler(0, 90, 0),
                        1 => mirror
                            ? Quaternion.Euler(90, 0, 0)
                            : Quaternion.Euler(270, 0, 0),
                        2 => mirror
                            ? Quaternion.Euler(180, 0, 0)
                            : Quaternion.Euler(0, 0, 0),
                        _ => Quaternion.identity
                    };
                }

                _gameObjects.Add(go);
            }
        }

        private void AddColorGizmo()
        {
            var ebg = bebgdm.GetEventBoxesByEventBoxGroupId(ebgs.eventBoxGroupContext.id)
                .Cast<LightColorEventBoxEditorData>().ToList();

            // foreach (var l in manager._lightTranslationGroups)
            foreach (var l in _colorManager.lightGroups.Where(x =>
                         x.groupId == ebgs.eventBoxGroupContext.groupId))
            {
                var markId = new HashSet<int>();
                foreach (var eb in ebg)
                {
                    var r = IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.numberOfElements);
                    foreach (var i in r)
                    {
                        markId.Add(i);
                    }
                }

                var transforms = new List<Transform>();
                foreach (var i in markId)
                {
                    var lightWithIds =
                        _colorManager._lightColorGroupEffects.FirstOrDefault(x => x._lightId == l.startLightId + i)
                            ?._lightManager._lights.ElementAt(l.startLightId + i);
                    if (lightWithIds == null) continue;
                    foreach (var lightWithId in lightWithIds)
                    {
                        switch (lightWithId)
                        {
                            case MaterialLightWithId matLightWithId:
                                transforms.Add(matLightWithId.transform);
                                break;
                            case TubeBloomPrePassLightWithId tubeBloomPrePassLightWithId:
                                transforms.Add(tubeBloomPrePassLightWithId.transform);
                                break;
                        }
                    }
                }

                DoTheFunny(transforms, ebgs.eventBoxGroupContext.type, 3, false);
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
                var xIdx = new HashSet<int>();
                var yIdx = new HashSet<int>();
                var zIdx = new HashSet<int>();
                foreach (var eb in ebg)
                {
                    var r = IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.lightGroup.numberOfElements);
                    var putTo = eb.axis switch
                    {
                        LightAxis.X => xIdx,
                        LightAxis.Y => yIdx,
                        LightAxis.Z => zIdx,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    foreach (var i in r)
                    {
                        putTo.Add(i);
                    }
                }

                var transformsXYZ = new List<List<Transform>> { l.xTransforms, l.yTransforms, l.zTransforms };
                var markIdXYZ = new List<HashSet<int>> { xIdx, yIdx, zIdx };
                for (var idx = 0; idx < transformsXYZ.Count; idx++)
                {
                    var transformList = transformsXYZ[idx];
                    var idxFilter = markIdXYZ[idx];
                    var filteredTransforms = transformList.Where((_, i) => idxFilter.Contains(i));
                    var mirror = idx switch
                    {
                        0 => l.mirrorX,
                        1 => l.mirrorY,
                        2 => l.mirrorZ,
                        _ => false
                    };
                    DoTheFunny(filteredTransforms, ebgs.eventBoxGroupContext.type, idx, mirror);
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
                var xIdx = new HashSet<int>();
                var yIdx = new HashSet<int>();
                var zIdx = new HashSet<int>();
                foreach (var eb in ebg)
                {
                    var r = IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.lightGroup.numberOfElements);
                    var putTo = eb.axis switch
                    {
                        LightAxis.X => xIdx,
                        LightAxis.Y => yIdx,
                        LightAxis.Z => zIdx,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    foreach (var i in r)
                    {
                        putTo.Add(i);
                    }
                }

                var transformsXYZ = new List<List<Transform>> { l.xTransforms, l.yTransforms, l.zTransforms };
                var markIdXYZ = new List<HashSet<int>> { xIdx, yIdx, zIdx };
                for (var idx = 0; idx < transformsXYZ.Count; idx++)
                {
                    var transformList = transformsXYZ[idx];
                    var idxFilter = markIdXYZ[idx];
                    var filteredTransforms = transformList.Where((_, i) => idxFilter.Contains(i));
                    var mirror = idx switch
                    {
                        0 => l.mirrorX,
                        1 => l.mirrorY,
                        2 => l.mirrorZ,
                        _ => false
                    };
                    DoTheFunny(filteredTransforms, ebgs.eventBoxGroupContext.type, idx, mirror);
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
                var markId = new HashSet<int>();
                foreach (var eb in ebg)
                {
                    var r = IndexFilterHelpers.GetIndexFilterRange(eb.indexFilter, l.lightGroup.numberOfElements);
                    foreach (var i in r)
                    {
                        markId.Add(i);
                    }
                }

                var transforms = l.targets.Select(t => t.transform).Where((_, i) => markId.Contains(i));
                DoTheFunny(transforms, ebgs.eventBoxGroupContext.type, 3, false);
            }
        }
    }
}