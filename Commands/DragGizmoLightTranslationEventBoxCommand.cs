using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Commands;

public class DragGizmoLightTranslationEventBoxCommand : IBeatmapEditorCommand, IBeatmapEditorCommandWithHistory
{
    private DragGizmoLightTranslationEventBoxSignal _signal;
    private SignalBus _signalBus;
    private BeatmapState _beatmapState;
    private EventBoxGroupsState _eventBoxGroupsState;
    private BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;

    public bool shouldAddToHistory { get; private set; }
    private BeatmapEditorObjectId _eventBoxId;
    private BaseEditorData _event;
    private BaseEditorData _conflict;

    public DragGizmoLightTranslationEventBoxCommand(DragGizmoLightTranslationEventBoxSignal signal,
        SignalBus signalBus, BeatmapState beatmapState, EventBoxGroupsState eventBoxGroupsState,
        BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel)
    {
        _signal = signal;
        _signalBus = signalBus;
        _beatmapState = beatmapState;
        _eventBoxGroupsState = eventBoxGroupsState;
        _beatmapEventBoxGroupsDataModel = beatmapEventBoxGroupsDataModel;
    }

    public void Execute()
    {
        _eventBoxId = _signal.EventBoxEditorData.id;
        var beat = _beatmapState.beat - _eventBoxGroupsState.eventBoxGroupContext.beat;
        _conflict = _beatmapEventBoxGroupsDataModel.GetBaseEditorDataAt(_eventBoxId, beat);
        _event = LightTranslationBaseEditorData.CreateNew(beat, _eventBoxGroupsState.lightTranslationEaseLeadType,
            _eventBoxGroupsState.lightTranslationEaseCurveType, Mathf.Round(_signal.Value * 1_000f) / 1_000f,
            false);
        shouldAddToHistory = true;
        Redo();
    }

    public void Redo()
    {
        if (_conflict != null) _beatmapEventBoxGroupsDataModel.RemoveBaseEditorData(_eventBoxId, _conflict);
        _beatmapEventBoxGroupsDataModel.InsertBaseEditorData(_eventBoxId, _event);
        _signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }

    public void Undo()
    {
        _beatmapEventBoxGroupsDataModel.RemoveBaseEditorData(_eventBoxId, _event);
        if (_conflict != null) _beatmapEventBoxGroupsDataModel.InsertBaseEditorData(_eventBoxId, _conflict);
        _signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }
}