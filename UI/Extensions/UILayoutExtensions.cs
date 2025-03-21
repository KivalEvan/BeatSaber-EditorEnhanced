using EditorEnhanced.UI.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Extensions;

public static class UILayoutExtensions
{
    public static T SetSpacing<T>(this T self, float spacing) where T : IUILayout
    {
        self.Spacing = spacing;
        return self;
    }

    public static T SetPadding<T>(this T self, RectOffset padding) where T : IUILayout
    {
        self.Padding = padding;
        return self;
    }

    public static T SetVerticalFit<T>(this T self, ContentSizeFitter.FitMode fitMode) where T : IUILayout
    {
        self.VerticalFit = fitMode;
        return self;
    }

    public static T SetHorizontalFit<T>(this T self, ContentSizeFitter.FitMode fitMode) where T : IUILayout
    {
        self.HorizontalFit = fitMode;
        return self;
    }

    public static T SetChildAlignment<T>(this T self, TextAnchor anchor) where T : IUILayout
    {
        self.ChildAlignment = anchor;
        return self;
    }

    public static T SetChildControlWidth<T>(this T self, bool toggle) where T : IUILayout
    {
        self.ChildControlWidth = toggle;
        return self;
    }

    public static T SetChildControlHeight<T>(this T self, bool toggle) where T : IUILayout
    {
        self.ChildControlHeight = toggle;
        return self;
    }

    public static T SetChildScaleWidth<T>(this T self, bool toggle) where T : IUILayout
    {
        self.ChildScaleWidth = toggle;
        return self;
    }

    public static T SetChildScaleHeight<T>(this T self, bool toggle) where T : IUILayout
    {
        self.ChildScaleHeight = toggle;
        return self;
    }

    public static T SetChildForceExpandWidth<T>(this T self, bool toggle) where T : IUILayout
    {
        self.ChildForceExpandWidth = toggle;
        return self;
    }

    public static T SetChildForceExpandHeight<T>(this T self, bool toggle) where T : IUILayout
    {
        self.ChildForceExpandHeight = toggle;
        return self;
    }

    public static T SetFlexibleWidth<T>(this T self, float flexibleWidth) where T : IUILayout
    {
        self.FlexibleWidth = flexibleWidth;
        return self;
    }

    public static T SetFlexibleHeight<T>(this T self, float flexibleHeight) where T : IUILayout
    {
        self.FlexibleHeight = flexibleHeight;
        return self;
    }
}