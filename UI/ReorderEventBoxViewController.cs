using System;
using System.Collections.Generic;
using BeatmapEditor3D.Views;
using BeatSaberMarkupLanguage.Tags;
using EditorEnhanced.Commands;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI;

internal class ReorderEventBoxViewController(SignalBus signalBus) : IInitializable, IDisposable
{
    private List<GameObject> _uiObjects;
    private EventBoxView _eventBoxView;

    public void Initialize()
    {
        var hlTag = new HorizontalLayoutTag();
        var btnTag = new EditorButtonTextTag();
        var labelTag = new EditorLabelTag();
        
        var uiWrapper =
            GameObject.Find(
                "/Wrapper/ViewControllers/EditBeatmapViewController/EventBoxesView/EventBoxView");
        if (uiWrapper == null) return;
        _eventBoxView = uiWrapper.GetComponent<EventBoxView>();
        _uiObjects = [];

        var container = new GameObject("ReorderEventBoxView");
        container.transform.SetParent(uiWrapper.transform, false);
        container.transform.SetAsFirstSibling();
        container.AddComponent<LayoutElement>();
        container.AddComponent<StackLayoutGroup>();
        var contentSizeFitter = container.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _uiObjects.Add(container);

        var bg = Object.Instantiate(uiWrapper.transform.Find("GroupInfoView/Background4px"), container.transform,
            false);

        var layout = hlTag.CreateObject(container.transform);
        var horizontalLayoutGroup = layout.GetComponent<HorizontalLayoutGroup>();
        horizontalLayoutGroup.childAlignment = TextAnchor.LowerCenter;
        horizontalLayoutGroup.childControlWidth = false;
        horizontalLayoutGroup.padding = new RectOffset(8, 8, 8, 8);
        
        var go = labelTag.CreateObject(layout.transform);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "REORDER";
        tmp.fontSize = 18;
        tmp.fontWeight = FontWeight.Bold;

        go = btnTag.CreateObject(layout.transform);
        tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Top";
        tmp.fontSize = 16;
        var ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(MoveEventBoxTop);
        _uiObjects.Add(go);

        go = btnTag.CreateObject(layout.transform);
        tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Up";
        tmp.fontSize = 16;
        ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(MoveEventBoxUp);
        _uiObjects.Add(go);

        go = btnTag.CreateObject(layout.transform);
        tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Down";
        tmp.fontSize = 16;
        ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(MoveEventBoxDown);
        _uiObjects.Add(go);

        go = btnTag.CreateObject(layout.transform);
        tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Bottom";
        tmp.fontSize = 16;
        ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(MoveEventBoxBottom);
        _uiObjects.Add(go);
    }

    public void Dispose()
    {
        foreach (var gameObject in _uiObjects)
        {
            Object.Destroy(gameObject);
        }
    }

    private void MoveEventBoxTop()
    {
        signalBus.Fire(new ReorderEventBoxSignal(_eventBoxView._eventBox, ReorderType.Top));
    }

    private void MoveEventBoxUp()
    {
        signalBus.Fire(new ReorderEventBoxSignal(_eventBoxView._eventBox, ReorderType.Up));
    }

    private void MoveEventBoxDown()
    {
        signalBus.Fire(new ReorderEventBoxSignal(_eventBoxView._eventBox, ReorderType.Down));
    }

    private void MoveEventBoxBottom()
    {
        signalBus.Fire(new ReorderEventBoxSignal(_eventBoxView._eventBox, ReorderType.Bottom));
    }
}