using System;
using System.Collections.Generic;
using System.Linq;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.Gizmo.Drawers;
using EditorEnhanced.Utils;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.Gizmo;

internal enum GizmoType
{
    Cube,
    Rotation,
    Translation,
    Sphere,
    Lane,
    Selection
}

internal class GizmoAssets : IInitializable, IDisposable
{
    public static readonly Material DefaultMaterial = FetchMaterial("Assets/Shaders/Gizmo.mat");
    public static readonly Material OutlineMaterial = FetchMaterial("Assets/Shaders/Outline.mat");
    private static readonly int MatColorId = Shader.PropertyToID("_Color");
    private readonly PluginConfig _config;

    private readonly DiContainer _diContainer;

    private readonly List<GameObject>[] _gizmoObjects = new List<GameObject>[6];
    private readonly GameObject[] _gizmoPrefab = new GameObject[6];
    private readonly Material[] _sharedMaterials = new Material[ColorAssignment.HueRange];
    private readonly SignalBus _signalBus;

    public GizmoAssets(DiContainer diContainer, SignalBus signalBus, PluginConfig config)
    {
        _diContainer = diContainer;
        _signalBus = signalBus;
        _config = config;
    }

    public void Dispose()
    {
        foreach (var gizmos in _gizmoObjects)
        {
            gizmos.ForEach(Object.Destroy);
            gizmos.Clear();
        }

        _signalBus.TryUnsubscribe<GizmoConfigRaycastGizmoUpdateSignal>(HandleRaycastGizmoUpdate);
        _signalBus.TryUnsubscribe<GizmoConfigRaycastLaneUpdateSignal>(HandleRaycastLaneUpdate);
    }

    public void Initialize()
    {
        _gizmoPrefab[(int)GizmoType.Cube] = CubeGizmo.SObject = CubeGizmo.Create(DefaultMaterial);
        _gizmoPrefab[(int)GizmoType.Rotation] = RotationGizmo.SObject = RotationGizmo.Create(DefaultMaterial);
        _gizmoPrefab[(int)GizmoType.Translation] = TranslationGizmo.SObject = TranslationGizmo.Create(DefaultMaterial);
        _gizmoPrefab[(int)GizmoType.Sphere] = SphereGizmo.SObject = SphereGizmo.Create(DefaultMaterial);
        _gizmoPrefab[(int)GizmoType.Lane] = LaneGizmo.SObject = LaneGizmo.Create(DefaultMaterial);
        _gizmoPrefab[(int)GizmoType.Selection] = SelectionGizmo.SObject = SelectionGizmo.Create(DefaultMaterial);

        for (var i = 0; i < _gizmoObjects.Length; i++) _gizmoObjects[i] = [];

        _signalBus.Subscribe<GizmoConfigRaycastGizmoUpdateSignal>(HandleRaycastGizmoUpdate);
        _signalBus.Subscribe<GizmoConfigRaycastLaneUpdateSignal>(HandleRaycastLaneUpdate);
    }

    private void HandleRaycastGizmoUpdate()
    {
        Action<GameObject> action = _config.Gizmo.RaycastGizmo ? EnableCollider : DisableCollider;
        _gizmoObjects[(int)GizmoType.Cube].ForEach(action);
        _gizmoObjects[(int)GizmoType.Sphere].ForEach(action);
        _gizmoObjects[(int)GizmoType.Rotation].ForEach(action);
        _gizmoObjects[(int)GizmoType.Translation].ForEach(action);
    }

    private void HandleRaycastLaneUpdate()
    {
        Action<GameObject> action = _config.Gizmo.RaycastLane ? EnableCollider : DisableCollider;
        _gizmoObjects[(int)GizmoType.Lane].ForEach(action);
        _gizmoObjects[(int)GizmoType.Selection].ForEach(action);
    }

    private static void EnableCollider(GameObject gizmo)
    {
        gizmo.layer = 22;
    }

    private static void DisableCollider(GameObject gizmo)
    {
        gizmo.layer = 0;
    }

    private Material GetOrCreateMaterial(int index)
    {
        if (index is < 0 or >= ColorAssignment.HueRange) return DefaultMaterial;
        if (_sharedMaterials[index] != null) return _sharedMaterials[index];
        var color = ColorAssignment.GetColorFromIndex(index);
        _sharedMaterials[index] = CreateMaterial(color);
        return _sharedMaterials[index];
    }

    private static Material FetchMaterial(string path)
    {
        var bundle = AssetLoader.LoadFromResource(nameof(EditorEnhanced) + ".model");
        return bundle.LoadAsset<Material>(path);
    }

    private static Material CreateMaterial(Color color)
    {
        var mat = new Material(DefaultMaterial);
        mat.SetColor(MatColorId, color);
        return mat;
    }

    public GameObject GetOrCreate(GizmoType gizmoType, int colorIdx)
    {
        var objects = _gizmoObjects[(int)gizmoType];

        var go = objects.FirstOrDefault(obj => !obj.activeSelf);
        if (go == null)
        {
            var prefab = _gizmoPrefab[(int)gizmoType];
            go = _diContainer.InstantiatePrefab(prefab);
            objects.Add(go);
        }

        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return go;

        Action<GameObject> action;
        if (gizmoType is GizmoType.Lane or GizmoType.Selection)
            action = _config.Gizmo.RaycastLane ? EnableCollider : DisableCollider;
        else
            action = _config.Gizmo.RaycastGizmo ? EnableCollider : DisableCollider;

        action(go);

        var mat = GetOrCreateMaterial(colorIdx);
        renderer.sharedMaterial = mat;

        // var lineRenderController = go.GetComponent<LineRenderController>();
        // if (lineRenderController != null)
        // {
        //     lineRenderController.SetMaterial(mat);
        //     lineRenderController.SetTransforms([]);
        //     lineRenderController.enabled = false;
        // }

        return go;
    }
}