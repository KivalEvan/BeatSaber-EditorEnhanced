using UnityEngine;

namespace EditorEnhanced.Utils;

public static class TextureLoader
{
    public static Texture2D LoadTextureRaw(byte[] file)
    {
        if (file.Length != 0)
        {
            Texture2D texture2D = new Texture2D(0, 0, TextureFormat.RGBA32, false, false);
            if (ImageConversion.LoadImage(texture2D, file))
                return texture2D;
        }
        return (Texture2D) null;
    }

    public static Sprite LoadSpriteRaw(byte[] image, float pixelsPerUnit = 100f)
    {
        return LoadSpriteFromTexture(LoadTextureRaw(image), pixelsPerUnit);
    }
    
    public static Sprite LoadSpriteFromTexture(Texture2D spriteTexture, float pixelsPerUnit = 100f)
    {
        if ((Object) spriteTexture == (Object) null)
            return (Sprite) null;
        Sprite sprite = Sprite.Create(spriteTexture, new Rect(0.0f, 0.0f, (float) spriteTexture.width, (float) spriteTexture.height), new Vector2(0.0f, 0.0f), pixelsPerUnit);
        sprite.name = spriteTexture.name;
        return sprite;
    }
}