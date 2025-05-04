using System;
using BeatmapEditor3D;
using EditorEnhanced.Managers;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using TMPro;
using Tweening;
using UnityEngine.UI;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class CameraPresetViewController(
    CameraPresetManager cameraPresetManager,
    BeatmapFlowCoordinator bfc,
    EditorLayoutStackBuilder editorLayoutStack,
    EditorLayoutHorizontalBuilder editorLayoutHorizontal,
    EditorButtonBuilder editorBtn,
    EditorButtonWithIconBuilder editorBtnIcon,
    EditorTextBuilder editorText)
    : IInitializable, IDisposable
{
    public void Initialize()
    {
        var target = bfc._editBeatmapViewController._beatmapEditorExtendedSettingsView;

        var horizontalTag = editorLayoutHorizontal.CreateNew()
            .SetSpacing(5)
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetChildControlWidth(false);
        var stackTag = editorLayoutStack.CreateNew()
            .SetFlexibleWidth(1)
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var btnTag = editorBtn.CreateNew()
            .SetFontSize(14)
            .SetTextAlignment(TextAlignmentOptions.Center);
        var btnIconTag = editorBtnIcon.CreateNew()
            .SetFontSize(14)
            .SetTextAlignment(TextAlignmentOptions.Center);
        var textTag = editorText.CreateNew()
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

    public void Dispose()
    {
    }

    public void SwitchToDefaultCamera()
    {
        cameraPresetManager.SetCamera(CameraPresetManager.CameraType.Default);
    }

    public void SwitchToPlayerCamera()
    {
        cameraPresetManager.SetCamera(CameraPresetManager.CameraType.Player);
    }

    public void SwitchToPreviousCamera()
    {
        cameraPresetManager.SetCamera(CameraPresetManager.CameraType.Previous);
    }

    public void SwitchToSavedCamera()
    {
        cameraPresetManager.SetCamera(CameraPresetManager.CameraType.Saved);
    }

    public void SaveCamera()
    {
        cameraPresetManager.SaveCamera();
    }
}