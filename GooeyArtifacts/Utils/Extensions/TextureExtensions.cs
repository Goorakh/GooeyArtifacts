using UnityEngine;

namespace GooeyArtifacts.Utils.Extensions
{
    public static class TextureExtensions
    {
        public static TemporaryTexture AsReadable(this Texture2D texture)
        {
            if (texture.isReadable)
                return new TemporaryTexture(texture, false);

            RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            Graphics.Blit(texture, tmp);

            RenderTexture previous = RenderTexture.active;

            RenderTexture.active = tmp;

            Texture2D readableTexture = new Texture2D(texture.width, texture.height);

            readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;

            RenderTexture.ReleaseTemporary(tmp);

            return new TemporaryTexture(readableTexture, true);
        }
    }
}
