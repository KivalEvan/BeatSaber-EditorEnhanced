using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Configuration;
using EditorEnhanced.EventBoxes;
using Newtonsoft.Json;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.Managers;

public sealed class EventBoxGroupPresetManager
{
   private readonly BeatmapFlowCoordinator _flowCoordinator;
   private readonly EventBoxGroupMutation _mutation;
   private readonly EventBoxGroupsState _state;
   private List<EventBoxPresetData> _clipboard;

   public EventBoxGroupPresetManager(
      BeatmapFlowCoordinator flowCoordinator,
      EventBoxGroupsState state,
      EventBoxGroupMutation mutation)
   {
      _flowCoordinator = flowCoordinator;
      _state = state;
      _mutation = mutation;
   }

   public bool CopyCurrent()
   {
      var context = _state.eventBoxGroupContext;
      if (context == null) return false;

      _clipboard = Capture(context.id);
      return true;
   }

   public IReadOnlyList<EventBoxEditorData> GetClipboard()
   {
      var context = _state.eventBoxGroupContext;
      if (context == null || _clipboard == null) return null;
      return _clipboard.Select(item => item.ToEditorData(context.type)).ToList();
   }

   public bool SaveCurrent(string name)
   {
      var context = _state.eventBoxGroupContext;
      name = name?.Trim();
      if (context == null || string.IsNullOrEmpty(name)) return false;

      var records = LoadRecords();
      var environment = CurrentEnvironment;
      records.RemoveAll(item => IsCurrentKey(item, environment, context.groupId, context.type)
                                 && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
      records.Add(new EventBoxPresetRecord
      {
         Environment = environment,
         GroupId = context.groupId,
         GroupType = context.type,
         Name = name,
         EventBoxes = Capture(context.id)
      });
      Plugin.Config.EventBoxPresetsJson = JsonConvert.SerializeObject(records);
      return true;
   }

   public IReadOnlyList<string> GetPresetNames()
   {
      var context = _state.eventBoxGroupContext;
      if (context == null) return [];

      var environment = CurrentEnvironment;
      return LoadRecords()
         .Where(item => IsCurrentKey(item, environment, context.groupId, context.type))
         .Select(item => item.Name)
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   public IReadOnlyList<EventBoxEditorData> LoadPreset(string name)
   {
      var context = _state.eventBoxGroupContext;
      if (context == null) return null;

      var environment = CurrentEnvironment;
      var record = LoadRecords().LastOrDefault(item =>
         IsCurrentKey(item, environment, context.groupId, context.type)
         && string.Equals(item.Name, name, StringComparison.Ordinal));
      return record?.EventBoxes?.Select(item => item.ToEditorData(context.type)).ToList();
   }

   private string CurrentEnvironment => _flowCoordinator._currentEnvironment?.serializedName ?? "Unknown";

   private List<EventBoxPresetData> Capture(BeatmapEditorObjectId groupId)
   {
      return _mutation.Capture(groupId).EventBoxes
         .Select(item => EventBoxPresetData.FromEditorData(item.EventBox))
         .ToList();
   }

   private static bool IsCurrentKey(
      EventBoxPresetRecord item,
      string environment,
      int groupId,
      EventBoxGroupType groupType)
   {
      return item != null
             && item.GroupId == groupId
             && item.GroupType == groupType
             && string.Equals(item.Environment, environment, StringComparison.Ordinal);
   }

   private static List<EventBoxPresetRecord> LoadRecords()
   {
      try
      {
         return JsonConvert.DeserializeObject<List<EventBoxPresetRecord>>(Plugin.Config.EventBoxPresetsJson ?? "[]")
                ?? new List<EventBoxPresetRecord>();
      }
      catch (JsonException exception)
      {
         Plugin.Log.Error($"Could not read event box presets: {exception.Message}");
         return new List<EventBoxPresetRecord>();
      }
   }
}
