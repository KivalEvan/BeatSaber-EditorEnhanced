using System;
using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using Tweening;
using UnityEngine;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleButtonBuilder : IEditorBuilder<EditorToggleButtonTag>
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly TimeTweeningManager _twm;

    public EditorToggleButtonBuilder(EditBeatmapViewController ebvc, TimeTweeningManager twm)
    {
        _ebvc = ebvc;
        _twm = twm;
    }

    public EditorToggleButtonTag Instantiate()
    {
        return new EditorToggleButtonTag(_ebvc, _twm);
    }
}

public class EditorToggleButtonTag : IEditorTag
{
    public EditorToggleButtonTag(EditBeatmapViewController ebvc, TimeTweeningManager twm)
    {
    }

    public string Name { get; set; } = "EditorToggleButton";

    public GameObject Create(Transform parent)
    {
        throw new NotImplementedException();
    }
}