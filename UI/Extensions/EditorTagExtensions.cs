using EditorEnhanced.UI.Interfaces;

namespace EditorEnhanced.UI.Extensions;

public static class EditorTagExtensions
{
   public static T SetName<T>(this T self, string value) where T : IEditorTag
   {
      self.Name = value;
      return self;
   }
}