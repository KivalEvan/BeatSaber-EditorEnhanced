using EditorEnhanced.UI.Interfaces;
using UnityEngine;

namespace EditorEnhanced.UI.Extensions;

public static class UIRectExtensions
{
   public static T SetAnchorMin<T>(this T self, Vector2 value) where T : IUIRect
   {
      self.AnchorMin = value;
      return self;
   }

   public static T SetAnchorMax<T>(this T self, Vector2 value) where T : IUIRect
   {
      self.AnchorMax = value;
      return self;
   }

   public static T SetOffsetMin<T>(this T self, Vector2 value) where T : IUIRect
   {
      self.OffsetMin = value;
      return self;
   }

   public static T SetOffsetMax<T>(this T self, Vector2 value) where T : IUIRect
   {
      self.OffsetMax = value;
      return self;
   }

   public static T SetSizeDelta<T>(this T self, Vector2 value) where T : IUIRect
   {
      self.SizeDelta = value;
      return self;
   }

   public static T SetRect<T>(this T self, Rect value) where T : IUIRect
   {
      self.Rect = value;
      return self;
   }
}