using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Views;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo;

public class GizmoClickable : MonoBehaviour, IGizmoInput
{
    public EventBoxEditorData EventBoxEditorDataContext;

    [Inject] private readonly BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;
    [Inject] private readonly EditBeatmapViewController _ebvc;
    private EventBoxesView _eventBoxesView;

    private void Awake()
    {
        _eventBoxesView = _ebvc._editBeatmapRightPanelView._panels[2].elements[0].GetComponent<EventBoxesView>();
    }

    public void OnPointerEnter()
    {
    }

    public void OnPointerExit()
    {
    }

    public void OnDrag()
    {
    }

    public void OnMouseClick()
    {
        _eventBoxesView.DisplayEventBoxes(
            _beatmapEventBoxGroupsDataModel.GetEventBoxIdxByEventBoxId(EventBoxEditorDataContext.id));
    }

    public void OnMouseRelease()
    {
    }
}