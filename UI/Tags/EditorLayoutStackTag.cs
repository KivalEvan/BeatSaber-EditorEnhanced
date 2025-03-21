using EditorEnhanced.UI.Interfaces;
using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Tags;

public class EditorLayoutStackTag : IUILayout
{
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
        transform.anchorMin = new Vector2(0.0f, 0.0f);
        transform.anchorMax = new Vector2(1f, 1f);
        transform.sizeDelta = new Vector2(0.0f, 0.0f);
        
        var layout = go.AddComponent<LayoutElement>();
        layout.flexibleWidth = FlexibleWidth ?? layout.flexibleWidth;
        layout.flexibleHeight = FlexibleHeight ?? layout.flexibleHeight;

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
}