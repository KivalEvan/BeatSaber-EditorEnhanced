using System;
using System.Collections.Generic;
using System.Linq;
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
    private static readonly Material _defaultMaterial = FetchMaterial();
    private readonly List<GameObject> _colorObjects = [];
    private readonly List<GameObject> _fxObjects = [];
    private readonly List<GameObject> _rotationObjects = [];

    private readonly Material[] _sharedMaterials = new Material[ColorAssignment.HueRange];
    private readonly List<GameObject> _translationObjects = [];

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

    public void Initialize()
    {
        CubeGizmo.SObject = CubeGizmo.Create(_defaultMaterial);
        RotationGizmo.SObject = RotationGizmo.Create(_defaultMaterial);
        TranslationGizmo.SObject = TranslationGizmo.Create(_defaultMaterial);
        SphereGizmo.SObject = SphereGizmo.Create(_defaultMaterial);
    }

    private Material GetOrCreateMaterial(int index)
    {
        if (index < 0 || index >= ColorAssignment.HueRange) return _defaultMaterial;
        if (_sharedMaterials[index] != null) return _sharedMaterials[index];
        var color = ColorAssignment.GetColorFromIndex(index);
        _sharedMaterials[index] = CreateMaterial(color);
        return _sharedMaterials[index];
    }

    private static Material FetchMaterial()
    {
        var bundle = AssetLoader.LoadFromResource(nameof(EditorEnhanced) + ".model");
        return bundle.LoadAsset<Material>("Assets/Shaders/Unlit.mat");
    }

    private static Material CreateMaterial(Color color)
    {
        var mat = new Material(_defaultMaterial);
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
                GizmoType.Cube => Object.Instantiate(CubeGizmo.SObject),
                GizmoType.Rotation => Object.Instantiate(RotationGizmo.SObject),
                GizmoType.Translation => Object.Instantiate(TranslationGizmo.SObject),
                GizmoType.Sphere => Object.Instantiate(SphereGizmo.SObject),
                _ => throw new ArgumentOutOfRangeException(nameof(gizmoType), gizmoType, null)
            };
            objects.Add(go);
        }

        go.GetComponent<Renderer>().material = GetOrCreateMaterial(colorIdx);
        go.SetActive(true);
        return go;
    }
}