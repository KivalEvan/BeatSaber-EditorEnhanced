using EditorEnhanced.UI.Interfaces;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Tags;

public class EditorLayoutVerticalBuilder : IEditorBuilder<EditorLayoutVerticalTag>
{
   public EditorLayoutVerticalTag Instantiate()
   {
      return new EditorLayoutVerticalTag();
   }
}

public class EditorLayoutVerticalTag : IEditorTag, IUIRect, IUILayout
{
   public string Name { get; set; } = "EEVerticalLayoutGroup";

   public GameObject Create(Transform parent)
   {
      var go = new GameObject(Name)
      {
         layer = 5
      };
      go.transform.SetParent(parent, false);

      var le = go.AddComponent<LayoutElement>();
      le.flexibleWidth = FlexibleWidth ?? le.flexibleWidth;
      le.flexibleHeight = FlexibleHeight ?? le.flexibleHeight;
      le.preferredWidth = PreferredWidth ?? le.preferredWidth;
      le.preferredHeight = PreferredHeight ?? le.preferredHeight;

      var vlg = go.AddComponent<VerticalLayoutGroup>();
      vlg.spacing = Spacing ?? vlg.spacing;
      vlg.padding = Padding ?? vlg.padding;
      vlg.childAlignment = ChildAlignment ?? vlg.childAlignment;
      vlg.childControlWidth = ChildControlWidth ?? vlg.childControlWidth;
      vlg.childControlHeight = ChildControlHeight ?? vlg.childControlHeight;
      vlg.childScaleWidth = ChildScaleWidth ?? vlg.childScaleWidth;
      vlg.childScaleHeight = ChildScaleHeight ?? vlg.childScaleHeight;
      vlg.childForceExpandWidth = ChildForceExpandWidth ?? vlg.childForceExpandWidth;
      vlg.childForceExpandHeight = ChildForceExpandHeight ?? vlg.childForceExpandHeight;

      var csf = go.AddComponent<ContentSizeFitter>();
      csf.horizontalFit = HorizontalFit ?? ContentSizeFitter.FitMode.PreferredSize;
      csf.verticalFit = VerticalFit ?? ContentSizeFitter.FitMode.PreferredSize;

      var transform = go.transform as RectTransform;
      transform.anchorMin = AnchorMin ?? Vector2.zero;
      transform.anchorMax = AnchorMax ?? Vector2.one;
      transform.offsetMin = OffsetMin ?? transform.offsetMin;
      transform.offsetMax = OffsetMax ?? transform.offsetMax;
      transform.sizeDelta = SizeDelta ?? Vector2.zero;

      return go;
   }

   public float? Spacing { get; set; }
   [CanBeNull] public RectOffset Padding { get; set; }
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
   public Vector2? AnchorMin { get; set; }
   public Vector2? AnchorMax { get; set; }
   public Vector2? OffsetMin { get; set; }
   public Vector2? OffsetMax { get; set; }
   public Vector2? SizeDelta { get; set; }
   public Rect? Rect { get; set; }
}