using BeatmapEditor3D;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class IntegratedScriptViewController(
    EditBeatmapViewController ebvc,
    EditorButtonBuilder editorButtonBuilder,
    EditorTextBuilder editorTextBuilder,
    EditorLayoutStackBuilder editorEditorLayoutStackBuilder,
    EditorLayoutHorizontalBuilder editorEditorLayoutHorizontalBuilder) : IInitializable
{
    private GameObject _view;
    
    public void Initialize()
    {
        var buttonTag = editorButtonBuilder.CreateNew();
        var stackTag = editorEditorLayoutHorizontalBuilder.CreateNew();
        var textTag = editorTextBuilder.CreateNew();
        
        _view = stackTag.CreateObject(ebvc.transform);
        textTag.SetText("Integrated Script").CreateObject(_view.transform);
        
        buttonTag.SetText("Integrated Script").SetOnClick(ToggleView)
            .CreateObject(ebvc._beatmapEditorExtendedSettingsView.transform);
    }

    private void ToggleView()
    {
        _view.SetActive(!_view.activeSelf);
    }
}