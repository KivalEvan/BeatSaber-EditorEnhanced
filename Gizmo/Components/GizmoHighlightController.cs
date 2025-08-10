using System.Collections.Generic;
using EditorEnhanced.Configuration;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoHighlightController : MonoBehaviour, IGizmoInput
{
    [Inject] private readonly PluginConfig _config;
    private List<GizmoHighlight> _highlighters;
    private bool _isDragging;

    public void OnPointerEnter()
    {
        if (!_isDragging) Highlight();
    }

    public void OnPointerExit()
    {
        if (!_isDragging) Unhighlight();
    }

    public void OnDrag()
    {
    }

    public void OnMouseClick()
    {
        _isDragging = true;
        Highlight();
    }

    public void OnMouseRelease()
    {
        Unhighlight();
        _isDragging = false;
    }

    public void Highlight()
    {
        if (!_config.Gizmo.Highlight) return;
        foreach (var highlighter in _highlighters) highlighter.AddOutline();
    }

    public void Unhighlight()
    {
        if (!_config.Gizmo.Highlight) return;
        foreach (var highlighter in _highlighters) highlighter.RemoveOutline();
    }

    public void Add(GameObject gizmo)
    {
        var gizmoHighlighter = gizmo.GetComponent<GizmoHighlight>();
        if (gizmoHighlighter == null) return;
        _highlighters.Add(gizmoHighlighter);
    }

    public void SharedWith(GizmoHighlightController gizmoHighlightController)
    {
        _highlighters = gizmoHighlightController._highlighters;
    }

    public void Init()
    {
        _highlighters = [];
    }
}