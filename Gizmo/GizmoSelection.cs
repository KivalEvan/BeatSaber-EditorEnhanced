using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Gizmo;

public class GizmoSelection : MonoBehaviour
{
    [Inject] private readonly EditBeatmapViewController _ebvc;
    [Inject] private readonly BeatmapEventBoxGroupsDataModel _bebgdm;
    [Inject] private readonly EventBoxGroupsState _ebgs;
    [Inject] private readonly SignalBus _signalBus;

    private EventBoxesView _eventBoxesView;
    private int _index;
    private int _maxIndex;
    
    private void Awake()
    {
        _eventBoxesView = _ebvc._editBeatmapRightPanelView._panels[2].elements[0].GetComponent<EventBoxesView>();
    }

    private void OnEnable()
    {
        _maxIndex = _bebgdm.GetEventBoxesByEventBoxGroupId(_ebgs.eventBoxGroupContext.id).Count - 1;
        MoveTransform();
        _signalBus.Subscribe<EventBoxSelectedSignal>(MoveTransform);
    }

    private void OnDisable()
    {
        _signalBus.TryUnsubscribe<EventBoxSelectedSignal>(MoveTransform);
    }

    private void MoveTransform()
    {
        _index = _eventBoxesView._activeEventBoxIdx;
        transform.position = new Vector3(
            (_index - _maxIndex / 2f) / 2f,
            -0.1f,
            0f
        );
    }
}