using System;
using EditorEnhanced.UI.Interfaces;

namespace EditorEnhanced.UI.Extensions;

public static class UISliderExtensions
{
   public static T SetValue<T>(this T self, float value) where T : IUISlider
   {
      self.Value = value;
      return self;
   }

   public static T SetMinValue<T>(this T self, float value) where T : IUISlider
   {
      self.MinValue = value;
      return self;
   }

   public static T SetMaxValue<T>(this T self, float value) where T : IUISlider
   {
      self.MaxValue = value;
      return self;
   }

   public static T SetWholeNumber<T>(this T self, bool value) where T : IUISlider
   {
      self.WholeNumber = value;
      return self;
   }

   public static T ResetOnValueChange<T>(this T self) where T : IUISlider
   {
      self.OnValueChange.Clear();
      return self;
   }

   public static T AddOnValueChange<T>(this T self, Action<float> fn) where T : IUISlider
   {
      self.OnValueChange.Add(fn);
      return self;
   }

   public static T SetOnValueChange<T>(this T self, Action<float> fn) where T : IUISlider
   {
      return self
         .ResetOnValueChange()
         .AddOnValueChange(fn);
   }
}