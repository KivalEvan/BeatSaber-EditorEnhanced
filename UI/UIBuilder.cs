using EditorEnhanced.UI.Tags;
using Tweening;
using Zenject;

namespace EditorEnhanced.UI;

public sealed class UIBuilder
{
   private readonly UIButtonAudioFeedback _buttonAudioFeedback = new();
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

   public EditorButtonTag CreateButton()
   {
      return new EditorButtonTag(_viewLocator.GetButtonPrefab(), _tweeningManager, _buttonAudioFeedback);
   }

   public EditorButtonWithIconTag CreateButtonWithIcon()
   {
      return new EditorButtonWithIconTag(_viewLocator.GetButtonPrefab(), _tweeningManager, _buttonAudioFeedback);
   }

   public EditorCheckboxTag CreateCheckbox()
   {
      return new EditorCheckboxTag(_viewLocator.GetTogglePrefab(), _tweeningManager);
   }

   public EditorInputFloatTag CreateFloatInput()
   {
      return new EditorInputFloatTag(_viewLocator.GetInputPrefab(), _container);
   }

   public EditorInputIntTag CreateIntInput()
   {
      return new EditorInputIntTag(_viewLocator.GetInputPrefab(), _container);
   }

   public EditorInputStringTag CreateStringInput()
   {
      return new EditorInputStringTag(_viewLocator.GetInputPrefab());
   }

   public EditorLayoutHorizontalTag CreateHorizontalLayout()
   {
      return new EditorLayoutHorizontalTag();
   }

   public EditorLayoutStackTag CreateStackLayout()
   {
      return new EditorLayoutStackTag();
   }

   public EditorLayoutVerticalTag CreateVerticalLayout()
   {
      return new EditorLayoutVerticalTag();
   }

   public EditorSliderTag CreateSlider()
   {
      return new EditorSliderTag(_viewLocator.GetSliderPrefab());
   }

   public EditorTextTag CreateText()
   {
      return new EditorTextTag(_viewLocator.GetTextPrefab());
   }
}
