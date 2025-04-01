using System;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using TMPro;
using Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class CopyEventBoxViewController(
    SignalBus signalBus,
    BeatmapFlowCoordinator bfc,
    EditorLayoutStackBuilder editorLayoutStack,
    EditorLayoutHorizontalBuilder editorLayoutHorizontal,
    EditorButtonBuilder editorBtn,
    EditorTextBuilder editorText) : IInitializable, IDisposable
{
    public void Initialize()
    {
        var target = bfc._editBeatmapViewController._eventBoxesView._eventBoxView;

        var stackTag = editorLayoutStack.CreateNew()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var horizontalTag = editorLayoutHorizontal.CreateNew()
            .SetChildAlignment(TextAnchor.LowerCenter)
            .SetChildControlWidth(false)
            .SetPadding(new RectOffset(8, 8, 8, 8));
        var btnTag = editorBtn.CreateNew()
            .SetFontSize(16);
        var textTag = editorText.CreateNew()
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

    public void Dispose()
    {
    }

    private void CopyEventBox()
    {
        signalBus.Fire(new CopyEventBoxSignal(bfc._editBeatmapViewController._eventBoxesView._eventBoxView._eventBox));
    }

    private void PasteEventBox()
    {
        signalBus.Fire(new PasteEventBoxSignal(bfc._editBeatmapViewController._eventBoxesView._eventBoxView._eventBox));
    }

    private void DuplicateEventBox()
    {
        signalBus.Fire(
            new DuplicateEventBoxSignal(bfc._editBeatmapViewController._eventBoxesView._eventBoxView._eventBox));
    }
}