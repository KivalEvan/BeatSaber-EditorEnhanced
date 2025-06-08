using System;
using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class CopyEventBoxViewController : IInitializable, IDisposable
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly EditorButtonBuilder _editorBtn;
    private readonly EditorLayoutHorizontalBuilder _editorLayoutHorizontal;
    private readonly EditorLayoutStackBuilder _editorLayoutStack;
    private readonly EditorTextBuilder _editorText;
    private readonly SignalBus _signalBus;
    private EventBoxesView _ebv;

    public CopyEventBoxViewController(SignalBus signalBus,
        EditBeatmapViewController ebvc,
        EditorLayoutStackBuilder editorLayoutStack,
        EditorLayoutHorizontalBuilder editorLayoutHorizontal,
        EditorButtonBuilder editorBtn,
        EditorTextBuilder editorText)
    {
        _signalBus = signalBus;
        _ebvc = ebvc;
        _editorLayoutStack = editorLayoutStack;
        _editorLayoutHorizontal = editorLayoutHorizontal;
        _editorBtn = editorBtn;
        _editorText = editorText;
    }

    public void Dispose()
    {
    }

    public void Initialize()
    {
        _ebv = _ebvc._eventBoxesView;
        var target = _ebvc._eventBoxesView._eventBoxView;

        var stackTag = _editorLayoutStack.CreateNew()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var horizontalTag = _editorLayoutHorizontal.CreateNew()
            .SetChildAlignment(TextAnchor.LowerCenter)
            .SetChildControlWidth(false)
            .SetPadding(new RectOffset(8, 8, 8, 8));
        var btnTag = _editorBtn.CreateNew()
            .SetFontSize(16);
        var textTag = _editorText.CreateNew()
            .SetFontSize(20)
            .SetFontWeight(FontWeight.Bold)
            .SetTextAlignment(TextAlignmentOptions.Center);

        var container = stackTag.CreateObject(target.transform);
        container.transform.SetAsFirstSibling();
        Object.Instantiate(target.transform.Find("GroupInfoView/Background4px"), container.transform,
            false);
        var layout = horizontalTag.CreateObject(container.transform);
        textTag
            .SetText("COPY")
            .CreateObject(layout.transform);
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
        textTag
            .SetFontSize(16)
            .SetFontWeight(FontWeight.Regular)
            .SetTextAlignment(TextAlignmentOptions.Left)
            .SetText("Increment")
            .CreateObject(layout.transform);
    }

    private void CopyEventBox()
    {
        _signalBus.Fire(new CopyEventBoxSignal(_ebv._eventBoxView._eventBox));
    }

    private void PasteEventBox()
    {
        _signalBus.Fire(new PasteEventBoxSignal(_ebv._eventBoxView._eventBox));
    }

    private void DuplicateEventBox()
    {
        _signalBus.Fire(
            new DuplicateEventBoxSignal(_ebv._eventBoxView._eventBox));
    }
}