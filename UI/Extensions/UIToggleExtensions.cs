using System;
using EditorEnhanced.UI.Interfaces;

namespace EditorEnhanced.UI.Extensions;

public static class UIToggleExtensions
{
    public static T ResetOnValueChange<T>(this T self) where T : IUIToggle
    {
        self.OnValueChange.Clear();
        return self;
    }

    public static T AddOnValueChange<T>(this T self, Action<bool> fn) where T : IUIToggle
    {
        self.OnValueChange.Add(fn);
        return self;
    }

    public static T SetOnValueChange<T>(this T self, Action<bool> fn) where T : IUIToggle
    {
        return self
            .ResetOnValueChange()
            .AddOnValueChange(fn);
    }

    public static T SetBool<T>(this T self, bool value) where T : IUIToggle
    {
        self.Bool = value;
        return self;
    }

    public static T SetSize<T>(this T self, float value) where T : IUIToggle
    {
        self.Size = value;
        return self;
    }
}