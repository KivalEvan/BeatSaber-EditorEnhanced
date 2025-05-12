using BeatmapEditor3D;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class MassValueShiftViewController : IInitializable
{
    [Inject] private readonly EditBeatmapViewController _ebvc;
    [Inject] private readonly EditorButtonBuilder _editorBtnBuilder;
    [Inject] private readonly EditorLayoutHorizontalBuilder _editorLayoutHorizontalBuilder;
    [Inject] private readonly EditorLayoutStackBuilder _editorLayoutStackBuilder;
    [Inject] private readonly EditorTextBuilder _editorTextBuilder;
    private GameObject _view;

    public void Initialize()
    {
        var targetBtn = _ebvc._beatmapEditorExtendedSettingsView;

        var stackTag = _editorLayoutStackBuilder.CreateNew();
        var horizontalTag = _editorLayoutHorizontalBuilder.CreateNew();
        var btnTag = _editorBtnBuilder.CreateNew();
        var textTag = _editorTextBuilder.CreateNew();

        _view = stackTag
            .SetAnchorMin(new Vector2(0, 1))
            .SetAnchorMax(new Vector2(0, 1))
            .SetOffsetMin(new Vector2(0, -80))
            .CreateObject(_ebvc.transform);
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