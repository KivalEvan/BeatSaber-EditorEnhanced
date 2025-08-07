using System;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.Configurations;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class ConfigurationViewController : IInitializable
{
    private readonly SignalBus _signalBus;
    private readonly PluginConfig _config;
    private readonly EditBeatmapViewController _ebvc;
    private readonly EditorButtonBuilder _editorBtn;
    private readonly EditorCheckboxBuilder _editorCheckbox;
    private readonly EditorLayoutHorizontalBuilder _editorLayoutHorizontal;
    private readonly EditorLayoutStackBuilder _editorLayoutStack;
    private readonly EditorLayoutVerticalBuilder _editorLayoutVertical;
    private readonly EditorTextBuilder _editorText;

    public ConfigurationViewController(SignalBus signalBus,
        PluginConfig config,
        EditBeatmapViewController ebvc,
        EditorLayoutStackBuilder editorLayoutStack,
        EditorLayoutVerticalBuilder editorLayoutVertical,
        EditorLayoutHorizontalBuilder editorLayoutHorizontal,
        EditorButtonBuilder editorBtn,
        EditorCheckboxBuilder editorCheckbox,
        EditorTextBuilder editorText)
    {
        _signalBus = signalBus;
        _config = config;
        _ebvc = ebvc;
        _editorLayoutStack = editorLayoutStack;
        _editorLayoutVertical = editorLayoutVertical;
        _editorLayoutHorizontal = editorLayoutHorizontal;
        _editorBtn = editorBtn;
        _editorCheckbox = editorCheckbox;
        _editorText = editorText;
    }

    public void Initialize()
    {
        var target = _ebvc._editBeatmapRightPanelView._scrollView.contentTransform as RectTransform;

        var stackTag = _editorLayoutStack.CreateNew()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var verticalTag = _editorLayoutVertical.CreateNew()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetPadding(new RectOffset(4, 4, 4, 4));
        var horizontalTag = _editorLayoutHorizontal.CreateNew()
            .SetChildAlignment(TextAnchor.LowerCenter)
            .SetChildControlWidth(true)
            .SetSpacing(8)
            .SetPadding(new RectOffset(4, 4, 2, 2));
        var btnTag = _editorBtn.CreateNew()
            .SetFontSize(16);
        var checkboxTag = _editorCheckbox.CreateNew()
            .SetSize(28)
            .SetFontSize(16);
        var textTag = _editorText.CreateNew()
            .SetFontSize(16);

        var container = verticalTag.CreateObject(target);
        var layout = horizontalTag.CreateObject(container.transform);
        checkboxTag
            .SetText("Gizmo")
            .SetBool(_config.GizmoEnabled)
            .SetOnValueChange(HandleGizmoEnable)
            .CreateObject(layout.transform);

        var configPanel = new EditBeatmapRightPanelView.PanelElement
        {
            name = "Editor Enhanced",
            panelType = (BeatmapPanelType)(Enum.GetValues(typeof(BeatmapPanelType)).Length + 1),
            elements = [container]
        };
        _ebvc._editBeatmapRightPanelView._panels = _ebvc._editBeatmapRightPanelView._panels.AddToArray(configPanel);
        _ebvc._editBeatmapRightPanelView._dropdown.SetTexts(_ebvc._editBeatmapRightPanelView._panels.Select(p => p.name)
            .ToArray());
        _ebvc._editBeatmapRightPanelView._dropdown._numberOfVisibleCell = _ebvc._editBeatmapRightPanelView._panels.Length;
    }

    private void HandleGizmoEnable(bool toggle)
    {
        _config.GizmoEnabled = toggle;
        _signalBus.Fire<GizmoUpdateSignal>();
    }
}