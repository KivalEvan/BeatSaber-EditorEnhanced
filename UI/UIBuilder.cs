using EditorEnhanced.UI.Tags;
using Tweening;
using Zenject;

namespace EditorEnhanced.UI;

internal sealed class UIBuilder
{
   private readonly DiContainer _container;
   private readonly TimeTweeningManager _tweeningManager;
   private readonly EditorViewLocator _viewLocator;

   public UIBuilder(
      DiContainer container,
      EditorViewLocator viewLocator,
      TimeTweeningManager tweeningManager)
   {
      _container = container;
      _viewLocator = viewLocator;
      _tweeningManager = tweeningManager;
   }

   public EditorButtonTag CreateButton() => new(_viewLocator.GetButtonPrefab(), _tweeningManager);
   public EditorButtonWithIconTag CreateButtonWithIcon() => new(_viewLocator.GetButtonPrefab(), _tweeningManager);
   public EditorCheckboxTag CreateCheckbox() => new(_viewLocator.GetTogglePrefab(), _tweeningManager);
   public EditorInputFloatTag CreateFloatInput() => new(_viewLocator.GetInputPrefab(), _container);
   public EditorInputIntTag CreateIntInput() => new(_viewLocator.GetInputPrefab(), _container);
   public EditorInputStringTag CreateStringInput() => new(_viewLocator.GetInputPrefab());
   public EditorLayoutHorizontalTag CreateHorizontalLayout() => new();
   public EditorLayoutStackTag CreateStackLayout() => new();
   public EditorLayoutVerticalTag CreateVerticalLayout() => new();
   public EditorSliderTag CreateSlider() => new(_viewLocator.GetSliderPrefab());
   public EditorTextTag CreateText() => new(_viewLocator.GetTextPrefab());
}
