using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace LethalMin.Utils
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

            colors.Add(colors[0]);

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

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, $"graident.png"), bytes);

            texture.Apply();
            return texture;
        }

        public static Texture2D GenerateRadialGradient(List<Color> colors, float smoothness, int textureSize = 256)
        {
            if (colors == null || colors.Count < 2)
            {
                Debug.LogError("At least two colors are required to create a gradient.");
                return null!;
            }

            colors.Add(colors[0]);

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            float step = 1f / (colors.Count - 1);
            Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    // Calculate angle from center
                    Vector2 pixel = new Vector2(x, y) - center;
                    float angle = Mathf.Atan2(pixel.y, pixel.x);

                    // Convert angle to 0-1 range
                    float t = (angle + Mathf.PI) / (2f * Mathf.PI);

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

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, $"graident_radial.png"), bytes);

            texture.Apply();
            return texture;
        }

        public static Texture2D GenerateHorizontalGradient(List<Color> colors, float smoothness, int textureSize = 128)
        {
            if (colors == null || colors.Count < 2)
            {
                Debug.LogError("At least two colors are required to create a gradient.");
                return null!;
            }

            // Ensure the gradient wraps around by adding the first color to the end
            colors.Add(colors[0]);

            Texture2D texture = new Texture2D(textureSize, 1, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            float step = 1f / (colors.Count - 1);

            for (int y = 0; y < 2; y++)
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

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, $"graident_hori.png"), bytes);

            texture.Apply();
            return texture;
        }
    }
}