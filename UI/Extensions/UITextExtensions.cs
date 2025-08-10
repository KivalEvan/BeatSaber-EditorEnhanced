using EditorEnhanced.UI.Interfaces;
using TMPro;

namespace EditorEnhanced.UI.Extensions;

public static class UITextExtensions
{
    public static T SetText<T>(this T self, string text) where T : IUIText
    {
        self.Text = text;
        return self;
    }

    public static T SetTextAlignment<T>(this T self, TextAlignmentOptions value) where T : IUIText
    {
        self.TextAlignment = value;
        return self;
    }

    public static T SetRichText<T>(this T self, bool value) where T : IUIText
    {
        self.RichText = value;
        return self;
    }

    public static T SetFontSize<T>(this T self, float value) where T : IUIText
    {
        self.FontSize = value;
        return self;
    }

    public static T SetFontWeight<T>(this T self, FontWeight value) where T : IUIText
    {
        self.FontWeight = value;
        return self;
    }
}