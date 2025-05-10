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

    public static T SetTextAlignment<T>(this T self, TextAlignmentOptions alignment) where T : IUIText
    {
        self.TextAlignment = alignment;
        return self;
    }

    public static T SetRichText<T>(this T self, bool richText) where T : IUIText
    {
        self.RichText = richText;
        return self;
    }

    public static T SetFontSize<T>(this T self, float size) where T : IUIText
    {
        self.FontSize = size;
        return self;
    }

    public static T SetFontWeight<T>(this T self, FontWeight weight) where T : IUIText
    {
        self.FontWeight = weight;
        return self;
    }
}