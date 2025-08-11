using System;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.UI.Extensions;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class ConfigurationViewController : IInitializable
{
    private readonly PluginConfig _config;
    private readonly EditBeatmapViewController _ebvc;
    private readonly SignalBus _signalBus;
    private readonly UIBuilder _uiBuilder;
    private GameObject _globalScaleInput;

    private GameObject _globalScaleSlider;
    private GameObject _sizeBaseInput;
    private GameObject _sizeBaseSlider;
    private GameObject _sizeRotationInput;
    private GameObject _sizeRotationSlider;
    private GameObject _sizeTranslationInput;
    private GameObject _sizeTranslationSlider;

    public ConfigurationViewController(SignalBus signalBus,
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

        var stackTag = _uiBuilder.LayoutStack.Instantiate()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetChildAlignment(TextAnchor.MiddleCenter)
            .SetPadding(new RectOffset(4, 4, 4, 4));
        var verticalTag = _uiBuilder.LayoutVertical.Instantiate()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetChildAlignment(TextAnchor.UpperLeft)
            .SetPadding(new RectOffset(4, 4, 4, 4));
        var horizontalTag = _uiBuilder.LayoutHorizontal.Instantiate()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetChildAlignment(TextAnchor.MiddleLeft)
            .SetSpacing(8)
            .SetPadding(new RectOffset(4, 4, 4, 4));
        var checkboxTag = _uiBuilder.Checkbox.Instantiate()
            .SetTextAlignment(TextAlignmentOptions.Left)
            .SetSize(28)
            .SetFontSize(16);
        var inputFloatTag = _uiBuilder.InputFloat.Instantiate()
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetPreferredWidth(80)
            .SetMinValue(GizmoAssets.MinSize)
            .SetMaxValue(GizmoAssets.MaxSize)
            .SetValidatorType(FloatInputFieldValidator.ValidatorType.Clamp)
            .SetPadding(new RectOffset(2, 2, 2, 2));
        var sliderTag = _uiBuilder.Slider.Instantiate()
            .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetPreferredWidth(260)
            .SetMinValue(GizmoAssets.MinSize)
            .SetMaxValue(GizmoAssets.MaxSize);
        var textTag = _uiBuilder.Text.Instantiate()
            .SetFontSize(16);

        var mainContainer = verticalTag
            .SetSpacing(4)
            .Create(target);
        mainContainer.name = "EditorEnhancedView";
        var container = stackTag.Create(mainContainer.transform);
        Object.Instantiate(
            _ebvc._editBeatmapRightPanelView._editObjectView._noteDataView.transform.Find("Background4px"),
            container.transform, false);

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
        _globalScaleSlider = sliderTag
            .SetValue(_config.Gizmo.GlobalScale)
            .SetOnValueChange(HandleGizmoGlobalScale)
            .Create(layout.transform);
        _globalScaleInput = inputFloatTag
            .SetValue(_config.Gizmo.GlobalScale)
            .SetOnValueChange(HandleGizmoGlobalScale)
            .Create(layout.transform);
        layout = horizontalTag.Create(container.transform);
        textTag
            .SetText("Base")
            .Create(layout.transform);
        _sizeBaseSlider = sliderTag
            .SetValue(_config.Gizmo.SizeBase)
            .SetOnValueChange(HandleGizmoSizeBase)
            .Create(layout.transform);
        _sizeBaseInput = inputFloatTag
            .SetValue(_config.Gizmo.SizeBase)
            .SetOnValueChange(HandleGizmoSizeBase)
            .Create(layout.transform);
        layout = horizontalTag.Create(container.transform);
        textTag
            .SetText("Rotation")
            .Create(layout.transform);
        _sizeRotationSlider = sliderTag
            .SetValue(_config.Gizmo.SizeRotation)
            .SetOnValueChange(HandleGizmoSizeRotation)
            .Create(layout.transform);
        _sizeRotationInput = inputFloatTag
            .SetValue(_config.Gizmo.SizeRotation)
            .SetOnValueChange(HandleGizmoSizeRotation)
            .Create(layout.transform);
        layout = horizontalTag.Create(container.transform);
        textTag
            .SetText("Translation")
            .Create(layout.transform);
        _sizeTranslationSlider = sliderTag
            .SetValue(_config.Gizmo.SizeTranslation)
            .SetOnValueChange(HandleGizmoSizeTranslation)
            .Create(layout.transform);
        _sizeTranslationInput = inputFloatTag
            .SetValue(_config.Gizmo.SizeTranslation)
            .SetOnValueChange(HandleGizmoSizeTranslation)
            .Create(layout.transform);

        // layout = horizontalTag.Create(container.transform);
        // textTag
        //     .SetText("COLOR")
        //     .SetFontSize(20f)
        //     .SetFontWeight(FontWeight.Bold)
        //     .Create(container.transform);
        // layout = horizontalTag.Create(container.transform);
        // textTag
        //     .SetText("Base")
        //     .SetFontSize(16f)
        //     .SetFontWeight(FontWeight.Regular)
        //     .Create(layout.transform);
        // layout = horizontalTag.Create(container.transform);
        // textTag
        //     .SetText("Highlight")
        //     .Create(layout.transform);

        container = stackTag.Create(mainContainer.transform);
        Object.Instantiate(
            _ebvc._editBeatmapRightPanelView._editObjectView._noteDataView.transform.Find("Background4px"),
            container.transform, false);
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
        inputFloatTag
            .SetValidatorType(FloatInputFieldValidator.ValidatorType.None);
        foreach (var precisionsKey in LightColorEventHelper._precisions.Keys)
            inputFloatTag
                .SetValue(LightColorEventHelper._precisions[precisionsKey])
                .SetOnValueChange(val =>
                {
                    LightColorEventHelper._precisions[precisionsKey] = val;
                    _config.Precision.Color[(int)precisionsKey] = val;
                })
                .Create(layout.transform);

        layout = horizontalTag
            .Create(container.transform);
        textTag
            .SetText("Rotation")
            .Create(layout.transform);
        foreach (var precisionsKey in ModifyHoveredLightRotationDeltaRotationCommand._precisions.Keys)
            inputFloatTag
                .SetValue(ModifyHoveredLightRotationDeltaRotationCommand._precisions[precisionsKey])
                .SetOnValueChange(val =>
                {
                    ModifyHoveredLightRotationDeltaRotationCommand._precisions[precisionsKey] = val;
                    _config.Precision.Rotation[(int)precisionsKey] = val;
                })
                .Create(layout.transform);

        layout = horizontalTag
            .Create(container.transform);
        textTag
            .SetText("Translation")
            .Create(layout.transform);
        foreach (var precisionsKey in ModifyHoveredLightTranslationDeltaTranslationCommand._precisions.Keys)
            inputFloatTag
                .SetValue(ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[precisionsKey])
                .SetOnValueChange(val =>
                {
                    ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[precisionsKey] = val;
                    _config.Precision.Translation[(int)precisionsKey] = val;
                })
                .Create(layout.transform);

        layout = horizontalTag
            .Create(container.transform);
        textTag
            .SetText("FX")
            .Create(layout.transform);
        foreach (var precisionsKey in ModifyHoveredFloatFxDeltaValueCommand._precisions.Keys)
            inputFloatTag
                .SetValue(ModifyHoveredFloatFxDeltaValueCommand._precisions[precisionsKey])
                .SetOnValueChange(val =>
                {
                    ModifyHoveredFloatFxDeltaValueCommand._precisions[precisionsKey] = val;
                    _config.Precision.Fx[(int)precisionsKey] = val;
                })
                .Create(layout.transform);

        var configPanel = new EditBeatmapRightPanelView.PanelElement
        {
            name = "Editor Enhanced",
            panelType = (BeatmapPanelType)(Enum.GetValues(typeof(BeatmapPanelType)).Length + 1),
            elements = [mainContainer]
        };
        _ebvc._editBeatmapRightPanelView._panels = _ebvc._editBeatmapRightPanelView._panels.AddToArray(configPanel);
        _ebvc._editBeatmapRightPanelView._dropdown.SetTexts(_ebvc._editBeatmapRightPanelView._panels.Select(p => p.name)
            .ToArray());
        _ebvc._editBeatmapRightPanelView._dropdown._numberOfVisibleCell =
            _ebvc._editBeatmapRightPanelView._panels.Length;
    }

    private void HandleGizmoEnable(bool value)
    {
        _config.Gizmo.Enabled = value;
        _signalBus.Fire<GizmoRefreshSignal>();
    }

    private void HandleGizmoDraggable(bool value)
    {
        _config.Gizmo.Draggable = value;
        _signalBus.Fire<GizmoConfigDraggableUpdateSignal>();
    }

    private void HandleGizmoSwappable(bool value)
    {
        _config.Gizmo.Swappable = value;
        _signalBus.Fire<GizmoConfigSwappableUpdateSignal>();
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

    private void HandleGizmoHighlight(bool value)
    {
        _config.Gizmo.Highlight = value;
        _signalBus.Fire<GizmoConfigHighlightUpdateSignal>();
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
        _config.Gizmo.GlobalScale =
            Mathf.Clamp(Mathf.Round(value * 100f) / 100f, GizmoAssets.MinSize, GizmoAssets.MaxSize);
        _globalScaleSlider.GetComponent<Slider>().SetValueWithoutNotify(_config.Gizmo.GlobalScale);
        _globalScaleInput.GetComponent<FloatInputFieldValidator>()
            .SetValueWithoutNotify(_config.Gizmo.GlobalScale, true);
        _signalBus.Fire<GizmoConfigGlobalScaleUpdateSignal>();
    }

    private void HandleGizmoSizeBase(float value)
    {
        _config.Gizmo.SizeBase =
            Mathf.Clamp(Mathf.Round(value * 100f) / 100f, GizmoAssets.MinSize, GizmoAssets.MaxSize);
        _sizeBaseSlider.GetComponent<Slider>().SetValueWithoutNotify(_config.Gizmo.SizeBase);
        _sizeBaseInput.GetComponent<FloatInputFieldValidator>()
            .SetValueWithoutNotify(_config.Gizmo.SizeBase, true);
        _signalBus.Fire<GizmoConfigSizeBaseUpdateSignal>();
    }

    private void HandleGizmoSizeRotation(float value)
    {
        _config.Gizmo.SizeRotation =
            Mathf.Clamp(Mathf.Round(value * 100f) / 100f, GizmoAssets.MinSize, GizmoAssets.MaxSize);
        _sizeRotationSlider.GetComponent<Slider>().SetValueWithoutNotify(_config.Gizmo.SizeRotation);
        _sizeRotationInput.GetComponent<FloatInputFieldValidator>()
            .SetValueWithoutNotify(_config.Gizmo.SizeRotation, true);
        _signalBus.Fire<GizmoConfigSizeRotationUpdateSignal>();
    }

    private void HandleGizmoSizeTranslation(float value)
    {
        _config.Gizmo.SizeTranslation =
            Mathf.Clamp(Mathf.Round(value * 100f) / 100f, GizmoAssets.MinSize, GizmoAssets.MaxSize);
        _sizeTranslationSlider.GetComponent<Slider>().SetValueWithoutNotify(_config.Gizmo.SizeTranslation);
        _sizeTranslationInput.GetComponent<FloatInputFieldValidator>()
            .SetValueWithoutNotify(_config.Gizmo.SizeTranslation, true);
        _signalBus.Fire<GizmoConfigSizeTranslationUpdateSignal>();
    }
}