using UnityEngine;

namespace EditorEnhanced.Utils;

public static class TextureLoader
{
   public static Texture2D LoadTextureRaw(byte[] file)
   {
      if (file.Length != 0)
      {
         var texture2D = new Texture2D(0, 0, TextureFormat.RGBA32, false, false);
         if (texture2D.LoadImage(file)) return texture2D;
      }

      return null;
   }

   public static Sprite LoadSpriteRaw(byte[] image, float pixelsPerUnit = 100f)
   {
      return LoadSpriteFromTexture(LoadTextureRaw(image), pixelsPerUnit);
   }

   public static Sprite LoadSpriteFromTexture(Texture2D spriteTexture, float pixelsPerUnit = 100f)
   {
      if (spriteTexture == null) return null;
      var sprite = Sprite.Create(
         spriteTexture,
         new Rect(0.0f, 0.0f, spriteTexture.width, spriteTexture.height),
         new Vector2(0.0f, 0.0f),
         pixelsPerUnit);
      sprite.name = spriteTexture.name;
      return sprite;
   }
}