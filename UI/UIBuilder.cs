using EditorEnhanced.UI.Tags;
using Zenject;

namespace EditorEnhanced.UI;

public class UIBuilder
{
   [Inject] public readonly EditorButtonBuilder Button;
   [Inject] public readonly EditorButtonWithIconBuilder ButtonWithIcon;
   [Inject] public readonly EditorCheckboxBuilder Checkbox;
   [Inject] public readonly EditorInputFloatBuilder InputFloat;
   [Inject] public readonly EditorInputIntBuilder InputInt;
   [Inject] public readonly EditorInputStringBuilder InputString;
   [Inject] public readonly EditorLayoutHorizontalBuilder LayoutHorizontal;
   [Inject] public readonly EditorLayoutStackBuilder LayoutStack;
   [Inject] public readonly EditorLayoutVerticalBuilder LayoutVertical;
   [Inject] public readonly EditorSliderBuilder Slider;
   [Inject] public readonly EditorTextBuilder Text;
   [Inject] public readonly EditorToggleButtonBuilder ToggleButton;
   [Inject] public readonly EditorToggleGroupBuilder ToggleGroup;
}