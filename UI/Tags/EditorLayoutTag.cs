using EditorEnhanced.UI.Interfaces;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Tags;

public abstract class EditorLayoutTag : IEditorTag, IUIRect, IUILayout
{
   public virtual string Name { get; set; } = "EditorLayoutGroup";

   public virtual GameObject Create(Transform parent)
   {
      var go = new GameObject(Name) { layer = 5 };
      go.transform.SetParent(parent, false);

      var le = go.AddComponent<LayoutElement>();
      le.flexibleWidth = FlexibleWidth ?? le.flexibleWidth;
      le.flexibleHeight = FlexibleHeight ?? le.flexibleHeight;
      le.preferredWidth = PreferredWidth ?? le.preferredWidth;
      le.preferredHeight = PreferredHeight ?? le.preferredHeight;

      CreateAndConfigureLayoutGroup(go);

      var csf = go.AddComponent<ContentSizeFitter>();
      ConfigureContentSizeFitter(csf);

      var transform = (RectTransform)go.transform;
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

   protected abstract void CreateAndConfigureLayoutGroup(GameObject go);

   protected virtual void ConfigureContentSizeFitter(ContentSizeFitter csf)
   {
      csf.horizontalFit = HorizontalFit ?? ContentSizeFitter.FitMode.PreferredSize;
      csf.verticalFit = VerticalFit ?? ContentSizeFitter.FitMode.PreferredSize;
   }
}
