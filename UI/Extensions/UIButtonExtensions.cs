using System;
using EditorEnhanced.UI.Interfaces;

namespace EditorEnhanced.UI.Extensions;

public static class UIButtonExtensions
{
    public static T ResetOnClick<T>(this T self, Action fn) where T : IUIButton
    {
        self.OnClick.Clear();
        return self;
    }

    public static T AddOnClick<T>(this T self, Action fn) where T : IUIButton
    {
        self.OnClick.Add(fn);
        return self;
    }

    public static T SetOnClick<T>(this T self, Action fn) where T : IUIButton
    {
        return self
            .ResetOnClick(fn)
            .AddOnClick(fn);
    }
}