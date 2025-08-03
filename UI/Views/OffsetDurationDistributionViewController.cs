using System;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class OffsetDurationDistributionViewController : IInitializable, IDisposable
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly EditorButtonBuilder _editorButton;

    private EventBoxView _ebv;

    public OffsetDurationDistributionViewController(
        EditBeatmapViewController ebvc,
        EditorButtonBuilder editorButton)
    {
        _ebvc = ebvc;
        _editorButton = editorButton;
    }

    public void Dispose()
    {
    }

    public void Initialize()
    {
        _ebv = _ebvc._editBeatmapRightPanelView._panels.First(p => p.panelType == BeatmapPanelType.EventBox)
            .elements[0].GetComponent<EventBoxesView>()._eventBoxView;

        var buttonTag = _editorButton.CreateNew()
            .SetFontSize(16);

        buttonTag
            .SetText("-0.001")
            .SetOnClick(OffsetNegative)
            .CreateObject(_ebv._beatDistributionInput.transform.parent)
            .transform.localPosition = new Vector3(160f, 25f, 0f);
        buttonTag
            .SetText("+0.001")
            .SetOnClick(OffsetPositive)
            .CreateObject(_ebv._beatDistributionInput.transform.parent)
            .transform.localPosition = new Vector3(240f, 25f, 0f);
    }

    private void OffsetNegative()
    {
        _ebv._beatDistributionInput.SetValue(_ebv._eventBox.beatDistributionParam - 0.001f);
    }

    private void OffsetPositive()
    {
        _ebv._beatDistributionInput.SetValue(_ebv._eventBox.beatDistributionParam + 0.001f);
    }
}