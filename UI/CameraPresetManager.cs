using System;
using BeatmapEditor3D.Controller;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI;

internal class CameraPresetManager : IInitializable
{
    internal enum CameraType
    {
        Default,
        Player,
        Previous,
        Saved
    }
    
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;

    private static Vector3 _defaultPosition = Vector3.zero;
    private static Quaternion _defaultRotation = Quaternion.identity;

    private static Vector3 _savedPosition = Vector3.zero;
    private static Quaternion _savedRotation = Quaternion.identity;

    public void Initialize()
    {
        var cameraWrapper = GameObject.Find("/Wrapper/BeatmapEditorUICameraWrapper");
        var comp = cameraWrapper.GetComponent<BeatmapEditor360CameraController>();
        _defaultPosition = Vector3.zero + comp._uiCameraMovementTransform.position;
        _defaultRotation = Quaternion.identity * comp._uiCameraTransform.transform.rotation;
    }

    public void SetCamera(CameraType cameraType)
    {
        switch (cameraType)
        {
            case CameraType.Default:
                SetCamera(_defaultPosition, _defaultRotation);
                break;
            case CameraType.Player:
                SetCamera(new Vector3(0, 1.5f, 0), Quaternion.identity);
                break;
            case CameraType.Previous:
                SetCamera(_previousPosition, _previousRotation);
                break;
            case CameraType.Saved:
                SetCamera(_savedPosition, _savedRotation);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cameraType), cameraType, null);
        }
    }
    
    public void SetCamera(Vector3 position, Quaternion rotation)
    {
        var cameraWrapper = GameObject.Find("/Wrapper/BeatmapEditorUICameraWrapper");
        if (cameraWrapper == null) return;
        var comp = cameraWrapper.GetComponent<BeatmapEditor360CameraController>();

        _previousPosition = Vector3.zero + comp._uiCameraMovementTransform.position;
        _previousRotation = Quaternion.identity * comp._uiCameraTransform.rotation;

        comp._uiCameraMovementTransform.position = Vector3.zero + position;
        comp._uiCameraTransform.transform.rotation = Quaternion.identity * rotation;
        comp._mouseLook._cameraTargetRot = Quaternion.identity * comp._uiCameraTransform.transform.rotation;
    }

    public void SaveCamera()
    {
        var cameraWrapper = GameObject.Find("/Wrapper/BeatmapEditorUICameraWrapper");
        if (cameraWrapper == null) return;
        var comp = cameraWrapper.GetComponent<BeatmapEditor360CameraController>();

        _savedPosition = Vector3.zero + comp._uiCameraMovementTransform.position;
        _savedRotation = Quaternion.identity * comp._uiCameraTransform.rotation;
    }
}