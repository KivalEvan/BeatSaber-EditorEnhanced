using System.Collections.Generic;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

public class GizmoHighlighter : MonoBehaviour
{
    private GameObject _highlightObject;

    private void Awake()
    {
        _highlightObject = gameObject.transform.GetChild(0).gameObject;
    }

    public void AddOutline()
    {
        _highlightObject.SetActive(true);
    }

    public void RemoveOutline()
    {
        _highlightObject.SetActive(false);
    }
}