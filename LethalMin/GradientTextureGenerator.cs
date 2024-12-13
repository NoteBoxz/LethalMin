using UnityEngine;
using System.Collections.Generic;
namespace LethalMin
{
    public static class GradientTextureGenerator
    {
        public static Texture2D Generate90DegreeGradient(List<Color> colors, float smoothness, int textureSize = 256)
        {
            if (colors == null || colors.Count < 2)
            {
                Debug.LogError("At least two colors are required to create a gradient.");
                return null!;
            }

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            float step = 1f / (colors.Count - 1);

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float t = (float)x / (textureSize - 1);
                    int colorIndex = Mathf.FloorToInt(t / step);
                    colorIndex = Mathf.Clamp(colorIndex, 0, colors.Count - 2);

                    Color colorA = colors[colorIndex];
                    Color colorB = colors[colorIndex + 1];
                    float localT = (t - colorIndex * step) / step;

                    // Adjust the interpolation with the smoothness parameter
                    localT = Mathf.Pow(localT, smoothness);

                    Color pixelColor = Color.Lerp(colorA, colorB, localT);
                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}