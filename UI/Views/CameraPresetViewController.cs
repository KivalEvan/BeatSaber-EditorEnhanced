using System;
using BeatmapEditor3D;
using EditorEnhanced.Managers;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using TMPro;
using UnityEngine.UI;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class CameraPresetViewController : IInitializable, IDisposable
{
    private readonly CameraPresetManager _cameraPresetManager;
    private readonly EditBeatmapViewController _ebvc;
    private readonly EditorButtonBuilder _editorBtn;
    private readonly EditorButtonWithIconBuilder _editorBtnIcon;
    private readonly EditorLayoutHorizontalBuilder _editorLayoutHorizontal;
    private readonly EditorLayoutStackBuilder _editorLayoutStack;
    private readonly EditorTextBuilder _editorText;

    public CameraPresetViewController(CameraPresetManager cameraPresetManager,
        EditBeatmapViewController ebvc,
        EditorLayoutStackBuilder editorLayoutStack,
        EditorLayoutHorizontalBuilder editorLayoutHorizontal,
        EditorButtonBuilder editorBtn,
        EditorButtonWithIconBuilder editorBtnIcon,
        EditorTextBuilder editorText)
    {
        _cameraPresetManager = cameraPresetManager;
        _ebvc = ebvc;
        _editorLayoutStack = editorLayoutStack;
        _editorLayoutHorizontal = editorLayoutHorizontal;
        _editorBtn = editorBtn;
        _editorBtnIcon = editorBtnIcon;
        _editorText = editorText;
    }

    public void Dispose()
    {
    }

    public void Initialize()
    {
        var target = _ebvc._beatmapEditorExtendedSettingsView;

        var horizontalTag = _editorLayoutHorizontal.CreateNew()
            .SetSpacing(5)
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetChildControlWidth(false);
        var stackTag = _editorLayoutStack.CreateNew()
            .SetFlexibleWidth(1)
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var btnTag = _editorBtn.CreateNew()
            .SetFontSize(14)
            .SetTextAlignment(TextAlignmentOptions.Center);
        var btnIconTag = _editorBtnIcon.CreateNew()
            .SetFontSize(14)
            .SetTextAlignment(TextAlignmentOptions.Center);
        var textTag = _editorText.CreateNew()
            .SetFontSize(14);

        var mainLayout = horizontalTag.CreateObject(target.transform);
        mainLayout.transform.SetSiblingIndex(target._copyDifficultyButton.transform.GetSiblingIndex());
        var stackLayout = stackTag.CreateObject(mainLayout.transform);
        textTag
            .SetText("Cam")
            .CreateObject(stackLayout.transform);
        btnTag
            .SetText("1")
            .SetOnClick(SwitchToDefaultCamera)
            .CreateObject(mainLayout.transform);
        btnTag
            .SetText("2")
            .SetOnClick(SwitchToPlayerCamera)
            .CreateObject(mainLayout.transform);
        btnTag
            .SetText("P")
            // .SetImage(nameof(EditorEnhanced) + ".cameraPrev.png")
            .SetOnClick(SwitchToPreviousCamera)
            .CreateObject(mainLayout.transform);
        btnTag
            .SetText("L")
            // .SetImage(nameof(EditorEnhanced) + ".cameraLoad.png")
            .SetOnClick(SwitchToSavedCamera)
            .CreateObject(mainLayout.transform);
        btnTag
            .SetText("S")
            // .SetImage(nameof(EditorEnhanced) + ".cameraSave.png")
            .SetOnClick(SaveCamera)
            .CreateObject(mainLayout.transform);
    }

    public void SwitchToDefaultCamera()
    {
        _cameraPresetManager.SetCamera(CameraPresetManager.CameraType.Default);
    }

    public void SwitchToPlayerCamera()
    {
        _cameraPresetManager.SetCamera(CameraPresetManager.CameraType.Player);
    }

    public void SwitchToPreviousCamera()
    {
        _cameraPresetManager.SetCamera(CameraPresetManager.CameraType.Previous);
    }

    public void SwitchToSavedCamera()
    {
        _cameraPresetManager.SetCamera(CameraPresetManager.CameraType.Saved);
    }

    public void SaveCamera()
    {
        _cameraPresetManager.SaveCamera();
    }
}