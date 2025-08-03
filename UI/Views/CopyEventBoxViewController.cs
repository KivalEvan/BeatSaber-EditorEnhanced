using System;
using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class CopyEventBoxViewController : IInitializable, IDisposable
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly EditorButtonBuilder _editorBtn;
    private readonly EditorCheckboxBuilder _editorCheckbox;
    private readonly EditorLayoutHorizontalBuilder _editorLayoutHorizontal;
    private readonly EditorLayoutStackBuilder _editorLayoutStack;
    private readonly EditorLayoutVerticalBuilder _editorLayoutVertical;
    private readonly EditorTextBuilder _editorText;
    private readonly SignalBus _signalBus;

    private bool _copyEvent;
    private EventBoxesView _ebv;
    private bool _increment;
    private bool _randomSeed;

    public CopyEventBoxViewController(SignalBus signalBus,
        EditBeatmapViewController ebvc,
        EditorLayoutStackBuilder editorLayoutStack,
        EditorLayoutVerticalBuilder editorLayoutVertical,
        EditorLayoutHorizontalBuilder editorLayoutHorizontal,
        EditorButtonBuilder editorBtn,
        EditorCheckboxBuilder editorCheckbox,
        EditorTextBuilder editorText)
    {
        _signalBus = signalBus;
        _ebvc = ebvc;
        _editorLayoutStack = editorLayoutStack;
        _editorLayoutVertical = editorLayoutVertical;
        _editorLayoutHorizontal = editorLayoutHorizontal;
        _editorBtn = editorBtn;
        _editorCheckbox = editorCheckbox;
        _editorText = editorText;
    }

    public void Dispose()
    {
    }

    public void Initialize()
    {
        _ebv = _ebvc._editBeatmapRightPanelView._panels[2].elements[0].GetComponent<EventBoxesView>();
        var target = _ebv._eventBoxView;
        var replacement = target.transform.GetChild(0);
        replacement.gameObject.SetActive(false);

        var stackTag = _editorLayoutStack.CreateNew()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var verticalTag = _editorLayoutVertical.CreateNew()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetPadding(new RectOffset(4, 4, 4, 4));
        var horizontalTag = _editorLayoutHorizontal.CreateNew()
            .SetChildAlignment(TextAnchor.LowerCenter)
            .SetChildControlWidth(true)
            .SetSpacing(8)
            .SetPadding(new RectOffset(4, 4, 2, 2));
        var btnTag = _editorBtn.CreateNew()
            .SetFontSize(16);
        var checkboxTag = _editorCheckbox.CreateNew()
            .SetSize(28)
            .SetFontSize(16);

        var container = stackTag.CreateObject(target.transform);
        container.transform.SetAsFirstSibling();
        Object.Instantiate(target.transform.Find("GroupInfoView/Background4px"), container.transform,
            false);
        container = verticalTag.CreateObject(container.transform);
        var layout = horizontalTag.CreateObject(container.transform);

        replacement.GetChild(1).SetParent(layout.transform);
        btnTag
            .SetText("Copy")
            .SetOnClick(CopyEventBox)
            .CreateObject(layout.transform);
        btnTag
            .SetText("Paste")
            .SetOnClick(PasteEventBox)
            .CreateObject(layout.transform);
        btnTag
            .SetText("Duplicate")
            .SetOnClick(DuplicateEventBox)
            .CreateObject(layout.transform);
        layout = horizontalTag.CreateObject(container.transform);
        checkboxTag
            .SetText("Copy Event")
            .SetBool(_copyEvent)
            .SetOnValueChange(val => _copyEvent = val)
            .CreateObject(layout.transform);
        checkboxTag
            .SetText("Random Seed")
            .SetBool(_randomSeed)
            .SetOnValueChange(val => _randomSeed = val)
            .CreateObject(layout.transform);
        checkboxTag
            .SetText("Increment ID")
            .SetBool(_increment)
            .SetOnValueChange(val => _increment = val)
            .CreateObject(layout.transform);
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