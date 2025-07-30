using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo;

public class GizmoHighlighterGrouped : MonoBehaviour, IGizmoInput
{
    private readonly List<GizmoHighlighter> _highlighters = [];

    public void Highlight()
    {
        foreach (var highlighter in _highlighters)
        {
            highlighter.AddOutline();
        }
    }

    public void Unhighlight()
    {
        foreach (var highlighter in _highlighters)
        {
            highlighter.RemoveOutline();
        }
    }

    public void Add(GameObject gizmo)
    {
        var gizmoHighlighter = gizmo.GetComponent<GizmoHighlighter>();
        if (gizmoHighlighter == null) return;
        _highlighters.Add(gizmoHighlighter);
    }

    public void Clear()
    {
        _highlighters.Clear();
    }

    public void OnPointerEnter()
    {
        Highlight();
    }

    public void OnPointerExit()
    {
        Unhighlight();
    }

    public void OnDrag()
    {
    }

    public void OnBeginDrag()
    {
    }

    public void OnEndDrag()
    {
    }
}