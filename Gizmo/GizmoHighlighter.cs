using System.Collections.Generic;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

public class GizmoHighlighter : MonoBehaviour
{
    private Renderer _renderer;
    
    private void Awake()
    {
        _renderer = gameObject.GetComponent<Renderer>();
    }

    public void AddOutline()
    {
        var mats = new List<Material>();
        _renderer.GetSharedMaterials(mats);
        if (!mats.Contains(GizmoAssets.OutlineMaterial)) mats.Insert(0, GizmoAssets.OutlineMaterial);
        _renderer.SetSharedMaterials(mats);
    }

    public void RemoveOutline()
    {
        var mats = new List<Material>();
        _renderer.GetSharedMaterials(mats);
        mats.Remove(GizmoAssets.OutlineMaterial);
        _renderer.SetSharedMaterials(mats);
    }
}