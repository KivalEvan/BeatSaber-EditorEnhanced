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

internal class GizmoAssets : IInitializable
{
    private List<Material> materials;
    private List<GameObject> colorObjects = new List<GameObject>();
    private List<GameObject> rotationObjects = new List<GameObject>();
    private List<GameObject> translationObjects = new List<GameObject>();
    private List<GameObject> fxObjects = new List<GameObject>();

    public void Initialize()
    {
        var colors = new List<Color>
            { Color.red, Color.green, Color.blue, Color.white };
        materials = [];

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
            materials.Add(sharedMat);
        }

        ColorGizmo.SObject = ColorGizmo.Create(materials[0]);
        RotationGizmo.SObject = RotationGizmo.Create(materials[0]);
        TranslationGizmo.SObject = TranslationGizmo.Create(materials[0]);
        FxGizmo.SObject = FxGizmo.Create(materials[0]);
    }

    public GameObject GetOrCreate(GizmoType gizmoType, int colorIdx)
    {
        var objects = gizmoType switch
        {
            GizmoType.Color => colorObjects,
            GizmoType.Rotation => rotationObjects,
            GizmoType.Translation => translationObjects,
            GizmoType.Fx => fxObjects,
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
            renderer.material = materials[colorIdx];
        }

        go.SetActive(true);
        return go;
    }
}