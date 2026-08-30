using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatSaber.TrackDefinitions.DataModels;
using Tweening;

namespace EditorEnhanced.Configuration;

public sealed class EventBoxPresetRecord
{
   public string Environment { get; set; }
   public int GroupId { get; set; }
   public EventBoxGroupType GroupType { get; set; }
   public string Name { get; set; }
   public List<EventBoxPresetData> EventBoxes { get; set; } = new();
}

public sealed class EventBoxPresetData
{
   public string Type { get; set; }
   public int FilterType { get; set; }
   public int Param0 { get; set; }
   public int Param1 { get; set; }
   public bool Reversed { get; set; }
   public int Chunks { get; set; }
   public int RandomType { get; set; }
   public int Seed { get; set; }
   public float Limit { get; set; }
   public int LimitAlsoAffectType { get; set; }
   public int BeatDistributionParamType { get; set; }
   public float BeatDistributionParam { get; set; }
   public int ValueDistributionParamType { get; set; }
   public float ValueDistributionParam { get; set; }
   public bool ValueDistributionShouldAffectFirstBaseEvent { get; set; }
   public int ValueDistributionEaseType { get; set; }
   public int Axis { get; set; }
   public bool Flip { get; set; }

   public static EventBoxPresetData FromEditorData(EventBoxEditorData eventBox)
   {
      if (eventBox == null) throw new ArgumentNullException(nameof(eventBox));

      var result = new EventBoxPresetData
      {
         FilterType = (int)eventBox.indexFilter.type,
         Param0 = eventBox.indexFilter.param0,
         Param1 = eventBox.indexFilter.param1,
         Reversed = eventBox.indexFilter.reversed,
         Chunks = eventBox.indexFilter.chunks,
         RandomType = (int)eventBox.indexFilter.randomType,
         Seed = eventBox.indexFilter.seed,
         Limit = eventBox.indexFilter.limit,
         LimitAlsoAffectType = (int)eventBox.indexFilter.limitAlsoAffectType,
         BeatDistributionParamType = (int)eventBox.beatDistributionParamType,
         BeatDistributionParam = eventBox.beatDistributionParam
      };

      switch (eventBox)
      {
         case LightColorEventBoxEditorData color:
            result.Type = "Color";
            result.ValueDistributionParamType = (int)color.brightnessDistributionParamType;
            result.ValueDistributionParam = color.brightnessDistributionParam;
            result.ValueDistributionShouldAffectFirstBaseEvent = color.brightnessDistributionShouldAffectFirstBaseEvent;
            result.ValueDistributionEaseType = (int)color.brightnessDistributionEaseType;
            break;
         case LightRotationEventBoxEditorData rotation:
            result.Type = "Rotation";
            result.ValueDistributionParamType = (int)rotation.rotationDistributionParamType;
            result.ValueDistributionParam = rotation.rotationDistributionParam;
            result.ValueDistributionShouldAffectFirstBaseEvent = rotation.rotationDistributionShouldAffectFirstBaseEvent;
            result.ValueDistributionEaseType = (int)rotation.rotationDistributionEaseType;
            result.Axis = (int)rotation.axis;
            result.Flip = rotation.flipRotation;
            break;
         case LightTranslationEventBoxEditorData translation:
            result.Type = "Translation";
            result.ValueDistributionParamType = (int)translation.gapDistributionParamType;
            result.ValueDistributionParam = translation.gapDistributionParam;
            result.ValueDistributionShouldAffectFirstBaseEvent = translation.gapDistributionShouldAffectFirstBaseEvent;
            result.ValueDistributionEaseType = (int)translation.gapDistributionEaseType;
            result.Axis = (int)translation.axis;
            result.Flip = translation.flipTranslation;
            break;
         case FxEventBoxEditorData fx:
            result.Type = "Fx";
            result.ValueDistributionParamType = (int)fx.vfxDistributionParamType;
            result.ValueDistributionParam = fx.vfxDistributionParam;
            result.ValueDistributionShouldAffectFirstBaseEvent = fx.vfxDistributionShouldAffectFirstBaseEvent;
            result.ValueDistributionEaseType = (int)fx.vfxDistributionEaseType;
            break;
         default:
            throw new NotSupportedException($"Unsupported event box type: {eventBox.GetType().FullName}");
      }

      return result;
   }

   public EventBoxEditorData ToEditorData(EventBoxGroupType targetType)
   {
      var filter = IndexFilterEditorData.CreateNew(
         (IndexFilterEditorData.IndexFilterType)FilterType,
         Param0,
         Param1,
         Reversed,
         Chunks,
         (IndexFilter.IndexFilterRandomType)RandomType,
         Seed,
         Limit,
         (IndexFilter.IndexFilterLimitAlsoAffectType)LimitAlsoAffectType);
      var beatType = (BeatmapEventDataBox.DistributionParamType)BeatDistributionParamType;
      var valueType = (BeatmapEventDataBox.DistributionParamType)ValueDistributionParamType;
      var easeType = (EaseType)ValueDistributionEaseType;

      return targetType switch
      {
         EventBoxGroupType.Color => LightColorEventBoxEditorData.CreateNew(
            filter, beatType, BeatDistributionParam, valueType, ValueDistributionParam,
            ValueDistributionShouldAffectFirstBaseEvent, easeType),
         EventBoxGroupType.Rotation => LightRotationEventBoxEditorData.CreateNew(
            filter, beatType, BeatDistributionParam, valueType, ValueDistributionParam,
            ValueDistributionShouldAffectFirstBaseEvent, easeType, CompatibleAxis, CompatibleFlip),
         EventBoxGroupType.Translation => LightTranslationEventBoxEditorData.CreateNew(
            filter, beatType, BeatDistributionParam, valueType, ValueDistributionParam,
            ValueDistributionShouldAffectFirstBaseEvent, easeType, CompatibleAxis, CompatibleFlip),
         EventBoxGroupType.FloatFx => FxEventBoxEditorData.CreateNew(
            filter, beatType, BeatDistributionParam, valueType, ValueDistributionParam,
            ValueDistributionShouldAffectFirstBaseEvent, easeType),
         _ => throw new NotSupportedException($"Unsupported event box group type: {targetType}")
      };
   }

   private LightAxis CompatibleAxis => Type is "Rotation" or "Translation" ? (LightAxis)Axis : default;
   private bool CompatibleFlip => Type is "Rotation" or "Translation" && Flip;
}
