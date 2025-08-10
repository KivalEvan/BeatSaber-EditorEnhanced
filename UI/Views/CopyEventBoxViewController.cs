using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class CopyEventBoxViewController : IInitializable
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly SignalBus _signalBus;
    private readonly UIBuilder _uiBuilder;

    private bool _copyEvent;
    private EventBoxesView _ebv;
    private bool _increment;
    private bool _randomSeed;

    public CopyEventBoxViewController(SignalBus signalBus,
        EditBeatmapViewController ebvc,
        UIBuilder uiBuilder)
    {
        _signalBus = signalBus;
        _ebvc = ebvc;
        _uiBuilder = uiBuilder;
    }

    public void Initialize()
    {
        _ebv = _ebvc._editBeatmapRightPanelView._panels[2].elements[0].GetComponent<EventBoxesView>();
        var target = _ebv._eventBoxView;
        var replacement = target.transform.GetChild(0);
        replacement.gameObject.SetActive(false);

        var stackTag = _uiBuilder.LayoutStack.Instantiate()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var verticalTag = _uiBuilder.LayoutVertical.Instantiate()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetPadding(new RectOffset(4, 4, 4, 4));
        var horizontalTag = _uiBuilder.LayoutHorizontal.Instantiate()
            .SetChildAlignment(TextAnchor.LowerCenter)
            .SetChildControlWidth(true)
            .SetSpacing(8)
            .SetPadding(new RectOffset(4, 4, 2, 2));
        var btnTag = _uiBuilder.Button.Instantiate()
            .SetFontSize(16);
        var checkboxTag = _uiBuilder.Checkbox.Instantiate()
            .SetSize(28)
            .SetFontSize(16);

        var container = stackTag.Create(target.transform);
        container.transform.SetAsFirstSibling();
        Object.Instantiate(target.transform.Find("GroupInfoView/Background4px"), container.transform,
            false);
        container = verticalTag.Create(container.transform);
        var layout = horizontalTag.Create(container.transform);

        replacement.GetChild(1).SetParent(layout.transform);
        btnTag
            .SetText("Copy")
            .SetOnClick(CopyEventBox)
            .Create(layout.transform);
        btnTag
            .SetText("Paste")
            .SetOnClick(PasteEventBox)
            .Create(layout.transform);
        btnTag
            .SetText("Duplicate")
            .SetOnClick(DuplicateEventBox)
            .Create(layout.transform);
        layout = horizontalTag.Create(container.transform);
        checkboxTag
            .SetText("Copy Event")
            .SetBool(_copyEvent)
            .SetOnValueChange(val => _copyEvent = val)
            .Create(layout.transform);
        checkboxTag
            .SetText("Random Seed")
            .SetBool(_randomSeed)
            .SetOnValueChange(val => _randomSeed = val)
            .Create(layout.transform);
        checkboxTag
            .SetText("Increment ID")
            .SetBool(_increment)
            .SetOnValueChange(val => _increment = val)
            .Create(layout.transform);
    }

    private void CopyEventBox()
    {
        _signalBus.Fire(new CopyEventBoxSignal(_ebv._eventBoxView._eventBox));
    }

    private void PasteEventBox()
    {
        _signalBus.Fire(new PasteEventBoxSignal(_ebv._eventBoxView._eventBox, _copyEvent, _randomSeed, _increment));
    }

    private void DuplicateEventBox()
    {
        _signalBus.Fire(
            new DuplicateEventBoxSignal(_ebv._eventBoxView._eventBox, _copyEvent, _randomSeed, _increment));
    }
}