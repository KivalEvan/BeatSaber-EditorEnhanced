using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using EditorEnhanced.Managers;
using Zenject;

namespace EditorEnhanced.Commands;

public class PasteEventBoxCommand(
    SignalBus signalBus,
    PasteEventBoxSignal signal,
    EventBoxClipboardManager clipboardManager,
    EventBoxGroupsState eventBoxGroupsState,
    BeatmapEventBoxGroupsDataModel beatmapEventBoxGroupsDataModel) : IBeatmapEditorCommandWithHistory
{
    private BeatmapEditorObjectId _groupId;
    private (EventBoxEditorData box, List<BaseEditorData> events) _oldItem;
    private (EventBoxEditorData box, List<BaseEditorData> events) _newItem;

    public bool shouldAddToHistory { get; private set; }

    public void Execute()
    {
        var newItem = clipboardManager.Paste(eventBoxGroupsState.eventBoxGroupContext.type);
        if (newItem == null)
        {
            return;
        }
        var prevBox = signal.EventBoxEditorData;
        var prevList = beatmapEventBoxGroupsDataModel.GetBaseEventsListByEventBoxId(prevBox.id).ToList();

        _newItem = (EventBoxGroupsClipboardHelper.CopyEventBoxEditorDataWithoutId(newItem.Value.box),
            newItem.Value.events.Select(d => EventBoxGroupsClipboardHelper.CopyBaseEditorDataWithoutId(d)).ToList());
        _oldItem = (prevBox, prevList);
        _groupId = eventBoxGroupsState.eventBoxGroupContext.id;
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

        signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }
}