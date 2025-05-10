using EditorEnhanced.UI.Interfaces;
using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Tags;

public class EditorLayoutStackBuilder
{
    public EditorLayoutStackTag CreateNew()
    {
        return new EditorLayoutStackTag();
    }
}

public class EditorLayoutStackTag : IUIRect, IUILayout
{
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
    public Vector2? AnchorMin { get; set; }
    public Vector2? AnchorMax { get; set; }
    public Vector2? OffsetMin { get; set; }
    public Vector2? OffsetMax { get; set; }
    public Vector2? SizeDelta { get; set; }
    public Rect? Rect { get; set; }

    public GameObject CreateObject(Transform parent)
    {
        var go = new GameObject("EEStackLayoutGroup")
        {
            layer = 5
        };
        go.transform.SetParent(parent, false);

        var slg = go.AddComponent<StackLayoutGroup>();
        slg.padding = Padding ?? slg.padding;
        slg.childAlignment = ChildAlignment ?? slg.childAlignment;
        slg.childForceExpandWidth = ChildForceExpandWidth ?? slg.childForceExpandWidth;
        slg.childForceExpandHeight = ChildForceExpandHeight ?? slg.childForceExpandHeight;

        var csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = HorizontalFit ?? csf.horizontalFit;
        csf.verticalFit = VerticalFit ?? ContentSizeFitter.FitMode.PreferredSize;

        var transform = go.transform as RectTransform;
        transform.anchorMin = AnchorMin ?? new Vector2(0.0f, 0.0f);
        transform.anchorMax = AnchorMax ?? new Vector2(1f, 1f);
        transform.offsetMin = OffsetMin ?? transform.offsetMin;
        transform.offsetMax = OffsetMax ?? transform.offsetMax;
        transform.sizeDelta = SizeDelta ?? new Vector2(0.0f, 0.0f);

        var layout = go.AddComponent<LayoutElement>();
        layout.flexibleWidth = FlexibleWidth ?? layout.flexibleWidth;
        layout.flexibleHeight = FlexibleHeight ?? layout.flexibleHeight;

        return go;
    }
}