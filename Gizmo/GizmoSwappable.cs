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

    private Camera Camera;

    [Inject] private readonly BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;
    [Inject] private readonly EditBeatmapViewController _ebvc;
    [Inject] private readonly BeatmapEventBoxGroupsDataModel _bebgdm;
    [Inject] private readonly EventBoxGroupsState _ebgs;
    [Inject] private readonly SignalBus _signalBus;

    private EventBoxesView _eventBoxesView;
    private int _index;
    private int _startIndex;
    private int _maxIndex;
    private Vector3 _initialPosition;

    private void Awake()
    {
        Camera = Camera.main;
        _eventBoxesView = _ebvc._editBeatmapRightPanelView._panels[2].elements[0].GetComponent<EventBoxesView>();
    }

    public void OnPointerEnter()
    {
    }

    public void OnPointerExit()
    {
    }

    private int GetIndex(float v)
    {
        var t = Mathf.InverseLerp(-_maxIndex / 4f, _maxIndex / 4f, v);
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
            Camera.WorldToScreenPoint(transform.position).z);
    }

    public void OnDrag()
    {
        var screenPosition = GetScreenPosition();
        var worldPosition = Camera.ScreenToWorldPoint(screenPosition);
        transform.position = worldPosition;

        transform.localPosition = new Vector3(
            SnapPosition(transform.localPosition.x),
            0f,
            0f
        );
    }

    public void OnMouseClick()
    {
        _startIndex = _bebgdm.GetEventBoxIdxByEventBoxId(EventBoxEditorDataContext.id);
        _maxIndex = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id).Count;
        _eventBoxesView.DisplayEventBoxes(
            _beatmapEventBoxGroupsDataModel.GetEventBoxIdxByEventBoxId(EventBoxEditorDataContext.id));
        _initialPosition = transform.position;
    }

    public void OnMouseRelease()
    {
        if (_startIndex > _index)
            _signalBus.Fire(new ReorderEventBoxSignal(EventBoxEditorDataContext, ReorderType.Any, _index));
        else if (_startIndex < _index - 1)
            _signalBus.Fire(new ReorderEventBoxSignal(EventBoxEditorDataContext, ReorderType.Any, _index - 1));
        else
            transform.position = _initialPosition;
    }
}