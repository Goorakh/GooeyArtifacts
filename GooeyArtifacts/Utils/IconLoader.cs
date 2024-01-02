using UnityEngine;

namespace GooeyArtifacts.Utils
{
    public class IconLoader
    {
        public static Sprite LoadSpriteFromBytes(byte[] imageBytes)
        {
            Texture2D texture = new Texture2D(1, 1);
            if (texture.LoadImage(imageBytes))
            {
                return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
            }
            else
            {
                Log.Error("Failed to load image");
                return null;
            }
        }
    }
}
