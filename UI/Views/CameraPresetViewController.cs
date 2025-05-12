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
    [Inject] private readonly CameraPresetManager _cameraPresetManager;
    [Inject] private readonly EditBeatmapViewController _ebvc;
    [Inject] private readonly EditorButtonBuilder _editorBtn;
    [Inject] private readonly EditorButtonWithIconBuilder _editorBtnIcon;
    [Inject] private readonly EditorLayoutHorizontalBuilder _editorLayoutHorizontal;
    [Inject] private readonly EditorLayoutStackBuilder _editorLayoutStack;
    [Inject] private readonly EditorTextBuilder _editorText;

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