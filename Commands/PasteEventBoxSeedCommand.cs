using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using EditorEnhanced.Managers;
using IPA.Utilities;
using Zenject;

namespace EditorEnhanced.Commands;

public class PasteEventBoxSeedCommand(
    SignalBus signalBus,
    PasteEventBoxSeedSignal signal,
    EventBoxGroupsState eventBoxGroupsState,
    BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel) : IBeatmapEditorCommandWithHistory
{
    private BeatmapEditorObjectId _groupId;
    private (EventBoxEditorData box, List<BaseEditorData> events) _newItem;
    private (EventBoxEditorData box, List<BaseEditorData> events) _oldItem;
    private int _eventBoxId;

    public bool shouldAddToHistory { get; private set; }

    public void Execute()
    {
        var newBox = signal.EventBoxEditorData;
        if (newBox == null) return;
        var newList = beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(newBox.id).ToList();
        var prevBox = signal.EventBoxEditorData;
        var prevList = beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(prevBox.id).ToList();

        _newItem = (EventBoxGroupsClipboardHelper.CopyEventBoxEditorDataWithoutId(newBox),
            newList.Select(d => EventBoxGroupsClipboardHelper.CopyBaseEditorDataWithoutId(d)).ToList());
        _newItem.box.indexFilter.SetField("seed", signal.Seed);
        _oldItem = (prevBox, prevList);
        _groupId = eventBoxGroupsState.eventBoxGroupContext.id;
        _eventBoxId = beatmapEventBoxGroupsDataModel.GetEventBoxIdxByEventBoxId(_newItem.box.id);
        shouldAddToHistory = true;
        Redo();
    }

    public void Undo()
    {
        beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(_newItem.box.id, _newItem.events);
        beatmapEventBoxGroupsDataModel.RemoveEventBox(_groupId, _newItem.box);

        beatmapEventBoxGroupsDataModel.InsertEventBox(_groupId, _oldItem.box);
        if (_oldItem.events != null)
            beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(_oldItem.box.id,
                _oldItem.events);

        signalBus.Fire(new EventBoxesUpdatedSignal(_eventBoxId));
        signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }

    public void Redo()
    {
        beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(_oldItem.box.id,
            _oldItem.events);
        beatmapEventBoxGroupsDataModel.RemoveEventBox(_groupId, _oldItem.box);

        beatmapEventBoxGroupsDataModel.InsertEventBox(_groupId, _newItem.box);
        if (_newItem.events != null)
            beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(_newItem.box.id, _newItem.events);

        signalBus.Fire(new EventBoxesUpdatedSignal(_eventBoxId - 1));
        signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }
}