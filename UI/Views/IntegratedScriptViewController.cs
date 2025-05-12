using BeatmapEditor3D;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class IntegratedScriptViewController : IInitializable
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly EditorButtonBuilder _editorButtonBuilder;
    private readonly EditorLayoutHorizontalBuilder _editorEditorLayoutHorizontalBuilder;
    private readonly EditorTextBuilder _editorTextBuilder;
    private GameObject _view;

    public IntegratedScriptViewController(EditBeatmapViewController ebvc,
        EditorButtonBuilder editorButtonBuilder,
        EditorTextBuilder editorTextBuilder,
        EditorLayoutStackBuilder editorEditorLayoutStackBuilder,
        EditorLayoutHorizontalBuilder editorEditorLayoutHorizontalBuilder)
    {
        _ebvc = ebvc;
        _editorButtonBuilder = editorButtonBuilder;
        _editorTextBuilder = editorTextBuilder;
        _editorEditorLayoutHorizontalBuilder = editorEditorLayoutHorizontalBuilder;
    }

    public void Initialize()
    {
        var buttonTag = _editorButtonBuilder.CreateNew();
        var stackTag = _editorEditorLayoutHorizontalBuilder.CreateNew();
        var textTag = _editorTextBuilder.CreateNew();

        _view = stackTag.CreateObject(_ebvc.transform);
        textTag.SetText("Integrated Script").CreateObject(_view.transform);

        buttonTag.SetText("Integrated Script").SetOnClick(ToggleView)
            .CreateObject(_ebvc._beatmapEditorExtendedSettingsView.transform);
    }

    private void ToggleView()
    {
        _view.SetActive(!_view.activeSelf);
    }
}