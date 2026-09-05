using System;
using BeatmapEditor3D;
using EditorEnhanced.MotionPath;
using EditorEnhanced.MotionPath.Configuration;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

public partial class ConfigurationView
{
   private NumericControl _motionPathBeatLabelOffsetControl;
   private NumericControl _motionPathBeatLabelSizeControl;
   private NumericControl _motionPathBeatMarkerSizeControl;
   private NumericControl _motionPathCurrentBeatMarkerSizeControl;
   private NumericControl _motionPathEventMarkerSizeControl;
   private NumericControl _motionPathFocusedLineWidthControl;
   private NumericControl _motionPathMaximumSelectedTargetsControl;
   private NumericControl _motionPathMaximumSelectedTracksControl;
    private NumericControl _motionPathMaximumVisibleEventMarkersControl;
   private NumericControl _motionPathBackwardRangeControl;
   private NumericControl _motionPathForwardRangeControl;
   private NumericControl _motionPathSamplesPerBeatControl;
   private NumericControl _motionPathSubBeatDivisionsControl;
   private NumericControl _motionPathSubBeatMarkerSizeControl;
   private NumericControl _motionPathUnfocusedLineWidthControl;

   private void BuildMotionPathSection(Transform parent, Transform noteBackground)
   {
      var stackTag = _uiBuilder
         .CreateStackLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.MiddleCenter)
         .SetPadding(new RectOffset(4, 4, 4, 4));
      var verticalTag = _uiBuilder
         .CreateVerticalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.UpperLeft)
         .SetPadding(new RectOffset(4, 4, 4, 4));
      var horizontalTag = _uiBuilder
         .CreateHorizontalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildControlWidth(false)
         .SetChildForceExpandWidth(false)
         .SetChildAlignment(TextAnchor.MiddleLeft)
         .SetSpacing(8)
         .SetPadding(new RectOffset(4, 4, 4, 4));
      var visibilityTag = _uiBuilder
         .CreateVerticalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.UpperLeft)
         .SetSpacing(4)
         .SetPadding(new RectOffset(0, 0, 0, 0));
      var numericRowTag = _uiBuilder
         .CreateVerticalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.UpperLeft)
         .SetChildControlWidth(false)
         .SetChildForceExpandWidth(false)
         .SetSpacing(2)
         .SetPadding(new RectOffset(0, 0, 0, 0));
      var numericControlsTag = _uiBuilder
         .CreateHorizontalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildControlWidth(false)
         .SetChildForceExpandWidth(false)
         .SetChildAlignment(TextAnchor.MiddleLeft)
         .SetSpacing(8)
         .SetPadding(new RectOffset(0, 0, 0, 0));
      var checkboxTag = _uiBuilder
         .CreateCheckbox()
         .SetTextAlignment(TextAlignmentOptions.Left)
         .SetSize(28)
         .SetFontSize(16);
      var inputFloatTag = _uiBuilder
         .CreateFloatInput()
         .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetPreferredWidth(72)
         .SetValidatorType(FloatInputFieldValidator.ValidatorType.Clamp);
      var inputStringTag = _uiBuilder
         .CreateStringInput()
         .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetPreferredWidth(104)
         .SetTrimSpaces(true)
         .SetAllowEmpty(false);
      var sliderTag = _uiBuilder
         .CreateSlider()
         .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetPreferredWidth(240);
      var textTag = _uiBuilder.CreateText().SetFontSize(16);

      var card = stackTag.Create(parent);
      Object.Instantiate(noteBackground, card.transform, false);
      var container = verticalTag.SetSpacing(0).Create(card.transform);

      var layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("MOTION PATH")
         .SetFontSize(24f)
         .SetFontWeight(FontWeight.Bold)
         .Create(layout.transform);
      layout = horizontalTag.Create(container.transform);
      CreateCheckbox(layout.transform, checkboxTag, "Enable", _config.MotionPath.Enabled, HandleMotionPathEnable);
       textTag
         .SetText("VISIBILITY")
         .SetFontSize(20f)
         .SetFontWeight(FontWeight.Bold)
         .Create(container.transform);
      var visibilityLayout = visibilityTag.Create(container.transform);
      layout = horizontalTag.Create(visibilityLayout.transform);
      CreateCheckbox(layout.transform, checkboxTag, "Path lines", _config.MotionPath.ShowPathLines, HandleMotionPathShowPathLines);
      CreateCheckbox(
         layout.transform,
         checkboxTag,
         "Current beat marker",
         _config.MotionPath.ShowCurrentBeatMarker,
         HandleMotionPathShowCurrentBeatMarker);
      layout = horizontalTag.Create(visibilityLayout.transform);
      CreateCheckbox(layout.transform, checkboxTag, "Beat markers", _config.MotionPath.ShowBeatMarkers, HandleMotionPathShowBeatMarkers);
      CreateCheckbox(
         layout.transform,
         checkboxTag,
         "Sub-beat markers",
         _config.MotionPath.ShowSubBeatMarkers,
         HandleMotionPathShowSubBeatMarkers);
      layout = horizontalTag.Create(visibilityLayout.transform);
      CreateCheckbox(
         layout.transform,
         checkboxTag,
         "Event markers",
         _config.MotionPath.ShowEventMarkers,
         HandleMotionPathShowEventMarkers);
      CreateCheckbox(layout.transform, checkboxTag, "Beat labels", _config.MotionPath.ShowBeatLabels, HandleMotionPathShowBeatLabels);

      horizontalTag.Create(container.transform);
      textTag
         .SetText("COLOR")
         .SetFontSize(20f)
         .SetFontWeight(FontWeight.Bold)
         .Create(container.transform);
      CreateMotionPathColorRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         inputStringTag,
         "Past path line",
         _config.MotionPath.GetPastLineColor(),
         value => _config.MotionPath.PastLineColor = value);
      CreateMotionPathColorRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         inputStringTag,
         "Future path line",
         _config.MotionPath.GetFutureLineColor(),
         value => _config.MotionPath.FutureLineColor = value);
      CreateMotionPathColorRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         inputStringTag,
         "Unfocused path line",
         _config.MotionPath.GetUnfocusedLineColor(),
         value => _config.MotionPath.UnfocusedLineColor = value);

      horizontalTag.Create(container.transform);
      textTag
         .SetText("SIZE")
         .SetFontSize(20f)
         .SetFontWeight(FontWeight.Bold)
         .Create(container.transform);
      _motionPathFocusedLineWidthControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Focused line width",
         _config.MotionPath.GetFocusedLineWidth(),
         MotionPathConfig.MinimumLineWidth,
         MotionPathConfig.MaximumLineWidth,
         false,
         HandleMotionPathFocusedLineWidth);
      _motionPathUnfocusedLineWidthControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Unfocused line width",
         _config.MotionPath.GetUnfocusedLineWidth(),
         MotionPathConfig.MinimumLineWidth,
         MotionPathConfig.MaximumLineWidth,
         false,
         HandleMotionPathUnfocusedLineWidth);
      _motionPathCurrentBeatMarkerSizeControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Current beat marker",
         _config.MotionPath.GetCurrentBeatMarkerSize(),
         MotionPathConfig.MinimumMarkerSize,
         MotionPathConfig.MaximumMarkerSize,
         false,
         HandleMotionPathCurrentBeatMarkerSize);
      _motionPathBeatMarkerSizeControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Beat marker",
         _config.MotionPath.GetBeatMarkerSize(),
         MotionPathConfig.MinimumMarkerSize,
         MotionPathConfig.MaximumMarkerSize,
         false,
         HandleMotionPathBeatMarkerSize);
      _motionPathSubBeatMarkerSizeControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Sub-beat marker",
         _config.MotionPath.GetSubBeatMarkerSize(),
         MotionPathConfig.MinimumMarkerSize,
         MotionPathConfig.MaximumMarkerSize,
         false,
         HandleMotionPathSubBeatMarkerSize);
      _motionPathEventMarkerSizeControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Event marker",
         _config.MotionPath.GetEventMarkerSize(),
         MotionPathConfig.MinimumMarkerSize,
         MotionPathConfig.MaximumMarkerSize,
         false,
         HandleMotionPathEventMarkerSize);
      _motionPathBeatLabelSizeControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Beat label size",
         _config.MotionPath.GetBeatLabelSize(),
         MotionPathConfig.MinimumMarkerSize,
         MotionPathConfig.MaximumMarkerSize,
         false,
         HandleMotionPathBeatLabelSize);
      _motionPathBeatLabelOffsetControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Beat label offset",
         _config.MotionPath.GetBeatLabelOffset(),
         MotionPathConfig.MinimumBeatLabelOffset,
         MotionPathConfig.MaximumBeatLabelOffset,
         false,
         HandleMotionPathBeatLabelOffset);

      horizontalTag.Create(container.transform);
      textTag
         .SetText("SAMPLING")
         .SetFontSize(20f)
         .SetFontWeight(FontWeight.Bold)
         .Create(container.transform);
      _motionPathBackwardRangeControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Backward range (beats)",
         _config.MotionPath.GetBackwardRangeInBeats(),
         MotionPathConfig.MinimumRangeInBeats,
         MotionPathConfig.MaximumRangeInBeats,
         false,
         HandleMotionPathBackwardRange);
      _motionPathForwardRangeControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Forward range (beats)",
         _config.MotionPath.GetForwardRangeInBeats(),
         MotionPathConfig.MinimumRangeInBeats,
         MotionPathConfig.MaximumRangeInBeats,
         false,
         HandleMotionPathForwardRange);
      _motionPathSamplesPerBeatControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Samples per beat",
         _config.MotionPath.GetSamplesPerBeat(),
         MotionPathConfig.MinimumSamplesPerBeat,
         MotionPathConfig.MaximumSamplesPerBeat,
         true,
         HandleMotionPathSamplesPerBeat);
      _motionPathSubBeatDivisionsControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Sub-beat divisions",
         _config.MotionPath.GetSubBeatDivisions(),
         MotionPathConfig.MinimumSubBeatDivisions,
         MotionPathConfig.MaximumSubBeatDivisions,
         true,
         HandleMotionPathSubBeatDivisions);
      horizontalTag.Create(container.transform);
      textTag
         .SetText("LIMITS")
         .SetFontSize(20f)
         .SetFontWeight(FontWeight.Bold)
         .Create(container.transform);
      _motionPathMaximumSelectedTargetsControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Maximum selected targets",
         _config.MotionPath.GetMaximumSelectedTargets(),
         MotionPathConfig.MinimumSelectedTargets,
         MotionPathConfig.MaximumSelectedTargetCount,
         true,
         HandleMotionPathMaximumSelectedTargets);
      _motionPathMaximumSelectedTracksControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Maximum selected tracks",
         _config.MotionPath.GetMaximumSelectedTracks(),
         MotionPathConfig.MinimumSelectedTracks,
         MotionPathConfig.MaximumSelectedTrackCount,
         true,
         HandleMotionPathMaximumSelectedTracks);
       _motionPathMaximumVisibleEventMarkersControl = CreateMotionPathNumericRow(
         container.transform,
         numericRowTag,
         numericControlsTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Maximum visible event markers (0 = hidden)",
         _config.MotionPath.GetMaximumVisibleEventMarkers(),
         MotionPathConfig.MinimumVisibleEventMarkers,
         MotionPathConfig.MaximumVisibleEventMarkerCount,
         true,
         HandleMotionPathMaximumVisibleEventMarkers);
   }

   private static NumericControl CreateMotionPathNumericRow(
      Transform parent,
      EditorLayoutVerticalTag numericRowTag,
      EditorLayoutHorizontalTag numericControlsTag,
      EditorTextTag textTag,
      EditorSliderTag sliderTag,
      EditorInputFloatTag inputTag,
      string label,
      float value,
      float minValue,
      float maxValue,
      bool wholeNumbers,
      Action<float> onValueChange)
   {
      var row = numericRowTag.Create(parent);
      textTag
         .SetText(label)
         .SetFontSize(16f)
         .SetFontWeight(FontWeight.Regular)
         .Create(row.transform);
      var controls = numericControlsTag.Create(row.transform);
      return CreateNumericControl(
         controls.transform,
         sliderTag,
         inputTag,
         value,
         minValue,
         maxValue,
         wholeNumbers,
         onValueChange);
   }

    private void CreateMotionPathColorRow(
       Transform parent,
       EditorLayoutVerticalTag rowTag,
       EditorLayoutHorizontalTag controlsTag,
       EditorTextTag textTag,
       EditorInputStringTag inputTag,
       string label,
       Color value,
       Action<string> updateConfig)
    {
       var row = rowTag.Create(parent);
       textTag
          .SetText(label)
          .SetFontSize(16f)
          .SetFontWeight(FontWeight.Regular)
          .Create(row.transform);

       var controls = controlsTag.Create(row.transform);
       var input = inputTag.SetValue(ToMotionPathColorHex(value)).Create(controls.transform).GetComponent<TMP_InputField>();
       var inputTextColor = input.textComponent.color;
       var swatch = CreateMotionPathColorSwatch(controls.transform, value);
       var hint = textTag
          .SetText("#RRGGBB or #RRGGBBAA")
          .SetFontSize(12f)
          .SetFontWeight(FontWeight.Regular)
          .Create(row.transform)
          .GetComponent<TMP_Text>();

       input.onEndEdit.AddListener(editedValue =>
       {
          if (!TryParseMotionPathColor(editedValue, out var color))
          {
             input.textComponent.color = Color.red;
             hint.color = Color.red;
             hint.text = "Use #RRGGBB or #RRGGBBAA.";
             return;
          }

          var normalizedValue = ToMotionPathColorHex(color);
          input.SetTextWithoutNotify(normalizedValue);
          input.textComponent.color = inputTextColor;
          hint.color = inputTextColor;
          hint.text = "#RRGGBB or #RRGGBBAA";
          swatch.color = color;
          updateConfig(normalizedValue);
          _signalBus.Fire<MotionPathRefreshSignal>();
       });
    }

    private static Image CreateMotionPathColorSwatch(Transform parent, Color color)
    {
       var swatchObject = new GameObject("MotionPathColorSwatch") { layer = 5 };
       swatchObject.transform.SetParent(parent, false);

       var swatch = swatchObject.AddComponent<Image>();
       swatch.color = color;
       var layout = swatchObject.AddComponent<LayoutElement>();
       layout.preferredWidth = 20f;
       layout.preferredHeight = 20f;
       return swatch;
    }

    private static bool TryParseMotionPathColor(string value, out Color color)
    {
       color = default;
       return !string.IsNullOrEmpty(value)
          && value[0] == '#'
          && (value.Length == 7 || value.Length == 9)
          && ColorUtility.TryParseHtmlString(value, out color);
    }

    private static string ToMotionPathColorHex(Color color) => $"#{ColorUtility.ToHtmlStringRGBA(color)}";

   private void HandleMotionPathEnable(bool value)
   {
      _config.MotionPath.Enabled = value;
      _signalBus.Fire<MotionPathRefreshSignal>();
   }

    private void HandleMotionPathShowPathLines(bool value) => UpdateMotionPathVisibility(value, value => _config.MotionPath.ShowPathLines = value);
   private void HandleMotionPathShowCurrentBeatMarker(bool value) => UpdateMotionPathVisibility(value, value => _config.MotionPath.ShowCurrentBeatMarker = value);
   private void HandleMotionPathShowBeatMarkers(bool value) => UpdateMotionPathVisibility(value, value => _config.MotionPath.ShowBeatMarkers = value);
   private void HandleMotionPathShowSubBeatMarkers(bool value) => UpdateMotionPathVisibility(value, value => _config.MotionPath.ShowSubBeatMarkers = value);
   private void HandleMotionPathShowEventMarkers(bool value) => UpdateMotionPathVisibility(value, value => _config.MotionPath.ShowEventMarkers = value);
   private void HandleMotionPathShowBeatLabels(bool value) => UpdateMotionPathVisibility(value, value => _config.MotionPath.ShowBeatLabels = value);

   private void UpdateMotionPathVisibility(bool value, Action<bool> updateConfig)
   {
      updateConfig(value);
      _signalBus.Fire<MotionPathRefreshSignal>();
   }

   private void UpdateMotionPathFloat(
      float value,
      Func<float, float> normalize,
      Action<float> updateConfig,
      NumericControl control)
   {
      var normalized = normalize(value);
      updateConfig(normalized);
      control.SetValueWithoutNotify(normalized);
      _signalBus.Fire<MotionPathRefreshSignal>();
   }

   private void UpdateMotionPathInteger(
      float value,
      Func<float, int> normalize,
      Action<int> updateConfig,
      NumericControl control)
   {
      var normalized = normalize(value);
      updateConfig(normalized);
      control.SetValueWithoutNotify(normalized);
      _signalBus.Fire<MotionPathRefreshSignal>();
   }

   private void HandleMotionPathFocusedLineWidth(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampFocusedLineWidth(Mathf.Round(value * 1000f) / 1000f),
      normalized => _config.MotionPath.FocusedLineWidth = normalized,
      _motionPathFocusedLineWidthControl);

   private void HandleMotionPathUnfocusedLineWidth(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampUnfocusedLineWidth(Mathf.Round(value * 1000f) / 1000f),
      normalized => _config.MotionPath.UnfocusedLineWidth = normalized,
      _motionPathUnfocusedLineWidthControl);

   private void HandleMotionPathCurrentBeatMarkerSize(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampMarkerSize(Mathf.Round(value * 100f) / 100f, 0.30f),
      normalized => _config.MotionPath.CurrentBeatMarkerSize = normalized,
      _motionPathCurrentBeatMarkerSizeControl);

   private void HandleMotionPathBeatMarkerSize(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampMarkerSize(Mathf.Round(value * 100f) / 100f, 0.20f),
      normalized => _config.MotionPath.BeatMarkerSize = normalized,
      _motionPathBeatMarkerSizeControl);

   private void HandleMotionPathSubBeatMarkerSize(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampMarkerSize(Mathf.Round(value * 100f) / 100f, 0.11f),
      normalized => _config.MotionPath.SubBeatMarkerSize = normalized,
      _motionPathSubBeatMarkerSizeControl);

   private void HandleMotionPathEventMarkerSize(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampMarkerSize(Mathf.Round(value * 100f) / 100f, 0.24f),
      normalized => _config.MotionPath.EventMarkerSize = normalized,
      _motionPathEventMarkerSizeControl);

   private void HandleMotionPathBeatLabelSize(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampMarkerSize(Mathf.Round(value * 100f) / 100f, 0.14f),
      normalized => _config.MotionPath.BeatLabelSize = normalized,
      _motionPathBeatLabelSizeControl);

   private void HandleMotionPathBeatLabelOffset(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampBeatLabelOffset(Mathf.Round(value * 100f) / 100f),
      normalized => _config.MotionPath.BeatLabelOffset = normalized,
      _motionPathBeatLabelOffsetControl);

   private void HandleMotionPathBackwardRange(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampRangeInBeats(Mathf.Round(value * 4f) / 4f),
      normalized => _config.MotionPath.BackwardRangeInBeats = normalized,
      _motionPathBackwardRangeControl);

   private void HandleMotionPathForwardRange(float value) => UpdateMotionPathFloat(
      value,
      value => MotionPathConfig.ClampRangeInBeats(Mathf.Round(value * 4f) / 4f),
      normalized => _config.MotionPath.ForwardRangeInBeats = normalized,
      _motionPathForwardRangeControl);

   private void HandleMotionPathSamplesPerBeat(float value) => UpdateMotionPathInteger(
      value,
      value => MotionPathConfig.ClampSamplesPerBeat(Mathf.RoundToInt(value)),
      normalized => _config.MotionPath.SamplesPerBeat = normalized,
      _motionPathSamplesPerBeatControl);

   private void HandleMotionPathSubBeatDivisions(float value) => UpdateMotionPathInteger(
      value,
      value => MotionPathConfig.ClampSubBeatDivisions(Mathf.RoundToInt(value)),
      normalized => _config.MotionPath.SubBeatDivisions = normalized,
      _motionPathSubBeatDivisionsControl);

   private void HandleMotionPathMaximumSelectedTargets(float value) => UpdateMotionPathInteger(
      value,
      value => MotionPathConfig.ClampMaximumSelectedTargets(Mathf.RoundToInt(value)),
      normalized =>
      {
         _config.MotionPath.MaximumSelectedTargets = normalized;
         if (_config.MotionPath.MaximumSelectedTracks < normalized)
         {
            _config.MotionPath.MaximumSelectedTracks = normalized;
            _motionPathMaximumSelectedTracksControl.SetValueWithoutNotify(normalized);
         }
      },
      _motionPathMaximumSelectedTargetsControl);

   private void HandleMotionPathMaximumSelectedTracks(float value) => UpdateMotionPathInteger(
      value,
      _config.MotionPath.NormalizeMaximumSelectedTracks,
      normalized => _config.MotionPath.MaximumSelectedTracks = normalized,
      _motionPathMaximumSelectedTracksControl);

    private void HandleMotionPathMaximumVisibleEventMarkers(float value) => UpdateMotionPathInteger(
      value,
      value => MotionPathConfig.ClampMaximumVisibleEventMarkers(Mathf.RoundToInt(value)),
      normalized => _config.MotionPath.MaximumVisibleEventMarkers = normalized,
      _motionPathMaximumVisibleEventMarkersControl);
}
