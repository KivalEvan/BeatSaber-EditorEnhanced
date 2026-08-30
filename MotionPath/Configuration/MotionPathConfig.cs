namespace EditorEnhanced.MotionPath.Configuration;

/// <summary>Configures the experimental motion-path view.</summary>
public class MotionPathConfig
{
   internal const float MaximumRangeInBeats = 8f;
   internal const float MinimumRangeInBeats = 0f;
   internal const float MinimumLineWidth = 0.001f;
   internal const float MaximumLineWidth = 1f;
   internal const float MinimumMarkerSize = 0.01f;
   internal const float MaximumMarkerSize = 2f;
   internal const float MinimumBeatLabelOffset = 0f;
   internal const float MaximumBeatLabelOffset = 2f;
   internal const float MinimumBakeBufferInBeats = 0.25f;
   internal const float MaximumBakeBufferInBeats = 16f;
   internal const int MinimumSamplesPerBeat = 8;
   internal const int MaximumSamplesPerBeat = 256;
   internal const int MinimumSubBeatDivisions = 1;
   internal const int MaximumSubBeatDivisions = 16;
   internal const int MinimumSelectedTargets = 1;
   internal const int MaximumSelectedTargetCount = 512;
   internal const int MinimumSelectedTracks = 1;
   internal const int MaximumSelectedTrackCount = 1024;
    internal const int MinimumVisibleEventMarkers = 0;
   internal const int MaximumVisibleEventMarkerCount = 256;

   /// <summary>Gets or sets whether the motion-path view is visible.</summary>
   public virtual bool Enabled { get; set; } = true;

   /// <summary>Gets or sets the number of beats shown before the current beat.</summary>
   public virtual float BackwardRangeInBeats { get; set; } = 0.5f;

   /// <summary>Gets or sets the number of beats shown after the current beat.</summary>
   public virtual float ForwardRangeInBeats { get; set; } = 1.5f;

    /// <summary>Gets or sets whether motion-path lines are rendered.</summary>
   public virtual bool ShowPathLines { get; set; } = true;

   /// <summary>Gets or sets whether the marker at the current beat is rendered.</summary>
   public virtual bool ShowCurrentBeatMarker { get; set; } = true;

   /// <summary>Gets or sets whether whole-beat timeline markers are rendered.</summary>
   public virtual bool ShowBeatMarkers { get; set; } = true;

   /// <summary>Gets or sets whether sub-beat timeline markers are rendered.</summary>
   public virtual bool ShowSubBeatMarkers { get; set; } = true;

   /// <summary>Gets or sets whether event timeline markers are rendered.</summary>
   public virtual bool ShowEventMarkers { get; set; } = true;

   /// <summary>Gets or sets whether beat labels are rendered.</summary>
   public virtual bool ShowBeatLabels { get; set; } = true;

   /// <summary>Gets or sets the width of the focused path line.</summary>
   public virtual float FocusedLineWidth { get; set; } = 0.2f;

   /// <summary>Gets or sets the width of an unfocused path line.</summary>
   public virtual float UnfocusedLineWidth { get; set; } = 0.1f;

   /// <summary>Gets or sets the size of the current-beat marker.</summary>
   public virtual float CurrentBeatMarkerSize { get; set; } = 0.30f;

   /// <summary>Gets or sets the size of a whole-beat marker.</summary>
   public virtual float BeatMarkerSize { get; set; } = 0.4f;

   /// <summary>Gets or sets the size of a sub-beat marker.</summary>
   public virtual float SubBeatMarkerSize { get; set; } = 0.5f;

   /// <summary>Gets or sets the size of an event marker.</summary>
   public virtual float EventMarkerSize { get; set; } = 0.75f;

   /// <summary>Gets or sets the size of a beat label.</summary>
   public virtual float BeatLabelSize { get; set; } = 0.14f;

   /// <summary>Gets or sets the vertical offset of a beat label.</summary>
   public virtual float BeatLabelOffset { get; set; } = 1f;

   /// <summary>Gets or sets the number of samples baked for each beat.</summary>
   public virtual int SamplesPerBeat { get; set; } = 64;

   /// <summary>Gets or sets the number of subdivisions between whole-beat markers.</summary>
   public virtual int SubBeatDivisions { get; set; } = 4;

   /// <summary>Gets or sets the extra baked range on each side of the visible range, in beats.</summary>
   public virtual float BakeBufferInBeats { get; set; } = 4f;

   /// <summary>Gets or sets the maximum number of selected target transforms to bake.</summary>
   public virtual int MaximumSelectedTargets { get; set; } = 64;

   /// <summary>Gets or sets the maximum number of selected target and ancestor paths to bake.</summary>
   public virtual int MaximumSelectedTracks { get; set; } = 96;

    /// <summary>Gets or sets the maximum number of event markers shown on the timeline. Zero hides event markers.</summary>
   public virtual int MaximumVisibleEventMarkers { get; set; } = 256;

   internal float GetBackwardRangeInBeats() =>
      Clamp(BackwardRangeInBeats, MinimumRangeInBeats, MaximumRangeInBeats, 0.5f);
   internal float GetForwardRangeInBeats() =>
      Clamp(ForwardRangeInBeats, MinimumRangeInBeats, MaximumRangeInBeats, 1.5f);
   internal bool GetShowPathLines() => ShowPathLines;
   internal bool GetShowCurrentBeatMarker() => ShowCurrentBeatMarker;
   internal bool GetShowBeatMarkers() => ShowBeatMarkers;
   internal bool GetShowSubBeatMarkers() => ShowSubBeatMarkers;
   internal bool GetShowEventMarkers() => ShowEventMarkers;
   internal bool GetShowBeatLabels() => ShowBeatLabels;
   internal float GetFocusedLineWidth() => ClampFocusedLineWidth(FocusedLineWidth);
   internal float GetUnfocusedLineWidth() => ClampUnfocusedLineWidth(UnfocusedLineWidth);
   internal float GetCurrentBeatMarkerSize() => ClampMarkerSize(CurrentBeatMarkerSize, 0.30f);
   internal float GetBeatMarkerSize() => ClampMarkerSize(BeatMarkerSize, 0.4f);
   internal float GetSubBeatMarkerSize() => ClampMarkerSize(SubBeatMarkerSize, 0.5f);
   internal float GetEventMarkerSize() => ClampMarkerSize(EventMarkerSize, 0.75f);
   internal float GetBeatLabelSize() => ClampMarkerSize(BeatLabelSize, 0.14f);
   internal float GetBeatLabelOffset() => ClampBeatLabelOffset(BeatLabelOffset);
   internal int GetSamplesPerBeat() => ClampSamplesPerBeat(SamplesPerBeat);
   internal int GetSubBeatDivisions() => ClampSubBeatDivisions(SubBeatDivisions);
   internal float GetBakeBufferInBeats() => ClampBakeBufferInBeats(BakeBufferInBeats);
   internal int GetMaximumSelectedTargets() => ClampMaximumSelectedTargets(MaximumSelectedTargets);
   internal int GetMaximumSelectedTracks() =>
      System.Math.Max(GetMaximumSelectedTargets(), ClampMaximumSelectedTracks(MaximumSelectedTracks));
    internal int GetMaximumVisibleEventMarkers() => ClampMaximumVisibleEventMarkers(MaximumVisibleEventMarkers);

   internal static float ClampRangeInBeats(float value) => Clamp(value, MinimumRangeInBeats, MaximumRangeInBeats, 1f);
   internal static float ClampFocusedLineWidth(float value) => Clamp(value, MinimumLineWidth, MaximumLineWidth, 0.2f);
   internal static float ClampUnfocusedLineWidth(float value) => Clamp(value, MinimumLineWidth, MaximumLineWidth, 0.1f);
   internal static float ClampMarkerSize(float value, float fallback) => Clamp(value, MinimumMarkerSize, MaximumMarkerSize, fallback);
   internal static float ClampBeatLabelOffset(float value) =>
      Clamp(value, MinimumBeatLabelOffset, MaximumBeatLabelOffset, 1f);
   internal static int ClampSamplesPerBeat(int value) =>
      UnityEngine.Mathf.Clamp(value, MinimumSamplesPerBeat, MaximumSamplesPerBeat);
   internal static int ClampSubBeatDivisions(int value) =>
      UnityEngine.Mathf.Clamp(value, MinimumSubBeatDivisions, MaximumSubBeatDivisions);
   internal static float ClampBakeBufferInBeats(float value) =>
      Clamp(value, MinimumBakeBufferInBeats, MaximumBakeBufferInBeats, 4f);
   internal static int ClampMaximumSelectedTargets(int value) =>
      UnityEngine.Mathf.Clamp(value, MinimumSelectedTargets, MaximumSelectedTargetCount);
   internal static int ClampMaximumSelectedTracks(int value) =>
      UnityEngine.Mathf.Clamp(value, MinimumSelectedTracks, MaximumSelectedTrackCount);
    internal static int ClampMaximumVisibleEventMarkers(int value) =>
      UnityEngine.Mathf.Clamp(value, MinimumVisibleEventMarkers, MaximumVisibleEventMarkerCount);

   internal int NormalizeMaximumSelectedTracks(float value) =>
      System.Math.Max(GetMaximumSelectedTargets(), ClampMaximumSelectedTracks(UnityEngine.Mathf.RoundToInt(value)));

   private static float Clamp(float value, float minValue, float maxValue, float fallback)
   {
      return float.IsNaN(value) ? fallback : UnityEngine.Mathf.Clamp(value, minValue, maxValue);
   }
}
