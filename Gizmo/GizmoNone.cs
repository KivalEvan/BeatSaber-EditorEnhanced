using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo;

public class GizmoNone : MonoBehaviour
{
    public Transform TargetTransform;

    private const float GizmoBaseSize = 0.5f;

    private void OnEnable()
    {
        transform.SetParent(TargetTransform.parent, false);
        transform.position = TargetTransform.position;
        transform.rotation = TargetTransform.parent.rotation;
        var localScale = transform.localScale;
        var lossyScale = transform.lossyScale;
        transform.localScale = new Vector3(
            Mathf.Abs(localScale.x / (lossyScale.x != 0 ? lossyScale.x : 1) * GizmoBaseSize),
            Mathf.Abs(localScale.y / (lossyScale.y != 0 ? lossyScale.y : 1) * GizmoBaseSize),
            Mathf.Abs(localScale.z / (lossyScale.z != 0 ? lossyScale.z : 1) * GizmoBaseSize));
        transform.SetParent(TargetTransform, true);
    }
}