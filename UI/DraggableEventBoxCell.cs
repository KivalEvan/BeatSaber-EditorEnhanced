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
using UnityEngine.Serialization;
using Zenject;

namespace EditorEnhanced.UI;

public class DraggableEventBoxCell : IInitializable
{
    private readonly DiContainer _container;
    private readonly EditBeatmapViewController _ebvc;

    public DraggableEventBoxCell(DiContainer container, SignalBus signalBus, EditBeatmapViewController ebvc)
    {
        _container = container;
        _ebvc = ebvc;
    }

    public void Initialize()
    {
        var ebv = _ebvc._editBeatmapRightPanelView._panels.First(p => p.panelType == BeatmapPanelType.EventBox).elements[0]
            .GetComponent<EventBoxesView>();

        SegmentedControlCell[] prefabs =
        [
            ebv._eventBoxButtonsTextSegmentedControl._firstCellPrefab,
            ebv._eventBoxButtonsTextSegmentedControl._middleCellPrefab,
            ebv._eventBoxButtonsTextSegmentedControl._lastCellPrefab,
            ebv._eventBoxButtonsTextSegmentedControl._singleCellPrefab
        ];

        foreach (var prefab in prefabs)
        {
            _container.InstantiateComponent<DragSwapSegmentCell>(prefab.gameObject);
        }
    }
}

public class DragSwapSegmentCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Inject] private readonly SignalBus _signalBus;
    
    private SegmentedControlCell _currentCell;
    private SegmentedControl _segmentedControl;
    private RectTransform _rectTransform;
    private Vector2 _startPos;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _currentCell = GetComponent<SegmentedControlCell>();
    }
    
    private void Start()
    {
        _segmentedControl = _currentCell._segmentedControl;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _startPos = _rectTransform.anchoredPosition;
        _currentCell.interactable = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = transform.position with { y = eventData.position.y };
        foreach (var segmentedControlCell in _segmentedControl.cells)
        {
            if (!segmentedControlCell.gameObject.activeSelf) continue;
            if (_currentCell == segmentedControlCell) continue;
            if (!AnchoredPositionWithinEachOther(_rectTransform, segmentedControlCell.GetComponent<RectTransform>()))
                continue;
            transform.SetSiblingIndex(segmentedControlCell.transform.GetSiblingIndex());
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var newIndex = transform.GetSiblingIndex() - 1;
        if (newIndex != _currentCell.cellNumber)
            _signalBus.Fire(new MoveEventBoxSignal(_currentCell.cellNumber, newIndex));
        else
            _rectTransform.anchoredPosition = _startPos;

        _currentCell.interactable = true;
    }

    private static bool AnchoredPositionWithinEachOther(RectTransform current, RectTransform other)
    {
        return current.anchoredPosition.y > other.anchoredPosition.y - other.rect.height / 2f &&
               current.anchoredPosition.y < other.anchoredPosition.y + other.rect.height / 2f;
    }
}