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

internal class ReorderEventBoxViewController(SignalBus signalBus, BeatmapFlowCoordinator bfc, TimeTweeningManager twm)
    : IInitializable, IDisposable
{
    public void Initialize()
    {
        var target = bfc._editBeatmapViewController._eventBoxesView._eventBoxView;

        var stackTag = new EditorLayoutStackTag()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var horizontalTag = new EditorLayoutHorizontalTag()
            .SetChildAlignment(TextAnchor.LowerCenter)
            .SetChildControlWidth(false)
            .SetPadding(new RectOffset(8, 8, 8, 8));
        var btnTag = new EditorButtonTag(bfc, twm)
            .SetFontSize(16);
        var textTag = new EditorTextTag(bfc)
            .SetFontSize(20)
            .SetFontWeight(FontWeight.Bold)
            .SetTextAlignment(TextAlignmentOptions.Center);

        var container = stackTag.CreateObject(target.transform);
        container.transform.SetAsFirstSibling();
        Object.Instantiate(target.transform.Find("GroupInfoView/Background4px"), container.transform,
            false);
        var layout = horizontalTag.CreateObject(container.transform);
        textTag
            .SetText("REORDER")
            .CreateObject(layout.transform);
        btnTag
            .SetText("Top")
            .SetOnClick(MoveEventBoxTop)
            .CreateObject(layout.transform);
        btnTag
            .SetText("Up")
            .SetOnClick(MoveEventBoxUp)
            .CreateObject(layout.transform);
        btnTag
            .SetText("Down")
            .SetOnClick(MoveEventBoxDown)
            .CreateObject(layout.transform);
        btnTag
            .SetText("Bottom")
            .SetOnClick(MoveEventBoxBottom)
            .CreateObject(layout.transform);
    }

    public void Dispose()
    {
    }

    private void MoveEventBoxTop()
    {
        signalBus.Fire(new ReorderEventBoxSignal(bfc._editBeatmapViewController._eventBoxesView._eventBoxView._eventBox,
            ReorderType.Top));
    }

    private void MoveEventBoxUp()
    {
        signalBus.Fire(new ReorderEventBoxSignal(bfc._editBeatmapViewController._eventBoxesView._eventBoxView._eventBox,
            ReorderType.Up));
    }

    private void MoveEventBoxDown()
    {
        signalBus.Fire(new ReorderEventBoxSignal(bfc._editBeatmapViewController._eventBoxesView._eventBoxView._eventBox,
            ReorderType.Down));
    }

    private void MoveEventBoxBottom()
    {
        signalBus.Fire(new ReorderEventBoxSignal(bfc._editBeatmapViewController._eventBoxesView._eventBoxView._eventBox,
            ReorderType.Bottom));
    }
}