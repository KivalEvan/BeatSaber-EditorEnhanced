using System;
using System.Collections.Generic;
using BeatmapEditor3D.Controller;
using BeatSaberMarkupLanguage.Tags;
using HMUI;
using TMPro;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI;

internal class CameraPresetViewController : IInitializable, IDisposable
{
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;

    private static bool _initialized;
    private static Vector3 _defaultPosition = Vector3.zero;
    private static Quaternion _defaultRotation = Quaternion.identity;

    private static Vector3 _savedPosition = Vector3.zero;
    private static Quaternion _savedRotation = Quaternion.identity;

    private List<GameObject> _presetCameraUi;

    public void Initialize()
    {
        var btnTag = new EditorButtonTextTag();
        
        var uiWrapper =
            GameObject.Find(
                "/Wrapper/ViewControllers/EditBeatmapViewController/StatusBarView/StatusBarControls/BasicControlsWrapper/SlidersWrapper");

        var cameraWrapper = GameObject.Find("/Wrapper/BeatmapEditorUICameraWrapper");
        var comp = cameraWrapper.GetComponent<BeatmapEditor360CameraController>();
        if (!_initialized)
        {
            _initialized = true;
            _defaultPosition = Vector3.zero + comp._uiCameraMovementTransform.position;
            _defaultRotation = Quaternion.identity * comp._uiCameraTransform.transform.rotation;
        }

        _presetCameraUi = [];

        var go = btnTag.CreateObject(uiWrapper.transform);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Cam 1";
        var ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(() => SetCamera(_defaultPosition, _defaultRotation));
        _presetCameraUi.Add(go);

        go = btnTag.CreateObject(uiWrapper.transform);
        tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Cam 2";
        ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(() => SetCamera(new Vector3(0, 1.5f, 0), Quaternion.identity));
        _presetCameraUi.Add(go);

        go = btnTag.CreateObject(uiWrapper.transform);
        tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Cam 3";
        ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(() => SetCamera(_previousPosition, _previousRotation));
        _presetCameraUi.Add(go);

        go = btnTag.CreateObject(uiWrapper.transform);
        tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Cam 4";
        ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(() => SetCamera(_savedPosition, _savedRotation));
        _presetCameraUi.Add(go);

        go = btnTag.CreateObject(uiWrapper.transform);
        tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Save";
        ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(SaveCamera);
        _presetCameraUi.Add(go);
    }

    public void Dispose()
    {
        foreach (var gameObject in _presetCameraUi)
        {
            Object.Destroy(gameObject);
        }
    }

    private void SetCamera(Vector3 position, Quaternion rotation)
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

    private void SaveCamera()
    {
        var cameraWrapper = GameObject.Find("/Wrapper/BeatmapEditorUICameraWrapper");
        if (cameraWrapper == null) return;
        var comp = cameraWrapper.GetComponent<BeatmapEditor360CameraController>();

        _savedPosition = Vector3.zero + comp._uiCameraMovementTransform.position;
        _savedRotation = Quaternion.identity * comp._uiCameraTransform.rotation;
    }
}