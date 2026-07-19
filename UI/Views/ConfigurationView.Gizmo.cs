using System;
using BeatmapEditor3D;
using EditorEnhanced.Gizmo;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

public partial class ConfigurationView
{
   private NumericControl _colorGradientControl;
   private NumericControl _colorIdControl;
   private NumericControl _globalScaleControl;
   private NumericControl _sizeBaseControl;
   private NumericControl _sizeRotationControl;
   private NumericControl _sizeTranslationControl;

   private void BuildGizmoSection(Transform parent, Transform noteBackground)
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
         .SetChildAlignment(TextAnchor.MiddleLeft)
         .SetSpacing(8)
         .SetPadding(new RectOffset(4, 4, 4, 4));
      var checkboxTag = _uiBuilder
         .CreateCheckbox()
         .SetTextAlignment(TextAlignmentOptions.Left)
         .SetSize(28)
         .SetFontSize(16);
      var inputFloatTag = _uiBuilder
         .CreateFloatInput()
         .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetPreferredWidth(80)
         .SetMinValue(GizmoAssets.MinSize)
         .SetMaxValue(GizmoAssets.MaxSize)
         .SetValidatorType(FloatInputFieldValidator.ValidatorType.Clamp);
      var sliderTag = _uiBuilder
         .CreateSlider()
         .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetPreferredWidth(260)
         .SetMinValue(GizmoAssets.MinSize)
         .SetMaxValue(GizmoAssets.MaxSize);
      var textTag = _uiBuilder.CreateText().SetFontSize(16);

      var card = stackTag.Create(parent);
      Object.Instantiate(noteBackground, card.transform, false);
      var container = verticalTag.SetSpacing(0).Create(card.transform);

      var layout = horizontalTag
         .SetChildControlWidth(false)
         .SetChildForceExpandWidth(false)
         .Create(container.transform);
      textTag
         .SetText("GIZMO")
         .SetFontSize(24f)
         .SetFontWeight(FontWeight.Bold)
         .Create(layout.transform);
      checkboxTag
         .SetText("Enable")
         .SetBool(_config.Gizmo.Enabled)
         .SetOnValueChange(HandleGizmoEnable)
         .Create(layout.transform);

      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("Functionality")
         .SetFontSize(16f)
         .SetFontWeight(FontWeight.Regular)
         .Create(layout.transform);
      CreateCheckbox(layout.transform, checkboxTag, "Draggable", _config.Gizmo.Draggable, HandleGizmoDraggable);
      CreateCheckbox(layout.transform, checkboxTag, "Swappable", _config.Gizmo.Swappable, HandleGizmoSwappable);

      layout = horizontalTag.Create(container.transform);
      textTag.SetText("Visualization").Create(layout.transform);
      layout = verticalTag.Create(layout.transform);
      CreateCheckbox(layout.transform, checkboxTag, "Highlight", _config.Gizmo.Highlight, HandleGizmoHighlight);
      CreateCheckbox(layout.transform, checkboxTag, "Multicolor ID", _config.Gizmo.MulticolorId, HandleGizmoIdColor);
      CreateCheckbox(
         layout.transform,
         checkboxTag,
         "Distribute Shape",
         _config.Gizmo.DistributeShape,
         HandleGizmoDistributeShape);

      layout = horizontalTag.Create(container.transform);
      textTag.SetText("Show").Create(layout.transform);
      CreateCheckbox(layout.transform, checkboxTag, "Base", _config.Gizmo.ShowBase, HandleGizmoShowBase);
      CreateCheckbox(layout.transform, checkboxTag, "Modifier", _config.Gizmo.ShowModifier, HandleGizmoShowModifier);
      CreateCheckbox(layout.transform, checkboxTag, "Lane", _config.Gizmo.ShowLane, HandleGizmoShowLane);
      CreateCheckbox(layout.transform, checkboxTag, "Info", _config.Gizmo.ShowInfo, HandleGizmoShowInfo);

      layout = horizontalTag.Create(container.transform);
      textTag.SetText("Interaction").Create(layout.transform);
      CreateCheckbox(layout.transform, checkboxTag, "Gizmo", _config.Gizmo.RaycastGizmo, HandleGizmoRaycastGizmo);
      CreateCheckbox(layout.transform, checkboxTag, "Lane", _config.Gizmo.RaycastLane, HandleGizmoRaycastLane);

      horizontalTag.Create(container.transform);
      textTag
         .SetText("SIZE")
         .SetFontSize(20f)
         .SetFontWeight(FontWeight.Bold)
         .Create(container.transform);
      _globalScaleControl = CreateNumericRow(
         container.transform,
         horizontalTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Global Scale",
         _config.Gizmo.GlobalScale,
         GizmoAssets.MinSize,
         GizmoAssets.MaxSize,
         false,
         HandleGizmoGlobalScale);
      _sizeBaseControl = CreateNumericRow(
         container.transform,
         horizontalTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Base",
         _config.Gizmo.SizeBase,
         GizmoAssets.MinSize,
         GizmoAssets.MaxSize,
         false,
         HandleGizmoSizeBase);
      _sizeRotationControl = CreateNumericRow(
         container.transform,
         horizontalTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Rotation",
         _config.Gizmo.SizeRotation,
         GizmoAssets.MinSize,
         GizmoAssets.MaxSize,
         false,
         HandleGizmoSizeRotation);
      _sizeTranslationControl = CreateNumericRow(
         container.transform,
         horizontalTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Translation",
         _config.Gizmo.SizeTranslation,
         GizmoAssets.MinSize,
         GizmoAssets.MaxSize,
         false,
         HandleGizmoSizeTranslation);

      horizontalTag.Create(container.transform);
      textTag
         .SetText("COLOR")
         .SetFontSize(20f)
         .SetFontWeight(FontWeight.Bold)
         .Create(container.transform);
      _colorIdControl = CreateNumericRow(
         container.transform,
         horizontalTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "ID Step",
         _config.Gizmo.ColorIdStep,
         -8,
         8,
         true,
         HandleGizmoColorIdSkip);
      _colorGradientControl = CreateNumericRow(
         container.transform,
         horizontalTag,
         textTag,
         sliderTag,
         inputFloatTag,
         "Gradient Step",
         _config.Gizmo.ColorGradientStep,
         -16,
         16,
         true,
         HandleGizmoColorGradientSkip);
   }

   private static void CreateCheckbox(
      Transform parent,
      EditorCheckboxTag checkboxTag,
      string text,
      bool value,
      Action<bool> onValueChange)
   {
      checkboxTag
         .SetText(text)
         .SetBool(value)
         .SetOnValueChange(onValueChange)
         .Create(parent);
   }

   private static NumericControl CreateNumericRow(
      Transform parent,
      EditorLayoutHorizontalTag horizontalTag,
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
      var layout = horizontalTag.Create(parent);
      textTag
         .SetText(label)
         .SetFontSize(16f)
         .SetFontWeight(FontWeight.Regular)
         .Create(layout.transform);
      return CreateNumericControl(
         layout.transform,
         sliderTag,
         inputTag,
         value,
         minValue,
         maxValue,
         wholeNumbers,
         onValueChange);
   }

   private static NumericControl CreateNumericControl(
      Transform parent,
      EditorSliderTag sliderTag,
      EditorInputFloatTag inputTag,
      float value,
      float minValue,
      float maxValue,
      bool wholeNumbers,
      Action<float> onValueChange)
   {
      var slider = sliderTag
         .SetValue(value)
         .SetMinValue(minValue)
         .SetMaxValue(maxValue)
         .SetWholeNumber(wholeNumbers)
         .SetOnValueChange(onValueChange)
         .Create(parent)
         .GetComponent<Slider>();
      var input = inputTag
         .SetValue(value)
         .SetMinValue(minValue)
         .SetMaxValue(maxValue)
         .SetOnValueChange(onValueChange)
         .Create(parent)
         .GetComponent<FloatInputFieldValidator>();

      return new NumericControl(slider, input);
   }

   private static void UpdateRoundedFloat(
      float value,
      Action<float> updateConfig,
      NumericControl control,
      Action fireSignal)
   {
      var normalized = Mathf.Clamp(
         Mathf.Round(value * 100f) / 100f,
         GizmoAssets.MinSize,
         GizmoAssets.MaxSize);
      updateConfig(normalized);
      control.SetValueWithoutNotify(normalized);
      fireSignal();
   }

   private static void UpdateRoundedInteger(
      float value,
      int minValue,
      int maxValue,
      Action<int> updateConfig,
      NumericControl control,
      Action fireSignal)
   {
      var normalized = (int)Math.Clamp(Mathf.Round(value), minValue, maxValue);
      updateConfig(normalized);
      control.SetValueWithoutNotify(normalized);
      fireSignal();
   }

   private void HandleGizmoEnable(bool value)
   {
      _config.Gizmo.Enabled = value;
      _signalBus.Fire<GizmoRefreshSignal>();
   }

   private void HandleGizmoDraggable(bool value)
   {
      _config.Gizmo.Draggable = value;
   }

   private void HandleGizmoSwappable(bool value)
   {
      _config.Gizmo.Swappable = value;
   }

   private void HandleGizmoRaycastGizmo(bool value)
   {
      _config.Gizmo.RaycastGizmo = value;
      _signalBus.Fire<GizmoColliderConfigChangedSignal>();
   }

   private void HandleGizmoRaycastLane(bool value)
   {
      _config.Gizmo.RaycastLane = value;
      _signalBus.Fire<GizmoColliderConfigChangedSignal>();
   }

   private void HandleGizmoShowBase(bool value)
   {
      _config.Gizmo.ShowBase = value;
      _signalBus.Fire<GizmoRefreshSignal>();
   }

   private void HandleGizmoShowModifier(bool value)
   {
      _config.Gizmo.ShowModifier = value;
      _signalBus.Fire<GizmoRefreshSignal>();
   }

   private void HandleGizmoShowLane(bool value)
   {
      _config.Gizmo.ShowLane = value;
      _signalBus.Fire<GizmoRefreshSignal>();
   }

   private void HandleGizmoShowInfo(bool value)
   {
      _config.Gizmo.ShowInfo = value;
      _signalBus.Fire<GizmoRefreshSignal>();
   }

   private void HandleGizmoHighlight(bool value)
   {
      _config.Gizmo.Highlight = value;
   }

   private void HandleGizmoIdColor(bool value)
   {
      _config.Gizmo.MulticolorId = value;
      _signalBus.Fire<GizmoRefreshSignal>();
   }

   private void HandleGizmoDistributeShape(bool value)
   {
      _config.Gizmo.DistributeShape = value;
      _signalBus.Fire<GizmoRefreshSignal>();
   }

   private void HandleGizmoGlobalScale(float value)
   {
      UpdateRoundedFloat(
         value,
         normalized => _config.Gizmo.GlobalScale = normalized,
         _globalScaleControl,
         () => _signalBus.Fire<GizmoScaleConfigChangedSignal>());
   }

   private void HandleGizmoSizeBase(float value)
   {
      UpdateRoundedFloat(
         value,
         normalized => _config.Gizmo.SizeBase = normalized,
         _sizeBaseControl,
         () => _signalBus.Fire<GizmoScaleConfigChangedSignal>());
   }

   private void HandleGizmoSizeRotation(float value)
   {
      UpdateRoundedFloat(
         value,
         normalized => _config.Gizmo.SizeRotation = normalized,
         _sizeRotationControl,
         () => _signalBus.Fire<GizmoScaleConfigChangedSignal>());
   }

   private void HandleGizmoSizeTranslation(float value)
   {
      UpdateRoundedFloat(
         value,
         normalized => _config.Gizmo.SizeTranslation = normalized,
         _sizeTranslationControl,
         () => _signalBus.Fire<GizmoScaleConfigChangedSignal>());
   }

   private void HandleGizmoColorIdSkip(float value)
   {
      UpdateRoundedInteger(
         value,
         -8,
         8,
         normalized => _config.Gizmo.ColorIdStep = normalized,
         _colorIdControl,
         () => _signalBus.Fire<GizmoRefreshSignal>());
   }

   private void HandleGizmoColorGradientSkip(float value)
   {
      UpdateRoundedInteger(
         value,
         -16,
         16,
         normalized => _config.Gizmo.ColorGradientStep = normalized,
         _colorGradientControl,
         () => _signalBus.Fire<GizmoRefreshSignal>());
   }

   private sealed class NumericControl
   {
      private readonly FloatInputFieldValidator _input;
      private readonly Slider _slider;

      public NumericControl(Slider slider, FloatInputFieldValidator input)
      {
         _slider = slider;
         _input = input;
      }

      public void SetValueWithoutNotify(float value)
      {
         _slider.SetValueWithoutNotify(value);
         _input.SetValueWithoutNotify(value, true);
      }
   }
}
