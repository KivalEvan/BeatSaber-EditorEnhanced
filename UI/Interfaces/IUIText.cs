using JetBrains.Annotations;
using TMPro;

namespace EditorEnhanced.UI.Interfaces;

public interface IUIText
{
    [CanBeNull] string Text { get; set; }
    TextAlignmentOptions? TextAlignment { get; set; }
    bool? RichText { get; set; }
    float? FontSize { get; set; }
    FontWeight? FontWeight { get; set; }
}