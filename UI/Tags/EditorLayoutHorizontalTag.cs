using EditorEnhanced.UI.Interfaces;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Tags;

public class EditorLayoutHorizontalBuilder
{
    public EditorLayoutHorizontalTag CreateNew()
    {
        return new EditorLayoutHorizontalTag();
    }
}

public class EditorLayoutHorizontalTag : IUIRect, IUILayout
{
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
    public Vector2? AnchorMin { get; set; }
    public Vector2? AnchorMax { get; set; }
    public Vector2? OffsetMin { get; set; }
    public Vector2? OffsetMax { get; set; }
    public Vector2? SizeDelta { get; set; }
    public Rect? Rect { get; set; }

    public GameObject CreateObject(Transform parent)
    {
        var go = new GameObject("EEHorizontalLayoutGroup")
        {
            layer = 5
        };
        go.transform.SetParent(parent, false);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = Spacing ?? hlg.spacing;
        hlg.padding = Padding ?? hlg.padding;
        hlg.childAlignment = ChildAlignment ?? hlg.childAlignment;
        hlg.childControlWidth = ChildControlWidth ?? hlg.childControlWidth;
        hlg.childControlHeight = ChildControlHeight ?? hlg.childControlHeight;
        hlg.childScaleWidth = ChildScaleWidth ?? hlg.childScaleWidth;
        hlg.childScaleHeight = ChildScaleHeight ?? hlg.childScaleHeight;
        hlg.childForceExpandWidth = ChildForceExpandWidth ?? hlg.childForceExpandWidth;
        hlg.childForceExpandHeight = ChildForceExpandHeight ?? hlg.childForceExpandHeight;

        var csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var transform = go.transform as RectTransform;
        transform.anchorMin = AnchorMin ?? Vector2.zero;
        transform.anchorMax = AnchorMax ?? Vector2.one;
        transform.offsetMin = OffsetMin ?? transform.offsetMin;
        transform.offsetMax = OffsetMax ?? transform.offsetMax;
        transform.sizeDelta = SizeDelta ?? Vector2.zero;

        var layout = go.AddComponent<LayoutElement>();
        layout.flexibleWidth = FlexibleWidth ?? layout.flexibleWidth;
        layout.flexibleHeight = FlexibleHeight ?? layout.flexibleHeight;

        return go;
    }
}