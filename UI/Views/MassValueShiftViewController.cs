using BeatmapEditor3D;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class MassValueShiftViewController : IInitializable
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly UIBuilder _uiBuilder;
    private GameObject _view;

    public MassValueShiftViewController(EditBeatmapViewController ebvc,
        UIBuilder uiBuilder)
    {
        _ebvc = ebvc;
        _uiBuilder = uiBuilder;
    }

    public void Initialize()
    {
        var targetBtn = _ebvc._beatmapEditorExtendedSettingsView;

        var stackTag = _uiBuilder.LayoutStack.Instantiate();
        var horizontalTag = _uiBuilder.LayoutHorizontal.Instantiate();
        var btnTag = _uiBuilder.Button.Instantiate();
        var textTag = _uiBuilder.Text.Instantiate();

        _view = stackTag
            .SetAnchorMin(new Vector2(0, 1))
            .SetAnchorMax(new Vector2(0, 1))
            .SetOffsetMin(new Vector2(0, -80))
            .Create(_ebvc.transform);
        _view.SetActive(false);
        textTag
            .SetFontSize(20)
            .SetText("Mass Shift")
            .Create(_view.transform);

        btnTag
            .SetText("Mass Shift")
            .SetOnClick(ToggleActive)
            .Create(targetBtn.transform);
    }

    private void ToggleActive()
    {
        _view.SetActive(!_view.activeSelf);
    }
}