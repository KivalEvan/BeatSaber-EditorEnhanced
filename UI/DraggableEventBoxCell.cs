using System;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using HMUI;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace EditorEnhanced.UI;

public class DraggableEventBoxCell : IInitializable, IDisposable
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly SignalBus _signalBus;
    private EventBoxesView _ebv;

    public DraggableEventBoxCell(SignalBus signalBus, EditBeatmapViewController ebvc)
    {
        _signalBus = signalBus;
        _ebvc = ebvc;
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<BeatmapEditingModeSwitchedSignal>(ApplyActionWithSignal);
        _signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(ApplyAction);
        _signalBus.TryUnsubscribe<ModifyEventBoxSignal>(ApplyAction);
        _signalBus.TryUnsubscribe<InsertEventBoxSignal>(ApplyAction);
        _signalBus.TryUnsubscribe<InsertEventBoxesForAllAxesSignal>(ApplyAction);
        _signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsSignal>(ApplyAction);
        _signalBus.TryUnsubscribe<InsertEventBoxesForAllIdsAndAxisSignal>(ApplyAction);
        _signalBus.TryUnsubscribe<DeleteEventBoxSignal>(ApplyAction);
    }

    public void Initialize()
    {
        _ebv = _ebvc._editBeatmapRightPanelView._panels.First(p => p.panelType == BeatmapPanelType.EventBox).elements[0]
            .GetComponent<EventBoxesView>();

        SegmentedControlCell[] l =
        [
            _ebv._eventBoxButtonsTextSegmentedControl._firstCellPrefab,
            _ebv._eventBoxButtonsTextSegmentedControl._middleCellPrefab,
            _ebv._eventBoxButtonsTextSegmentedControl._lastCellPrefab,
            _ebv._eventBoxButtonsTextSegmentedControl._singleCellPrefab
        ];

        foreach (var cell in l)
        {
            var comp = cell.gameObject.AddComponent<DragSwapSegmentCell>();
            comp.currentCell = cell;
            comp.segmentedControl = _ebv._eventBoxButtonsTextSegmentedControl;
        }

        ApplyAction();

        _signalBus.Subscribe<BeatmapEditingModeSwitchedSignal>(ApplyActionWithSignal);
        _signalBus.Subscribe<EventBoxesUpdatedSignal>(ApplyAction);
        _signalBus.Subscribe<ModifyEventBoxSignal>(ApplyAction);
        _signalBus.Subscribe<InsertEventBoxSignal>(ApplyAction);
        _signalBus.Subscribe<InsertEventBoxesForAllAxesSignal>(ApplyAction);
        _signalBus.Subscribe<InsertEventBoxesForAllIdsSignal>(ApplyAction);
        _signalBus.Subscribe<InsertEventBoxesForAllIdsAndAxisSignal>(ApplyAction);
        _signalBus.Subscribe<DeleteEventBoxSignal>(ApplyAction);
    }

    private void ApplyAction()
    {
        foreach (var cell in _ebv._eventBoxButtonsTextSegmentedControl.cells)
        {
            var component = cell.gameObject.GetComponent<DragSwapSegmentCell>();
            if (component != null) component.OnSwapAction ??= Reorder;
        }
    }

    private void ApplyActionWithSignal(BeatmapEditingModeSwitchedSignal signal)
    {
        if (signal.mode != BeatmapEditingMode.EventBoxes) return;
        ApplyAction();
    }

    private void Reorder(int index, int moved)
    {
        _signalBus.Fire(new ReorderEventBoxSignal(_ebv._eventBoxes[index],
            ReorderType.Any, index + moved));
    }
}

public class DragSwapSegmentCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public SegmentedControlCell currentCell;
    public SegmentedControl segmentedControl;
    private RectTransform _rectTransform;
    private int _startIndex;
    private Vector2 _startPos;
    public Action<int, int> OnSwapAction;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _startPos = _rectTransform.anchoredPosition;
        _startIndex = transform.GetSiblingIndex();
        currentCell.interactable = false;
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

    public void OnEndDrag(PointerEventData eventData)
    {
        if (transform.GetSiblingIndex() != _startIndex)
            OnSwapAction?.Invoke(currentCell.cellNumber, transform.GetSiblingIndex() - _startIndex);
        else
            _rectTransform.anchoredPosition = _startPos;

        currentCell.interactable = true;
    }

    private static bool AnchoredPositionWithinEachOther(RectTransform current, RectTransform other)
    {
        return current.anchoredPosition.y > other.anchoredPosition.y - other.rect.height / 2f &&
               current.anchoredPosition.y < other.anchoredPosition.y + other.rect.height / 2f;
    }
}