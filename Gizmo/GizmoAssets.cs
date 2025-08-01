using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D.DataModels;
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
    Sphere
}

internal class GizmoAssets : IInitializable, IDisposable
{
    private readonly DiContainer _diContainer;

    public static readonly Material DefaultMaterial = FetchMaterial("Assets/Shaders/Gizmo.mat");
    public static readonly Material OutlineMaterial = FetchMaterial("Assets/Shaders/Outline.mat");
    private readonly List<GameObject> _colorObjects = [];
    private readonly List<GameObject> _fxObjects = [];
    private readonly List<GameObject> _rotationObjects = [];
    private readonly List<GameObject> _translationObjects = [];

    private readonly Material[] _sharedMaterials = new Material[ColorAssignment.HueRange];

    public void Dispose()
    {
        _colorObjects.ForEach(Object.Destroy);
        _rotationObjects.ForEach(Object.Destroy);
        _translationObjects.ForEach(Object.Destroy);
        _fxObjects.ForEach(Object.Destroy);

        _colorObjects.Clear();
        _rotationObjects.Clear();
        _translationObjects.Clear();
        _fxObjects.Clear();
    }

    public GizmoAssets(DiContainer diContainer)
    {
        _diContainer = diContainer;
    }

    public void Initialize()
    {
        CubeGizmo.SObject = CubeGizmo.Create(DefaultMaterial);
        RotationGizmo.SObject = RotationGizmo.Create(DefaultMaterial);
        TranslationGizmo.SObject = TranslationGizmo.Create(DefaultMaterial);
        SphereGizmo.SObject = SphereGizmo.Create(DefaultMaterial);
    }

    public Material GetOrCreateMaterial(int index)
    {
        if (index < 0 || index >= ColorAssignment.HueRange) return DefaultMaterial;
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
        mat.SetColor("_Color", color);
        return mat;
    }

    public GameObject GetOrCreate(GizmoType gizmoType, int colorIdx)
    {
        var objects = gizmoType switch
        {
            GizmoType.Cube => _colorObjects,
            GizmoType.Rotation => _rotationObjects,
            GizmoType.Translation => _translationObjects,
            GizmoType.Sphere => _fxObjects,
            _ => throw new ArgumentOutOfRangeException(nameof(gizmoType), gizmoType, null)
        };

        var go = objects.FirstOrDefault(obj => !obj.activeSelf);
        if (go == null)
        {
            go = gizmoType switch
            {
                GizmoType.Cube => _diContainer.InstantiatePrefab(CubeGizmo.SObject),
                GizmoType.Rotation => _diContainer.InstantiatePrefab(RotationGizmo.SObject),
                GizmoType.Translation => _diContainer.InstantiatePrefab(TranslationGizmo.SObject),
                GizmoType.Sphere => _diContainer.InstantiatePrefab(SphereGizmo.SObject),
                _ => throw new ArgumentOutOfRangeException(nameof(gizmoType), gizmoType, null)
            };
            objects.Add(go);
        }

        var mat = GetOrCreateMaterial(colorIdx);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        
        // var lineRenderController = go.GetComponent<LineRenderController>();
        // if (lineRenderController != null)
        // {
        //     lineRenderController.SetMaterial(mat);
        //     lineRenderController.SetTransforms([]);
        //     lineRenderController.enabled = false;
        // }

        go.SetActive(true);
        return go;
    }
}