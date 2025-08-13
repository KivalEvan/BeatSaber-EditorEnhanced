using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Tags;

public abstract class EditorInputTag<T> : IEditorTag, IUILayout
{
   protected EditBeatmapViewController _ebvc;

   private GameObject PrefabInput =>
      _ebvc._editBeatmapRightPanelView._editObjectView._noteDataView
         ._beatInputFieldValidator.gameObject;

   public abstract string Name { get; set; }

   public virtual GameObject Create(Transform parent)
   {
      var go = Object.Instantiate(PrefabInput, parent, false);
      go.name = Name;
      go.SetActive(false);

      Object.Destroy(go.GetComponent<FloatInputFieldValidator>());
      Object.Destroy(go.GetComponent<FloatInputFieldValidatorChangeOnScroll>());

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
}