using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.Misc;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class ConfigurationView : IInitializable
{
   private readonly PluginConfig _config;
   private readonly EditBeatmapViewController _ebvc;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;
   private NumericControl _colorGradientControl;
   private NumericControl _colorIdControl;
   private NumericControl _globalScaleControl;
   private NumericControl _sizeBaseControl;
   private NumericControl _sizeRotationControl;
   private NumericControl _sizeTranslationControl;

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

   public ConfigurationView(
      SignalBus signalBus,
      PluginConfig config,
      EditBeatmapViewController ebvc,
      UIBuilder uiBuilder)
   {
      _signalBus = signalBus;
      _config = config;
      _ebvc = ebvc;
      _uiBuilder = uiBuilder;
   }

   public void Initialize()
   {
      var target = _ebvc._editBeatmapRightPanelView._scrollView.contentTransform;

      var stackTag = _uiBuilder.CreateStackLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.MiddleCenter)
         .SetPadding(new RectOffset(4, 4, 4, 4));
      var verticalTag = _uiBuilder.CreateVerticalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.UpperLeft)
         .SetPadding(new RectOffset(4, 4, 4, 4));
      var horizontalTag = _uiBuilder.CreateHorizontalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetChildAlignment(TextAnchor.MiddleLeft)
         .SetSpacing(8)
         .SetPadding(new RectOffset(4, 4, 4, 4));
      var checkboxTag = _uiBuilder.CreateCheckbox()
         .SetTextAlignment(TextAlignmentOptions.Left)
         .SetSize(28)
         .SetFontSize(16);
      var inputFloatTag = _uiBuilder.CreateFloatInput()
         .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetPreferredWidth(80)
         .SetMinValue(GizmoAssets.MinSize)
         .SetMaxValue(GizmoAssets.MaxSize)
         .SetValidatorType(FloatInputFieldValidator.ValidatorType.Clamp);
      var sliderTag = _uiBuilder.CreateSlider()
         .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetPreferredWidth(260)
         .SetMinValue(GizmoAssets.MinSize)
         .SetMaxValue(GizmoAssets.MaxSize);
      var textTag = _uiBuilder.CreateText()
         .SetFontSize(16);

      var mainContainer = verticalTag
         .SetSpacing(4)
         .Create(target);
      mainContainer.name = "EditorEnhancedView";
      var container = stackTag.Create(mainContainer.transform);
      Object.Instantiate(
         _ebvc._editBeatmapRightPanelView._editObjectView._noteDataView.transform.Find("Background4px"),
         container.transform,
         false);

      container = verticalTag
         .SetSpacing(0)
         .Create(container.transform);
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
      checkboxTag
         .SetText("Draggable")
         .SetBool(_config.Gizmo.Draggable)
         .SetOnValueChange(HandleGizmoDraggable)
         .Create(layout.transform);
      checkboxTag
         .SetText("Swappable")
         .SetBool(_config.Gizmo.Swappable)
         .SetOnValueChange(HandleGizmoSwappable)
         .Create(layout.transform);

      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("Visualization")
         .Create(layout.transform);
      layout = verticalTag.Create(layout.transform);
      checkboxTag
         .SetText("Highlight")
         .SetBool(_config.Gizmo.Highlight)
         .SetOnValueChange(HandleGizmoHighlight)
         .Create(layout.transform);
      checkboxTag
         .SetText("Multicolor ID")
         .SetBool(_config.Gizmo.MulticolorId)
         .SetOnValueChange(HandleGizmoIdColor)
         .Create(layout.transform);
      checkboxTag
         .SetText("Distribute Shape")
         .SetBool(_config.Gizmo.DistributeShape)
         .SetOnValueChange(HandleGizmoDistributeShape)
         .Create(layout.transform);

      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("Show")
         .Create(layout.transform);
      checkboxTag
         .SetText("Base")
         .SetBool(_config.Gizmo.ShowBase)
         .SetOnValueChange(HandleGizmoShowBase)
         .Create(layout.transform);
      checkboxTag
         .SetText("Modifier")
         .SetBool(_config.Gizmo.ShowModifier)
         .SetOnValueChange(HandleGizmoShowModifier)
         .Create(layout.transform);
      checkboxTag
         .SetText("Lane")
         .SetBool(_config.Gizmo.ShowLane)
         .SetOnValueChange(HandleGizmoShowLane)
         .Create(layout.transform);
      checkboxTag
         .SetText("Info")
         .SetBool(_config.Gizmo.ShowInfo)
         .SetOnValueChange(HandleGizmoShowInfo)
         .Create(layout.transform);

      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("Interaction")
         .Create(layout.transform);
      checkboxTag
         .SetText("Gizmo")
         .SetBool(_config.Gizmo.RaycastGizmo)
         .SetOnValueChange(HandleGizmoRaycastGizmo)
         .Create(layout.transform);
      checkboxTag
         .SetText("Lane")
         .SetBool(_config.Gizmo.RaycastLane)
         .SetOnValueChange(HandleGizmoRaycastLane)
         .Create(layout.transform);

      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("SIZE")
         .SetFontSize(20f)
         .SetFontWeight(FontWeight.Bold)
         .Create(container.transform);
      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("Global Scale")
         .SetFontSize(16f)
         .SetFontWeight(FontWeight.Regular)
         .Create(layout.transform);
      _globalScaleControl = CreateNumericControl(
         layout.transform,
         sliderTag,
         inputFloatTag,
         _config.Gizmo.GlobalScale,
         GizmoAssets.MinSize,
         GizmoAssets.MaxSize,
         false,
         HandleGizmoGlobalScale);
      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("Base")
         .Create(layout.transform);
      _sizeBaseControl = CreateNumericControl(
         layout.transform,
         sliderTag,
         inputFloatTag,
         _config.Gizmo.SizeBase,
         GizmoAssets.MinSize,
         GizmoAssets.MaxSize,
         false,
         HandleGizmoSizeBase);
      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("Rotation")
         .Create(layout.transform);
      _sizeRotationControl = CreateNumericControl(
         layout.transform,
         sliderTag,
         inputFloatTag,
         _config.Gizmo.SizeRotation,
         GizmoAssets.MinSize,
         GizmoAssets.MaxSize,
         false,
         HandleGizmoSizeRotation);
      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("Translation")
         .Create(layout.transform);
      _sizeTranslationControl = CreateNumericControl(
         layout.transform,
         sliderTag,
         inputFloatTag,
         _config.Gizmo.SizeTranslation,
         GizmoAssets.MinSize,
         GizmoAssets.MaxSize,
         false,
         HandleGizmoSizeTranslation);

      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("COLOR")
         .SetFontSize(20f)
         .SetFontWeight(FontWeight.Bold)
         .Create(container.transform);
      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("ID Step")
         .SetFontSize(16f)
         .SetFontWeight(FontWeight.Regular)
         .Create(layout.transform);
      _colorIdControl = CreateNumericControl(
         layout.transform,
         sliderTag,
         inputFloatTag,
         _config.Gizmo.ColorIdStep,
         -8,
         8,
         true,
         HandleGizmoColorIdSkip);
      layout = horizontalTag.Create(container.transform);
      textTag
         .SetText("Gradient Step")
         .Create(layout.transform);
      _colorGradientControl = CreateNumericControl(
         layout.transform,
         sliderTag,
         inputFloatTag,
         _config.Gizmo.ColorGradientStep,
         -16,
         16,
         true,
         HandleGizmoColorGradientSkip);

      container = stackTag.Create(mainContainer.transform);
      Object.Instantiate(
         _ebvc._editBeatmapRightPanelView._editObjectView._noteDataView.transform.Find("Background4px"),
         container.transform,
         false);
      container = verticalTag.Create(container.transform);

      layout = horizontalTag
         .Create(container.transform);
      textTag
         .SetText("PRECISION")
         .SetFontSize(24f)
         .SetFontWeight(FontWeight.Bold)
         .Create(layout.transform);
      layout = horizontalTag
         .Create(container.transform);
      textTag
         .SetText("Color")
         .SetFontSize(16)
         .SetFontWeight(FontWeight.Regular)
         .Create(layout.transform);
      CreatePrecisionInputs(
         layout.transform,
         LightColorEventHelper._precisions,
         (key, value) =>
         {
            LightColorEventHelper._precisions[key] = value;
            _config.Precision.Color[(int)key] = value;
         });

      layout = horizontalTag
         .Create(container.transform);
      textTag
         .SetText("Rotation")
         .Create(layout.transform);
      CreatePrecisionInputs(
         layout.transform,
         ModifyHoveredLightRotationDeltaRotationCommand._precisions,
         (key, value) =>
         {
            ModifyHoveredLightRotationDeltaRotationCommand._precisions[key] = value;
            _config.Precision.Rotation[(int)key] = value;
         });

      layout = horizontalTag
         .Create(container.transform);
      textTag
         .SetText("Translation")
         .Create(layout.transform);
      CreatePrecisionInputs(
         layout.transform,
         ModifyHoveredLightTranslationDeltaTranslationCommand._precisions,
         (key, value) =>
         {
            ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[key] = value;
            _config.Precision.Translation[(int)key] = value;
         });

      layout = horizontalTag
         .Create(container.transform);
      textTag
         .SetText("FX")
         .Create(layout.transform);
      CreatePrecisionInputs(
         layout.transform,
         ModifyHoveredFloatFxDeltaValueCommand._precisions,
         (key, value) =>
         {
            ModifyHoveredFloatFxDeltaValueCommand._precisions[key] = value;
            _config.Precision.Fx[(int)key] = value;
         });

      layout = horizontalTag
         .Create(container.transform);
      textTag
         .SetText("Time")
         .Create(layout.transform);
      CreatePrecisionInputs(
         layout.transform,
         CustomPrecisions.TimePrecisionFloat,
         (key, value) =>
         {
            CustomPrecisions.TimePrecisionFloat[key] = value;
            _config.Precision.Time[(int)key] = value;
         });

      layout = horizontalTag
         .Create(container.transform);
      textTag
         .SetText("Percent")
         .Create(layout.transform);
      CreatePrecisionInputs(
         layout.transform,
         CustomPrecisions.PercentPrecisionFloat,
         (key, value) =>
         {
            CustomPrecisions.PercentPrecisionFloat[key] = value;
            CustomPrecisions.PercentPrecisionInt[key] = (int)Math.Round(value);
            _config.Precision.Percent[(int)key] = value;
         });

      var configPanel = new EditBeatmapRightPanelView.PanelElement
      {
         name = "Editor Enhanced",
         panelType = (BeatmapPanelType)(Enum.GetValues(typeof(BeatmapPanelType)).Length + 1),
         elements = [mainContainer]
      };
      _ebvc._editBeatmapRightPanelView._panels = _ebvc._editBeatmapRightPanelView._panels.AddToArray(configPanel);
      _ebvc._editBeatmapRightPanelView._dropdown.SetTexts(
         _ebvc
            ._editBeatmapRightPanelView._panels.Select(p => p.name)
            .ToArray());
      _ebvc._editBeatmapRightPanelView._dropdown._numberOfVisibleCell =
         _ebvc._editBeatmapRightPanelView._panels.Length;
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

   private void CreatePrecisionInputs<TKey>(
      Transform parent,
      IDictionary<TKey, float> values,
      Action<TKey, float> onValueChange)
   {
      foreach (var key in values.Keys)
         _uiBuilder.CreateFloatInput()
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetPreferredWidth(80)
            .SetMinValue(-16)
            .SetMaxValue(16)
            .SetValidatorType(FloatInputFieldValidator.ValidatorType.None)
            .SetValue(values[key])
            .SetOnValueChange(value => onValueChange(key, value))
            .Create(parent);
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
      _signalBus.Fire<GizmoConfigRaycastGizmoUpdateSignal>();
   }

   private void HandleGizmoRaycastLane(bool value)
   {
      _config.Gizmo.RaycastLane = value;
      _signalBus.Fire<GizmoConfigRaycastLaneUpdateSignal>();
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
         () => _signalBus.Fire<GizmoConfigGlobalScaleUpdateSignal>());
   }

   private void HandleGizmoSizeBase(float value)
   {
      UpdateRoundedFloat(
         value,
         normalized => _config.Gizmo.SizeBase = normalized,
         _sizeBaseControl,
         () => _signalBus.Fire<GizmoConfigSizeBaseUpdateSignal>());
   }

   private void HandleGizmoSizeRotation(float value)
   {
      UpdateRoundedFloat(
         value,
         normalized => _config.Gizmo.SizeRotation = normalized,
         _sizeRotationControl,
         () => _signalBus.Fire<GizmoConfigSizeRotationUpdateSignal>());
   }

   private void HandleGizmoSizeTranslation(float value)
   {
      UpdateRoundedFloat(
         value,
         normalized => _config.Gizmo.SizeTranslation = normalized,
         _sizeTranslationControl,
         () => _signalBus.Fire<GizmoConfigSizeTranslationUpdateSignal>());
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
}
