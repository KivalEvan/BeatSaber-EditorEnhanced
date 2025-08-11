using UnityEngine;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoHighlight : MonoBehaviour
{
    private GameObject _highlightObject;

    private void Awake()
    {
        _highlightObject = transform.GetChild(0).gameObject;
    }

    public void AddOutline()
    {
        if (_highlightObject != null) _highlightObject.SetActive(true);
        else
            Plugin.Log.Error($"Highlight is missing {gameObject}");
    }

    public void RemoveOutline()
    {
        if (_highlightObject != null) _highlightObject.SetActive(false);
    }
}