using BeatmapEditor3D.Commands;
using EditorEnhanced.Commands;
using HMUI;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace EditorEnhanced.UI.Components;

public class DragSwapSegmentCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
   [Inject] private readonly SignalBus _signalBus;
   private SegmentedControlCell _currentCell;
   private RectTransform _rectTransform;
   private SegmentedControl _segmentedControl;
   private Vector2 _startPos;
   private int _newIndex;

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
         _newIndex = segmentedControlCell.cellNumber;
      }
   }

   public void OnEndDrag(PointerEventData eventData)
   {
      if (_newIndex != _currentCell.cellNumber)
         _signalBus.Fire(new ReorderEventBoxSignal(_currentCell.cellNumber, _newIndex));
      else
         _rectTransform.anchoredPosition = _startPos;

      _currentCell.interactable = true;
   }

   private static bool AnchoredPositionWithinEachOther(RectTransform current, RectTransform other)
   {
      return current.anchoredPosition.y > other.anchoredPosition.y - other.rect.height / 2f
         && current.anchoredPosition.y < other.anchoredPosition.y + other.rect.height / 2f;
   }
}