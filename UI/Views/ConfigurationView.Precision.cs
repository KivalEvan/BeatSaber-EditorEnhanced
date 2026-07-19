using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Types;
using EditorEnhanced.Configuration;
using EditorEnhanced.Misc;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

public partial class ConfigurationView
{
   private void BuildPrecisionSection(Transform parent, Transform noteBackground)
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
         .SetPadding(new RectOffset(4, 4, 4, 4))
         .SetSpacing(0);
      var horizontalTag = _uiBuilder
         .CreateHorizontalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.MiddleLeft)
         .SetSpacing(8)
         .SetPadding(new RectOffset(4, 4, 4, 4))
         .SetChildControlWidth(false)
         .SetChildForceExpandWidth(false);
      var textTag = _uiBuilder.CreateText().SetFontSize(16);

      var card = stackTag.Create(parent);
      Object.Instantiate(noteBackground, card.transform, false);
      var container = verticalTag.Create(card.transform);

      var layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("PRECISION")
         .SetFontSize(24f)
         .SetFontWeight(FontWeight.Bold)
         .Create(layout.transform);

      CreatePrecisionRow(
         container.transform,
         horizontalTag,
         textTag,
         "Color",
         LightColorEventHelper._precisions,
         (key, value) =>
         {
            LightColorEventHelper._precisions[key] = value;
            _config.Precision.Color[PrecisionDefaults.GetIndex(key)] = value;
         });
      CreatePrecisionRow(
         container.transform,
         horizontalTag,
         textTag,
         "Rotation",
         ModifyHoveredLightRotationDeltaRotationCommand._precisions,
         (key, value) =>
         {
            ModifyHoveredLightRotationDeltaRotationCommand._precisions[key] = value;
            _config.Precision.Rotation[PrecisionDefaults.GetIndex(key)] = value;
         });
      CreatePrecisionRow(
         container.transform,
         horizontalTag,
         textTag,
         "Translation",
         ModifyHoveredLightTranslationDeltaTranslationCommand._precisions,
         (key, value) =>
         {
            ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[key] = value;
            _config.Precision.Translation[PrecisionDefaults.GetIndex(key)] = value;
         });
      CreatePrecisionRow(
         container.transform,
         horizontalTag,
         textTag,
         "FX",
         ModifyHoveredFloatFxDeltaValueCommand._precisions,
         (key, value) =>
         {
            ModifyHoveredFloatFxDeltaValueCommand._precisions[key] = value;
            _config.Precision.Fx[PrecisionDefaults.GetIndex(key)] = value;
         });
      CreatePrecisionRow(
         container.transform,
         horizontalTag,
         textTag,
         "Time",
         CustomPrecisions.TimePrecisionFloat,
         (key, value) =>
         {
            CustomPrecisions.TimePrecisionFloat[key] = value;
            _config.Precision.Time[PrecisionDefaults.GetIndex(key)] = value;
         });
      CreatePrecisionRow(
         container.transform,
         horizontalTag,
         textTag,
         "Percent",
         CustomPrecisions.PercentPrecisionFloat,
         (key, value) =>
         {
            CustomPrecisions.PercentPrecisionFloat[key] = value;
            CustomPrecisions.PercentPrecisionInt[key] = Math.Max(1, (int)Math.Round(value));
            _config.Precision.Percent[PrecisionDefaults.GetIndex(key)] = value;
         });
   }

   private void CreatePrecisionRow(
      Transform parent,
      EditorLayoutHorizontalTag horizontalTag,
      EditorTextTag textTag,
      string label,
      IDictionary<PrecisionType, float> values,
      Action<PrecisionType, float> onValueChange)
   {
      var layout = horizontalTag.Create(parent);
      textTag
         .SetText(label)
         .SetFontSize(16)
         .SetFontWeight(FontWeight.Regular)
         .Create(layout.transform);
      CreatePrecisionInputs(layout.transform, values, onValueChange);
   }

   private void CreatePrecisionInputs(
      Transform parent,
      IDictionary<PrecisionType, float> values,
      Action<PrecisionType, float> onValueChange)
   {
      foreach (var key in PrecisionDefaults.SupportedTypes)
      {
         if (!values.ContainsKey(key)) continue;
         FloatInputFieldValidator input = null;
         input = _uiBuilder
            .CreateFloatInput()
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetPreferredWidth(80)
            .SetMinValue(-16)
            .SetMaxValue(16)
            .SetValidatorType(FloatInputFieldValidator.ValidatorType.None)
            .SetValue(values[key])
            .SetOnValueChange(value =>
            {
               var normalized = PrecisionConfigurationInitializer.IsValid(value) ? value : values[key];
               onValueChange(key, normalized);
               if (!Mathf.Approximately(value, normalized)) input.SetValueWithoutNotify(normalized, true);
            })
            .Create(parent)
            .GetComponent<FloatInputFieldValidator>();
      }
   }
}
