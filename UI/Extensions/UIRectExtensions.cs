using EditorEnhanced.UI.Interfaces;
using UnityEngine;

namespace EditorEnhanced.UI.Extensions;

public static class UIRectExtensions
{
    public static T SetAnchorMin<T>(this T self, Vector2 anchorMin) where T : IUIRect
    {
        self.AnchorMin = anchorMin;
        return self;
    }

    public static T SetAnchorMax<T>(this T self, Vector2 anchorMax) where T : IUIRect
    {
        self.AnchorMax = anchorMax;
        return self;
    }

    public static T SetOffsetMin<T>(this T self, Vector2 offsetMin) where T : IUIRect
    {
        self.OffsetMin = offsetMin;
        return self;
    }

    public static T SetOffsetMax<T>(this T self, Vector2 offsetMax) where T : IUIRect
    {
        self.OffsetMax = offsetMax;
        return self;
    }

    public static T SetSizeDelta<T>(this T self, Vector2 sizeDelta) where T : IUIRect
    {
        self.SizeDelta = sizeDelta;
        return self;
    }

    public static T SetRect<T>(this T self, Rect rect) where T : IUIRect
    {
        self.Rect = rect;
        return self;
    }
}