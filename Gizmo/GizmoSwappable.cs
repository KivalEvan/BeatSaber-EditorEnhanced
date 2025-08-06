using System;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo;

public class GizmoSwappable : MonoBehaviour, IGizmoInput
{
    public EventBoxEditorData EventBoxEditorDataContext;

    [Inject] private readonly EditBeatmapViewController _ebvc;
    [Inject] private readonly BeatmapEventBoxGroupsDataModel _bebgdm;
    [Inject] private readonly EventBoxGroupsState _ebgs;
    [Inject] private readonly SignalBus _signalBus;

    private Camera _camera;

    private EventBoxesView _eventBoxesView;
    private int _index;
    private int _startIndex;
    private int _maxIndex;
    private Vector3 _initialPosition;
    
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

    public void OnPointerEnter()
    {
    }

    public void OnPointerExit()
    {
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
        return new Vector3(Mouse.current.position.x.value, Mouse.current.position.y.value,
            _camera.WorldToScreenPoint(transform.position).z);
    }

    public void OnDrag()
    {
        var screenPosition = GetScreenPosition();
        var worldPosition = _camera.ScreenToWorldPoint(screenPosition);
        if (Math.Abs(_initialPosition.x - worldPosition.x) < 0.5f)
        {
            transform.position = _initialPosition;
            return;
        };
        
        transform.position = new Vector3(
            SnapPosition(worldPosition.x),
            0.05f,
            0f
        );
    }

    public void OnMouseClick()
    {
        _startIndex = _index;
        _eventBoxesView.DisplayEventBoxes(_bebgdm.GetEventBoxIdxByEventBoxId(EventBoxEditorDataContext.id));
        _initialPosition = transform.position;
    }

    public void OnMouseRelease()
    {
        if (_startIndex > _index)
            _signalBus.Fire(new ReorderEventBoxSignal(EventBoxEditorDataContext, ReorderType.Any, _index));
        else if (_startIndex < _index - 1)
            _signalBus.Fire(new ReorderEventBoxSignal(EventBoxEditorDataContext, ReorderType.Any, _index - 1));
        else
        {
            _index = _startIndex;
            transform.position = _initialPosition;
        }
    }
}