using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using Zenject;

namespace EditorEnhanced.Commands;

public class DuplicateEventBoxCommand(
    DuplicateEventBoxSignal signal,
    SignalBus signalBus,
    EventBoxGroupsState eventBoxGroupsState,
    BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel) : IBeatmapEditorCommandWithHistory
{
    private BeatmapEditorObjectId _eventBoxGroupId;
    private List<(EventBoxEditorData eventBox, List<BaseEditorData> baseList)> _newEventBoxes;
    private int _newIdx;
    private List<(EventBoxEditorData eventBox, List<BaseEditorData> baseList)> _previousEventBoxes;

    public bool shouldAddToHistory { get; private set; }

    public void Execute()
    {
        var selectedEventBox = signal.EventBoxEditorData;

        var eventBoxGroupId = eventBoxGroupsState.eventBoxGroupContext.id;
        var byEventBoxGroupId = beatmapEventBoxGroupsDataModel.GetEventBoxesByEventBoxGroupId(eventBoxGroupId);
        if (byEventBoxGroupId.Count == 0)
            return;
        var previousEventBoxes = new List<(EventBoxEditorData, List<BaseEditorData>)>(byEventBoxGroupId.Count);
        var newEventBoxes = new List<(EventBoxEditorData, List<BaseEditorData>)>();

        var newIdx = 0;
        for (var idx = 0; idx < byEventBoxGroupId.Count; idx++)
        {
            var eventBoxEditorData = byEventBoxGroupId[idx];
            var list = beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(eventBoxEditorData.id).ToList();
            previousEventBoxes.Add((eventBoxEditorData, list));
            newEventBoxes.Add((eventBoxEditorData, list));
            if (eventBoxEditorData.id != selectedEventBox.id) continue;
            newIdx = idx + 1;
            newEventBoxes.Add((EventBoxGroupsClipboardHelper.CopyEventBoxEditorDataWithoutId(eventBoxEditorData),
                list
                    .Select(d => EventBoxGroupsClipboardHelper.CopyBaseEditorDataWithoutId(d))
                    .ToList()));
            break;
        }

        _eventBoxGroupId = eventBoxGroupId;
        _newIdx = newIdx;
        _previousEventBoxes = previousEventBoxes;
        _newEventBoxes = newEventBoxes;
        shouldAddToHistory = true;
        Redo();
    }

    public void Undo()
    {
        foreach (var newEventBox in _newEventBoxes)
        {
            beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(newEventBox.eventBox.id, newEventBox.baseList);
            beatmapEventBoxGroupsDataModel.RemoveEventBox(_eventBoxGroupId, newEventBox.eventBox);
        }

        foreach (var previousEventBox in _previousEventBoxes)
        {
            beatmapEventBoxGroupsDataModel.InsertEventBox(_eventBoxGroupId, previousEventBox.eventBox);
            if (previousEventBox.baseList != null)
                beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(previousEventBox.eventBox.id,
                    previousEventBox.baseList);
        }

        signalBus.Fire(new EventBoxesUpdatedSignal(_newIdx));
        signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }

    public void Redo()
    {
        foreach (var previousEventBox in _previousEventBoxes)
        {
            beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(previousEventBox.eventBox.id,
                previousEventBox.baseList);
            beatmapEventBoxGroupsDataModel.RemoveEventBox(_eventBoxGroupId, previousEventBox.eventBox);
        }

        foreach (var newEventBox in _newEventBoxes)
        {
            beatmapEventBoxGroupsDataModel.InsertEventBox(_eventBoxGroupId, newEventBox.eventBox);
            if (newEventBox.baseList != null)
                beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(newEventBox.eventBox.id, newEventBox.baseList);
        }

        signalBus.Fire(new EventBoxesUpdatedSignal(_newIdx));
        signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }
}