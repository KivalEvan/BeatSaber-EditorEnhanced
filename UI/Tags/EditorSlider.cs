using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.UI.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Tags;

public class EditorSliderBuilder : IEditorBuilder<EditorSliderTag>
{
    private readonly EditBeatmapViewController _ebvc;

    public EditorSliderBuilder(EditBeatmapViewController ebvc)
    {
        _ebvc = ebvc;
    }

    public EditorSliderTag Instantiate()
    {
        return new EditorSliderTag(_ebvc);
    }
}

public class EditorSliderTag : IEditorTag, IUISlider, IUILayout
{
    private readonly EditBeatmapViewController _ebvc;

    public EditorSliderTag(EditBeatmapViewController ebvc)
    {
        _ebvc = ebvc;
    }

    private GameObject PrefabSlider => _ebvc.GetComponentInChildren<StatusBarView>()._musicVolumeSlider.gameObject;

    public string Name { get; set; } = "EEEditorSlider";

    public GameObject Create(Transform parent)
    {
        var go = Object.Instantiate(PrefabSlider, parent, false);
        go.name = Name;
        go.SetActive(false);

        Object.Destroy(go.GetComponent<SliderChangeOnScroll>());
        var slider = go.GetComponent<Slider>();
        slider.minValue = MinValue ?? slider.minValue;
        slider.maxValue = MaxValue ?? slider.maxValue;
        slider.wholeNumbers = WholeNumber ?? slider.wholeNumbers;
        slider.SetValueWithoutNotify(Value ?? slider.value);
        OnValueChange.ForEach(act => slider.onValueChanged.AddListener(f => act(f)));

        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = FlexibleWidth ?? le.flexibleWidth;
        le.flexibleHeight = FlexibleHeight ?? le.flexibleHeight;
        le.preferredWidth = PreferredWidth ?? le.preferredWidth;
        le.preferredHeight = PreferredHeight ?? le.preferredHeight;

        var csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = HorizontalFit ?? ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = VerticalFit ?? ContentSizeFitter.FitMode.PreferredSize;

        // var transform = go.transform as RectTransform;
        // transform.anchorMin = AnchorMin ?? Vector2.zero;
        // transform.anchorMax = AnchorMax ?? Vector2.one;
        // transform.offsetMin = OffsetMin ?? transform.offsetMin;
        // transform.offsetMax = OffsetMax ?? transform.offsetMax;
        // transform.sizeDelta = SizeDelta ?? Vector2.zero;

        go.SetActive(true);
        return go;
    }

    public float? Spacing { get; set; }
    public RectOffset Padding { get; set; }
    public ContentSizeFitter.FitMode? VerticalFit { get; set; }
    public ContentSizeFitter.FitMode? HorizontalFit { get; set; }
    public TextAnchor? ChildAlignment { get; set; }
    public bool? ChildControlWidth { get; set; }
    public bool? ChildControlHeight { get; set; }
    public bool? ChildScaleWidth { get; set; }
    public bool? ChildScaleHeight { get; set; }
    public bool? ChildForceExpandWidth { get; set; }
    public bool? ChildForceExpandHeight { get; set; }
    public float? FlexibleWidth { get; set; }
    public float? FlexibleHeight { get; set; }
    public float? PreferredWidth { get; set; }
    public float? PreferredHeight { get; set; }

    public float? Value { get; set; }
    public float? MinValue { get; set; }
    public float? MaxValue { get; set; }
    public bool? WholeNumber { get; set; }
    public List<Action<float>> OnValueChange { get; set; } = [];
}