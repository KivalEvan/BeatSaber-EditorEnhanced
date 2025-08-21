using System;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public class GizmoSwappable : MonoBehaviour, IGizmoInput
{
   [Inject] private readonly BeatmapEventBoxGroupsDataModel _bebgdm = null!;
   [Inject] private readonly PluginConfig _config = null!;
   [Inject] private readonly EventBoxGroupsState _ebgs = null!;
   [Inject] private readonly EditBeatmapViewController _ebvc = null!;
   [Inject] private readonly SignalBus _signalBus = null!;
   private Camera _camera;
   private EventBoxesView _eventBoxesView;
   private int _index;
   private Vector3 _initialPosition;
   private int _maxIndex;
   private int _startIndex;
   public EventBoxEditorData EventBoxEditorDataContext;

   private void Awake()
   {
      _camera = Camera.main;
      _eventBoxesView = _ebvc._editBeatmapRightPanelView._panels[2].elements[0].GetComponent<EventBoxesView>();
   }

   private void OnEnable()
   {
      _index = _bebgdm.GetEventBoxIdxByEventBoxId(EventBoxEditorDataContext.id);
      _maxIndex = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id).Count;
      transform.position = new Vector3((_index - (_maxIndex - 1) / 2f) / 2f, -0.1f, 0f);
   }

   public bool IsDragging { get; set; }

   public void OnPointerEnter()
   {
   }

   public void OnPointerExit()
   {
   }

   public void OnDrag()
   {
      if (!_config.Gizmo.Swappable && !IsDragging) return;
      var screenPosition = GetScreenPosition();
      var worldPosition = _camera.ScreenToWorldPoint(screenPosition);
      if (Math.Abs(_initialPosition.x - worldPosition.x) < 0.5f)
      {
         transform.position = _initialPosition;
         return;
      }

      transform.position = new Vector3(
         SnapPosition(worldPosition.x),
         0.05f,
         0f
      );
   }

   public void OnMouseClick()
   {
      if (!_config.Gizmo.Swappable) return;
      _startIndex = _index;
      _eventBoxesView.DisplayEventBoxes(_bebgdm.GetEventBoxIdxByEventBoxId(EventBoxEditorDataContext.id));
      _initialPosition = transform.position;
      IsDragging = true;
   }

   public void OnMouseRelease()
   {
      if (!_config.Gizmo.Swappable && !IsDragging) return;
      if (_startIndex > _index)
         _signalBus.Fire(new ReorderEventBoxSignal(_startIndex, _index));
      else if (_startIndex < _index - 1)
         _signalBus.Fire(new ReorderEventBoxSignal(_startIndex, _index - 1));
      else
      {
         _index = _startIndex;
         transform.position = _initialPosition;
      }

      IsDragging = false;
   }

   private int GetIndex(float v)
   {
      var t = Mathf.InverseLerp(-_maxIndex / 4f, _maxIndex / 4f, v + .25f);
      return (int)Mathf.Lerp(0, _maxIndex, t);
   }

   private float SnapPosition(float v)
   {
      _index = GetIndex(v);
      return (_index - _maxIndex / 2f) / 2f;
   }

   private Vector3 GetScreenPosition()
   {
      return new Vector3(
         Mouse.current.position.x.value,
         Mouse.current.position.y.value,
         _camera.WorldToScreenPoint(transform.position).z);
   }
}