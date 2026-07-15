using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.UI.Tags;
using Tweening;
using Zenject;

namespace EditorEnhanced.UI;

public class UIBuilder
{
   private readonly DiContainer _container;
   private readonly EditBeatmapNavigationViewController _ebnvc;
   private readonly EditBeatmapViewController _ebvc;
   private readonly TimeTweeningManager _tweeningManager;

   public UIBuilder(
      DiContainer container,
      EditBeatmapNavigationViewController ebnvc,
      EditBeatmapViewController ebvc,
      TimeTweeningManager tweeningManager)
   {
      _container = container;
      _ebnvc = ebnvc;
      _ebvc = ebvc;
      _tweeningManager = tweeningManager;
   }

   public EditorButtonTag CreateButton() => new(_ebvc, _tweeningManager);
   public EditorButtonWithIconTag CreateButtonWithIcon() => new(_ebvc, _tweeningManager);
   public EditorCheckboxTag CreateCheckbox() => new(_ebnvc, _tweeningManager);
   public EditorInputFloatTag CreateFloatInput() => new(_ebvc, _container);
   public EditorInputIntTag CreateIntInput() => new(_ebvc, _container);
   public EditorInputStringTag CreateStringInput() => new(_ebvc);
   public EditorLayoutHorizontalTag CreateHorizontalLayout() => new();
   public EditorLayoutStackTag CreateStackLayout() => new();
   public EditorLayoutVerticalTag CreateVerticalLayout() => new();
   public EditorSliderTag CreateSlider() => new(_ebvc);
   public EditorTextTag CreateText() => new(_ebvc);
}
