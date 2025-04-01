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

internal class CameraPresetViewController(CameraPresetManager cameraPresetManager, BeatmapFlowCoordinator bfc, TimeTweeningManager twm)
    : IInitializable, IDisposable
{
    public void Initialize()
    {
        var target = bfc._editBeatmapViewController._beatmapEditorExtendedSettingsView;

        var horizontalTag = new EditorLayoutHorizontalTag()
            .SetSpacing(5)
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetChildControlWidth(false);
        var stackTag = new EditorLayoutStackTag()
            .SetFlexibleWidth(1)
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var btnTag = new EditorButtonTag(bfc, twm)
            .SetFontSize(14)
            .SetTextAlignment(TextAlignmentOptions.Center);
        var textTag = new EditorTextTag(bfc)
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
            .SetText("Prev")
            .SetOnClick(SwitchToPreviousCamera)
            .CreateObject(mainLayout.transform);
        btnTag
            .SetText("Load")
            .SetOnClick(SwitchToSavedCamera)
            .CreateObject(mainLayout.transform);
        btnTag
            .SetText("Save")
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