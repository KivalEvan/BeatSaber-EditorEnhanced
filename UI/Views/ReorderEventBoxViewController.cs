using System;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class ReorderEventBoxViewController : IInitializable, IDisposable
{
    [Inject] private readonly EditBeatmapViewController _ebvc;
    [Inject] private readonly EditorButtonBuilder _editorBtn;
    [Inject] private readonly EditorLayoutHorizontalBuilder _editorLayoutHorizontal;
    [Inject] private readonly EditorLayoutStackBuilder _editorLayoutStack;
    [Inject] private readonly EditorTextBuilder _editorText;
    [Inject] private readonly SignalBus _signalBus;

    public void Dispose()
    {
    }

    public void Initialize()
    {
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

    private void MoveEventBoxTop()
    {
        _signalBus.Fire(new ReorderEventBoxSignal(_ebvc._eventBoxesView._eventBoxView._eventBox,
            ReorderType.Top));
    }

    private void MoveEventBoxUp()
    {
        _signalBus.Fire(new ReorderEventBoxSignal(_ebvc._eventBoxesView._eventBoxView._eventBox,
            ReorderType.Up));
    }

    private void MoveEventBoxDown()
    {
        _signalBus.Fire(new ReorderEventBoxSignal(_ebvc._eventBoxesView._eventBoxView._eventBox,
            ReorderType.Down));
    }

    private void MoveEventBoxBottom()
    {
        _signalBus.Fire(new ReorderEventBoxSignal(_ebvc._eventBoxesView._eventBoxView._eventBox,
            ReorderType.Bottom));
    }
}