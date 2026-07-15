using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Tags;

public class EditorSliderTag : IEditorTag, IUISlider, IUILayoutElement
{
   private readonly GameObject _prefabSlider;

   public EditorSliderTag(GameObject prefabSlider)
   {
      _prefabSlider = prefabSlider;
   }

   public string Name { get; set; } = "EEEditorSlider";

   public GameObject Create(Transform parent)
   {
      var go = Object.Instantiate(_prefabSlider, parent, false);
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

      go.SetActive(true);
      return go;
   }

   public ContentSizeFitter.FitMode? VerticalFit { get; set; }
   public ContentSizeFitter.FitMode? HorizontalFit { get; set; }
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
