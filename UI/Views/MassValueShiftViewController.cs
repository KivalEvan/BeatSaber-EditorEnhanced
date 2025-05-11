using BeatmapEditor3D;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class MassValueShiftViewController(
    EditBeatmapViewController ebvc,
    EditorLayoutStackBuilder editorLayoutStackBuilder,
    EditorLayoutHorizontalBuilder editorLayoutHorizontalBuilder,
    EditorButtonBuilder editorBtnBuilder,
    EditorTextBuilder editorTextBuilder) : IInitializable
{
    private GameObject _view;

    public void Initialize()
    {
        var targetBtn = ebvc._beatmapEditorExtendedSettingsView;

        var stackTag = editorLayoutStackBuilder.CreateNew();
        var horizontalTag = editorLayoutHorizontalBuilder.CreateNew();
        var btnTag = editorBtnBuilder.CreateNew();
        var textTag = editorTextBuilder.CreateNew();

        _view = stackTag
            .SetAnchorMin(new Vector2(0, 1))
            .SetAnchorMax(new Vector2(0, 1))
            .SetOffsetMin(new Vector2(0, -80))
            .CreateObject(ebvc.transform);
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