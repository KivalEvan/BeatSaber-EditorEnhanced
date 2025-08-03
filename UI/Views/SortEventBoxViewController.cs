using System;
using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class SortEventBoxViewController : IInitializable, IDisposable
{
    private readonly DiContainer _container;
    private readonly EditBeatmapViewController _ebvc;
    private readonly EditorButtonBuilder _editorBtn;
    private readonly EditorCheckboxBuilder _editorCheckbox;
    private readonly EditorLayoutHorizontalBuilder _editorLayoutHorizontal;
    private readonly EditorLayoutStackBuilder _editorLayoutStack;
    private readonly EditorLayoutVerticalBuilder _editorLayoutVertical;
    private readonly EditorTextBuilder _editorText;
    private readonly SignalBus _signalBus;

    private EventBoxesView _ebv;

    public SortEventBoxViewController(SignalBus signalBus,
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
        var target =
            _ebv._eventBoxButtonsScrollView.transform.parent.parent.Find("ControlButtons/RemoveButtonsWrapper");
        if (target == null) return;

        // target.parent.parent.gameObject.AddComponent<VerticalLayoutGroup>();
        // var csf = target.parent.parent.gameObject.AddComponent<ContentSizeFitter>();
        // csf.verticalFit  = ContentSizeFitter.FitMode.MinSize;

        // target.parent.gameObject.AddComponent<VerticalLayoutGroup>();
        // var csf = target.parent.gameObject.AddComponent<ContentSizeFitter>();

        _ebv._eventBoxButtonsScrollView.transform.parent.localPosition = new Vector3(-20f, -85f, 0f);

        var instance = Object.Instantiate(target.gameObject);
        instance.name = "SortButtonsWrapper";
        instance.transform.SetParent(target.parent, false);

        instance.transform.localPosition = new Vector3(40f, -80f, 0f);
        var behev = instance.GetComponent<BeatmapEditorHoverExpandView>();
        for (var i = behev._content.childCount - 1; i >= 0; i--) Object.Destroy(behev._content.GetChild(i).gameObject);

        var btnTag = _editorBtn.CreateNew()
            .SetSize(new Vector2(40f, 40f))
            .SetPadding(new RectOffset(0, 0, 0, 0))
            .SetChildForceExpandWidth(true)
            .SetChildForceExpandHeight(true)
            .SetFontSize(12f);

        btnTag
            .SetText("Sort\nAxis")
            .SetOnClick(SortAxisHandler)
            .CreateObject(behev._content);
        btnTag
            .SetText("Sort\nID")
            .SetOnClick(SortIdHandler)
            .CreateObject(behev._content);
    }

    private void SortAxisHandler()
    {
        _signalBus.Fire(new SortAxisEventBoxGroupSignal());
    }

    private void SortIdHandler()
    {
        _signalBus.Fire(new SortIdEventBoxGroupSignal());
    }
}