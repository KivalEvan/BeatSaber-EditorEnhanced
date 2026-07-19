using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.EventBoxes;
using EditorEnhanced.Managers;

namespace EditorEnhanced.Commands;

public class PasteEventBoxSignal
{
   public readonly bool CopyEvent;
   public readonly EventBoxEditorData EventBoxEditorData;
   public readonly bool Increment;
   public readonly bool RandomSeed;
   public readonly float Value;

   public PasteEventBoxSignal(
      EventBoxEditorData eventBoxEditorData,
      bool copyEvent,
      bool randomSeed,
      bool increment,
      float value)
   {
      EventBoxEditorData = eventBoxEditorData;
      CopyEvent = copyEvent;
      RandomSeed = randomSeed;
      Increment = increment;
      Value = value;
   }
}

public sealed class PasteEventBoxCommand : IBeatmapEditorCommandWithHistory
{
   private readonly EventBoxClipboardManager _clipboardManager;
   private readonly EventBoxCloneService _cloneService;
   private readonly BeatmapEventBoxGroupsDataModel _dataModel;
   private readonly EventBoxGroupsState _eventBoxGroupsState;
   private readonly EventBoxGroupMutation _mutation;
   private readonly PasteEventBoxSignal _signal;
   private int _newIdx;
   private EventBoxGroupSnapshot _newSnapshot;
   private EventBoxGroupSnapshot _previousSnapshot;

   public PasteEventBoxCommand(
      PasteEventBoxSignal signal,
      BeatmapEventBoxGroupsDataModel dataModel,
      EventBoxClipboardManager clipboardManager,
      EventBoxGroupsState eventBoxGroupsState,
      EventBoxGroupMutation mutation,
      EventBoxCloneService cloneService)
   {
      _signal = signal;
      _dataModel = dataModel;
      _clipboardManager = clipboardManager;
      _eventBoxGroupsState = eventBoxGroupsState;
      _mutation = mutation;
      _cloneService = cloneService;
   }

   public bool shouldAddToHistory { get; private set; }

   public void Execute()
   {
      var selectedEventBox = _signal.EventBoxEditorData;
      if (selectedEventBox == null) return;

      var context = _eventBoxGroupsState.eventBoxGroupContext;
      if (context == null) return;

      var previousSnapshot = _mutation.Capture(context.id);
      var selectedIndex = previousSnapshot.IndexOf(selectedEventBox.id);
      if (selectedIndex < 0) return;

      var clipboardItem = _clipboardManager.Get(context.type);
      if (clipboardItem == null) return;
      var maxId = int.MaxValue;
      if (_signal.Increment)
      {
         if (!_dataModel.TryGetGroupSizeByEventBoxGroupId(context.groupId, out var groupSize) || groupSize <= 0)
            return;
         maxId = groupSize - 1;
      }

      var replacement = _cloneService.Clone(
         clipboardItem,
         _signal.CopyEvent,
         _signal.Increment,
         _signal.RandomSeed,
         _signal.Value,
         maxId);
      var eventBoxes = previousSnapshot.EventBoxes.ToList();
      eventBoxes[selectedIndex] = replacement;

      _newIdx = selectedIndex;
      _previousSnapshot = previousSnapshot;
      _newSnapshot = previousSnapshot.WithEventBoxes(eventBoxes);
      shouldAddToHistory = true;
      Redo();
   }

   public void Undo()
   {
      _mutation.Replace(_newSnapshot, _previousSnapshot, _newIdx);
   }

   public void Redo()
   {
      _mutation.Replace(_previousSnapshot, _newSnapshot, _newIdx);
   }
}
