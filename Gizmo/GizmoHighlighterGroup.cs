using System.Collections.Generic;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

public class GizmoHighlighterGroup : MonoBehaviour, IGizmoInput
{
    private List<GizmoHighlighter> _highlighters = [];
    private bool _isDragging;

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

    public void SharedWith(GizmoHighlighterGroup gizmoHighlighterGroup)
    {
        _highlighters = gizmoHighlighterGroup._highlighters;
    }

    public void Clear()
    {
        _highlighters = [];
    }

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

    public void OnBeginDrag()
    {
        _isDragging = true;
        Highlight();
    }

    public void OnEndDrag()
    {
        Unhighlight();
        _isDragging = false;
    }
}