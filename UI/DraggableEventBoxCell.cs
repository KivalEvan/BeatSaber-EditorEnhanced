using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Commands.LevelEditor;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.Types;
using EditorEnhanced.Commands;
using HMUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.Windows;
using Zenject;

namespace EditorEnhanced.UI;

public class DraggableEventBoxCell(SignalBus signalBus, EditBeatmapViewController ebvc) : IInitializable, IDisposable
{
    public void Initialize()
    {
        SegmentedControlCell[] l =
        [
            ebvc._eventBoxesView._eventBoxButtonsTextSegmentedControl._firstCellPrefab,
            ebvc._eventBoxesView._eventBoxButtonsTextSegmentedControl._middleCellPrefab,
            ebvc._eventBoxesView._eventBoxButtonsTextSegmentedControl._lastCellPrefab,
            ebvc._eventBoxesView._eventBoxButtonsTextSegmentedControl._singleCellPrefab
        ];

        foreach (var cell in l)
        {
            var comp = cell.gameObject.AddComponent<DragSwapSegmentCell>();
            comp.currentCell = cell;
            comp.segmentedControl = ebvc._eventBoxesView._eventBoxButtonsTextSegmentedControl;
        }

        ApplyAction();

        signalBus.Subscribe<BeatmapEditingModeSwitched>(ApplyActionWithSignal);
        signalBus.Subscribe<EventBoxesUpdatedSignal>(ApplyAction);
        signalBus.Subscribe<ModifyEventBoxSignal>(ApplyAction);
        signalBus.Subscribe<InsertEventBoxSignal>(ApplyAction);
        signalBus.Subscribe<InsertEventBoxesForAllAxesSignal>(ApplyAction);
        signalBus.Subscribe<InsertEventBoxesForAllIdsSignal>(ApplyAction);
        signalBus.Subscribe<InsertEventBoxesForAllIdsAndAxisSignal>(ApplyAction);
        signalBus.Subscribe<DeleteEventBoxSignal>(ApplyAction);
    }

    public void Dispose()
    {
        signalBus.TryUnsubscribe<BeatmapEditingModeSwitched>(ApplyActionWithSignal);
        signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(ApplyAction);
        signalBus.TryUnsubscribe<ModifyEventBoxSignal>(ApplyAction);
        signalBus.TryUnsubscribe<InsertEventBoxSignal>(ApplyAction);
        signalBus.TryUnsubscribe<InsertEventBoxesForAllAxesSignal>(ApplyAction);
        signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsSignal>(ApplyAction);
        signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsAndAxisSignal>(ApplyAction);
        signalBus.TryUnsubscribe<DeleteEventBoxSignal>(ApplyAction);
    }

    private void ApplyAction()
    {
        foreach (var cell in ebvc._eventBoxesView._eventBoxButtonsTextSegmentedControl.cells)
        {
            var component = cell.gameObject.GetComponent<DragSwapSegmentCell>();
            if (component != null) component.OnSwapAction ??= Reorder;
        }
    }

    private void ApplyActionWithSignal(BeatmapEditingModeSwitched signal)
    {
        if (signal.mode != BeatmapEditingMode.EventBoxes) return;
        ApplyAction();
    }

    private void Reorder(int index, int moved)
    {
        signalBus.Fire(new ReorderEventBoxSignal(ebvc._eventBoxesView._eventBoxes[index],
            ReorderType.Any, index + moved));
    }
}

public class DragSwapSegmentCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private Vector2 _startPos;
    private int _startIndex;

    public SegmentedControlCell currentCell;
    public SegmentedControl segmentedControl;
    public Action<int, int> OnSwapAction;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = transform.position with { y = eventData.position.y };
        foreach (var segmentedControlCell in segmentedControl.cells)
        {
            if (!segmentedControlCell.gameObject.activeSelf) continue;
            if (currentCell == segmentedControlCell) continue;
            if (!AnchoredPositionWithinEachOther(_rectTransform, segmentedControlCell.GetComponent<RectTransform>()))
                continue;
            transform.SetSiblingIndex(segmentedControlCell.transform.GetSiblingIndex());
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _startPos = _rectTransform.anchoredPosition;
        _startIndex = transform.GetSiblingIndex();
        currentCell.interactable = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (transform.GetSiblingIndex() != _startIndex)
        {
            OnSwapAction?.Invoke(currentCell.cellNumber, transform.GetSiblingIndex() - _startIndex);
        }
        else
        {
            _rectTransform.anchoredPosition = _startPos;
        }

        currentCell.interactable = true;
    }

    private static bool AnchoredPositionWithinEachOther(RectTransform current, RectTransform other)
    {
        return current.anchoredPosition.y > other.anchoredPosition.y - other.rect.height / 2f &&
               current.anchoredPosition.y < other.anchoredPosition.y + other.rect.height / 2f;
    }
}