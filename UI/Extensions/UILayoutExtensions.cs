using EditorEnhanced.UI.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Extensions;

public static class UILayoutExtensions
{
   public static T SetSpacing<T>(this T self, float value) where T : IUILayout
   {
      self.Spacing = value;
      return self;
   }

   public static T SetPadding<T>(this T self, RectOffset value) where T : IUILayout
   {
      self.Padding = value;
      return self;
   }

   public static T SetVerticalFit<T>(this T self, ContentSizeFitter.FitMode value) where T : IUILayout
   {
      self.VerticalFit = value;
      return self;
   }

   public static T SetHorizontalFit<T>(this T self, ContentSizeFitter.FitMode value) where T : IUILayout
   {
      self.HorizontalFit = value;
      return self;
   }

   public static T SetChildAlignment<T>(this T self, TextAnchor value) where T : IUILayout
   {
      self.ChildAlignment = value;
      return self;
   }

   public static T SetChildControlWidth<T>(this T self, bool value) where T : IUILayout
   {
      self.ChildControlWidth = value;
      return self;
   }

   public static T SetChildControlHeight<T>(this T self, bool value) where T : IUILayout
   {
      self.ChildControlHeight = value;
      return self;
   }

   public static T SetChildScaleWidth<T>(this T self, bool value) where T : IUILayout
   {
      self.ChildScaleWidth = value;
      return self;
   }

   public static T SetChildScaleHeight<T>(this T self, bool value) where T : IUILayout
   {
      self.ChildScaleHeight = value;
      return self;
   }

   public static T SetChildForceExpandWidth<T>(this T self, bool value) where T : IUILayout
   {
      self.ChildForceExpandWidth = value;
      return self;
   }

   public static T SetChildForceExpandHeight<T>(this T self, bool value) where T : IUILayout
   {
      self.ChildForceExpandHeight = value;
      return self;
   }

   public static T SetFlexibleWidth<T>(this T self, float value) where T : IUILayout
   {
      self.FlexibleWidth = value;
      return self;
   }

   public static T SetFlexibleHeight<T>(this T self, float value) where T : IUILayout
   {
      self.FlexibleHeight = value;
      return self;
   }

   public static T SetPreferredWidth<T>(this T self, float value) where T : IUILayout
   {
      self.PreferredWidth = value;
      return self;
   }

   public static T SetPreferredHeight<T>(this T self, float value) where T : IUILayout
   {
      self.PreferredHeight = value;
      return self;
   }
}