using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using IPA.Utilities;
using Zenject;

namespace EditorEnhanced.Commands;

public class PasteEventBoxSeedCommand : IBeatmapEditorCommandWithHistory
{
    private readonly BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;
    private readonly EventBoxGroupsState _eventBoxGroupsState;
    private readonly PasteEventBoxSeedSignal _signal;
    private readonly SignalBus _signalBus;
    private int _eventBoxId;
    private BeatmapEditorObjectId _groupId;
    private (EventBoxEditorData box, List<BaseEditorData> events) _newItem;
    private (EventBoxEditorData box, List<BaseEditorData> events) _oldItem;

    public PasteEventBoxSeedCommand(SignalBus signalBus,
        PasteEventBoxSeedSignal signal,
        EventBoxGroupsState eventBoxGroupsState,
        BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel)
    {
        _signalBus = signalBus;
        _signal = signal;
        _eventBoxGroupsState = eventBoxGroupsState;
        _beatmapEventBoxGroupsDataModel = beatmapEventBoxGroupsDataModel;
    }

    public bool shouldAddToHistory { get; private set; }

    public void Execute()
    {
        var newBox = _signal.EventBoxEditorData;
        if (newBox == null) return;
        var newList = _beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(newBox.id).ToList();
        var prevBox = _signal.EventBoxEditorData;
        var prevList = _beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(prevBox.id).ToList();

        _newItem = (EventBoxGroupsClipboardHelper.CopyEventBoxEditorDataWithoutId(newBox),
            newList.Select(d => EventBoxGroupsClipboardHelper.CopyBaseEditorDataWithoutId(d)).ToList());
        _newItem.box.indexFilter.SetField("seed", _signal.Seed);
        _oldItem = (prevBox, prevList);
        _groupId = _eventBoxGroupsState.eventBoxGroupContext.id;
        _eventBoxId = _beatmapEventBoxGroupsDataModel.GetEventBoxIdxByEventBoxId(_newItem.box.id);
        shouldAddToHistory = true;
        Redo();
    }

    public void Undo()
    {
        _beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(_newItem.box.id, _newItem.events);
        _beatmapEventBoxGroupsDataModel.RemoveEventBox(_groupId, _newItem.box);

        _beatmapEventBoxGroupsDataModel.InsertEventBox(_groupId, _oldItem.box);
        if (_oldItem.events != null)
            _beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(_oldItem.box.id,
                _oldItem.events);

        _signalBus.Fire(new EventBoxesUpdatedSignal(_eventBoxId));
        _signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }

    public void Redo()
    {
        _beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(_oldItem.box.id,
            _oldItem.events);
        _beatmapEventBoxGroupsDataModel.RemoveEventBox(_groupId, _oldItem.box);

        _beatmapEventBoxGroupsDataModel.InsertEventBox(_groupId, _newItem.box);
        if (_newItem.events != null)
            _beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(_newItem.box.id, _newItem.events);

        _signalBus.Fire(new EventBoxesUpdatedSignal(_eventBoxId - 1));
        _signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }
}