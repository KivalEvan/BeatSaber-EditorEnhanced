using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.Gizmo;

internal enum GizmoType
{
    Color,
    Rotation,
    Translation,
    Fx
}

internal class GizmoAssets : IInitializable, IDisposable
{
    private List<Material> _materials;
    private readonly List<GameObject> _colorObjects = [];
    private readonly List<GameObject> _rotationObjects = [];
    private readonly List<GameObject> _translationObjects = [];
    private readonly List<GameObject> _fxObjects = [];

    public void Initialize()
    {
        var colors = new List<Color>
            { Color.red, Color.green, Color.blue, Color.white };
        _materials = [];

        foreach (var color in colors)
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            var sharedMat = new Material(shader);
            sharedMat.SetColor("_Color", color);
            sharedMat.SetInt("_SrcBlend", 5);
            sharedMat.SetInt("_DstBlend", 10);
            sharedMat.SetInt("_Cull", 0);
            sharedMat.SetInt("_ZWrite", 0);
            sharedMat.SetInt("_ZTest", 8);
            _materials.Add(sharedMat);
        }

        ColorGizmo.SObject = ColorGizmo.Create(_materials[0]);
        RotationGizmo.SObject = RotationGizmo.Create(_materials[0]);
        TranslationGizmo.SObject = TranslationGizmo.Create(_materials[0]);
        FxGizmo.SObject = FxGizmo.Create(_materials[0]);
    }

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

    public GameObject GetOrCreate(GizmoType gizmoType, int colorIdx)
    {
        var objects = gizmoType switch
        {
            GizmoType.Color => _colorObjects,
            GizmoType.Rotation => _rotationObjects,
            GizmoType.Translation => _translationObjects,
            GizmoType.Fx => _fxObjects,
            _ => throw new ArgumentOutOfRangeException(nameof(gizmoType), gizmoType, null)
        };

        var go = objects.FirstOrDefault(obj => !obj.activeSelf);
        if (go == null)
        {
            go = gizmoType switch
            {
                GizmoType.Color => Object.Instantiate(ColorGizmo.SObject),
                GizmoType.Rotation => Object.Instantiate(RotationGizmo.SObject),
                GizmoType.Translation => Object.Instantiate(TranslationGizmo.SObject),
                GizmoType.Fx => Object.Instantiate(FxGizmo.SObject),
                _ => throw new ArgumentOutOfRangeException(nameof(gizmoType), gizmoType, null)
            };
            objects.Add(go);
        }

        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
        {
            renderer.material = _materials[colorIdx];
        }

        go.SetActive(true);
        return go;
    }
}