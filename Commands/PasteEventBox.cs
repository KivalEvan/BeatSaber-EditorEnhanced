using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using EditorEnhanced.Managers;
using IPA.Utilities;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Commands;

public class PasteEventBoxSignal
{
    public readonly EventBoxEditorData EventBoxEditorData;
    public readonly bool CopyEvent;
    public readonly bool RandomSeed;
    public readonly bool Increment;

    public PasteEventBoxSignal(EventBoxEditorData eventBoxEditorData, bool copyEvent, bool randomSeed, bool increment)
    {
        EventBoxEditorData = eventBoxEditorData;
        CopyEvent = copyEvent;
        RandomSeed = randomSeed;
        Increment = increment;
    }
}

public class PasteEventBoxCommand : IBeatmapEditorCommandWithHistory
{
    private readonly BeatmapEventBoxGroupsDataModel _beatmapEventBoxGroupsDataModel;
    private readonly EventBoxClipboardManager _clipboardManager;
    private readonly EventBoxGroupsState _eventBoxGroupsState;
    private readonly PasteEventBoxSignal _signal;
    private readonly SignalBus _signalBus;
    private int _eventBoxId;
    private BeatmapEditorObjectId _groupId;
    private (EventBoxEditorData box, List<BaseEditorData> events) _newItem;
    private (EventBoxEditorData box, List<BaseEditorData> events) _oldItem;

    public PasteEventBoxCommand(SignalBus signalBus,
        PasteEventBoxSignal signal,
        EventBoxClipboardManager clipboardManager,
        EventBoxGroupsState eventBoxGroupsState,
        BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel)
    {
        _signalBus = signalBus;
        _signal = signal;
        _clipboardManager = clipboardManager;
        _eventBoxGroupsState = eventBoxGroupsState;
        _beatmapEventBoxGroupsDataModel = beatmapEventBoxGroupsDataModel;
    }

    public bool shouldAddToHistory { get; private set; }

    public void Execute()
    {
        var newItem = _clipboardManager.Paste(_eventBoxGroupsState.eventBoxGroupContext.type);
        if (newItem == null) return;
        var prevBox = _signal.EventBoxEditorData;
        var prevList = _beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(prevBox.id).ToList();

        var newEventBox = EventBoxGroupsClipboardHelper.CopyEventBoxEditorDataWithoutId(_newItem.box);

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

        _newItem = (newEventBox,
            _signal.CopyEvent
                ? newItem.Value.events.Select(d => EventBoxGroupsClipboardHelper.CopyBaseEditorDataWithoutId(d))
                    .ToList()
                : []);
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