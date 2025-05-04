using System;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class MassValueShiftViewController(
    BeatmapFlowCoordinator bfc,
    EditorLayoutStackBuilder editorLayoutStack,
    EditorLayoutHorizontalBuilder editorLayoutHorizontal,
    EditorButtonBuilder editorBtn,
    EditorTextBuilder editorText) : IInitializable
{
    private GameObject _view;

    public void Initialize()
    {
        var targetView = bfc._editBeatmapViewController;
        var targetBtn = bfc._editBeatmapViewController._beatmapEditorExtendedSettingsView;

        var stackTag = editorLayoutStack.CreateNew();
        var horizontalTag = editorLayoutHorizontal.CreateNew();
        var btnTag = editorBtn.CreateNew();
        var textTag = editorText.CreateNew();

        _view = stackTag
            .SetAnchorMin(new Vector2(0, 1))
            .SetAnchorMax(new Vector2(0, 1))
            .SetOffsetMin(new Vector2(0, -80))
            .CreateObject(targetView.transform);
        _view.SetActive(false);
        textTag
            .SetFontSize(20)
            .SetText("Mass Shift")
            .CreateObject(_view.transform);

        btnTag
            .SetText("Mass Shift")
            .SetOnClick(ToggleActive)
            .CreateObject(targetBtn.transform);
    }

    private void ToggleActive()
    {
        _view.SetActive(!_view.activeSelf);
    }
}