using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Commands;

public class DuplicateEventBoxCommand : IBeatmapEditorCommandWithHistory
{
    private readonly BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;
    private readonly EventBoxGroupsState _eventBoxGroupsState;
    private readonly DuplicateEventBoxSignal _signal;
    private readonly SignalBus _signalBus;
    private BeatmapEditorObjectId _eventBoxGroupId;
    private List<(EventBoxEditorData eventBox, List<BaseEditorData> baseList)> _newEventBoxes;
    private int _newIdx;
    private List<(EventBoxEditorData eventBox, List<BaseEditorData> baseList)> _previousEventBoxes;

    public DuplicateEventBoxCommand(DuplicateEventBoxSignal signal,
        SignalBus signalBus,
        EventBoxGroupsState eventBoxGroupsState,
        BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel)
    {
        _signal = signal;
        _signalBus = signalBus;
        _eventBoxGroupsState = eventBoxGroupsState;
        _beatmapEventBoxGroupsDataModel = beatmapEventBoxGroupsDataModel;
    }

    public bool shouldAddToHistory { get; private set; }

    public void Execute()
    {
        var selectedEventBox = _signal.EventBoxEditorData;

        var eventBoxGroupId = _eventBoxGroupsState.eventBoxGroupContext.id;
        var byEventBoxGroupId = _beatmapEventBoxGroupsDataModel.GetEventBoxesByEventBoxGroupId(eventBoxGroupId);
        if (byEventBoxGroupId.Count == 0)
            return;
        var previousEventBoxes = new List<(EventBoxEditorData, List<BaseEditorData>)>(byEventBoxGroupId.Count);
        var newEventBoxes = new List<(EventBoxEditorData, List<BaseEditorData>)>();

        var newIdx = 0;
        for (var idx = 0; idx < byEventBoxGroupId.Count; idx++)
        {
            var eventBoxEditorData = byEventBoxGroupId[idx];
            var list = _beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(eventBoxEditorData.id).ToList();
            previousEventBoxes.Add((eventBoxEditorData, list));
            newEventBoxes.Add((eventBoxEditorData, list));

            if (eventBoxEditorData.id != selectedEventBox.id) continue;
            newIdx = idx + 1;
            var newEventBox = EventBoxGroupsClipboardHelper.CopyEventBoxEditorDataWithoutId(eventBoxEditorData);

            if (_signal.Increment)
            {
                if (newEventBox.indexFilter.type == IndexFilterEditorData.IndexFilterType.Division)
                {
                    newEventBox.indexFilter.SetField("param1", newEventBox.indexFilter.param1 + 1);
                }
                else
                {
                    newEventBox.indexFilter.SetField("param0", newEventBox.indexFilter.param0 + 1);
                }
            }

            if (_signal.RandomSeed &&
                newEventBox.indexFilter.randomType.HasFlag(IndexFilter.IndexFilterRandomType.RandomElements))
            {
                newEventBox.indexFilter.SetField("seed", Random.Range(int.MinValue, int.MaxValue));
            }

            newEventBoxes.Add((newEventBox,
                _signal.CopyEvent
                    ? list
                        .Select(d => EventBoxGroupsClipboardHelper.CopyBaseEditorDataWithoutId(d))
                        .ToList()
                    : []));
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
            _beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(newEventBox.eventBox.id, newEventBox.baseList);
            _beatmapEventBoxGroupsDataModel.RemoveEventBox(_eventBoxGroupId, newEventBox.eventBox);
        }

        foreach (var previousEventBox in _previousEventBoxes)
        {
            _beatmapEventBoxGroupsDataModel.InsertEventBox(_eventBoxGroupId, previousEventBox.eventBox);
            if (previousEventBox.baseList != null)
                _beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(previousEventBox.eventBox.id,
                    previousEventBox.baseList);
        }

        _signalBus.Fire(new EventBoxesUpdatedSignal(_newIdx));
        _signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }

    public void Redo()
    {
        foreach (var previousEventBox in _previousEventBoxes)
        {
            _beatmapEventBoxGroupsDataModel.RemoveBaseEditorDataList(previousEventBox.eventBox.id,
                previousEventBox.baseList);
            _beatmapEventBoxGroupsDataModel.RemoveEventBox(_eventBoxGroupId, previousEventBox.eventBox);
        }

        foreach (var newEventBox in _newEventBoxes)
        {
            _beatmapEventBoxGroupsDataModel.InsertEventBox(_eventBoxGroupId, newEventBox.eventBox);
            if (newEventBox.baseList != null)
                _beatmapEventBoxGroupsDataModel.InsertBaseEditorDataList(newEventBox.eventBox.id, newEventBox.baseList);
        }

        _signalBus.Fire(new EventBoxesUpdatedSignal(_newIdx));
        _signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }
}