using UnityEngine;

namespace LethalMin.Utils
{
    public class PerlinNoiseGenerator : MonoBehaviour
    {
        public float scale = 20f;
        public float smoothness = 2f; // Controls the smoothness of the noise
        public Vector2 randomOffset = Vector2.zero; // Adds randomness to the noise

        [ContextMenu("Test")]
        public void TestGenOnAttachedRender()
        {
            Vector2 randomOffset = new Vector2(Random.Range(0, 1000), Random.Range(0, 1000));
            GetComponent<Renderer>().sharedMaterial.mainTexture = GeneratePerlinNoise(250, 250, scale, smoothness, randomOffset);
        }

        public static Texture2D GeneratePerlinNoise(int width = 250, int height = 250, float scale = 20f, float smoothness = 2f, Vector2 randomOffset = default)
        {
            Texture2D texture = new Texture2D(width, height);

            // Apply random offset to add uniqueness to each texture
            float offsetX = randomOffset.x;
            float offsetY = randomOffset.y;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Calculate the Perlin noise value with smoothness
                    float xCoord = (float)x / width * scale + offsetX;
                    float yCoord = (float)y / height * scale + offsetY;

                    // Sample multiple layers of noise for smoothness
                    float sample = 0f;
                    float amplitude = 1f;
                    float frequency = 1f;
                    float maxValue = 0f; // Used to normalize the result

                    for (int i = 0; i < smoothness; i++)
                    {
                        sample += Mathf.PerlinNoise(xCoord * frequency, yCoord * frequency) * amplitude;
                        maxValue += amplitude;
                        amplitude *= 0.5f; // Decrease amplitude for each layer
                        frequency *= 2f; // Increase frequency for each layer
                    }

                    sample /= maxValue; // Normalize the result

                    // Set the color based on the Perlin noise value
                    Color color = new Color(sample, sample, sample);
                    texture.SetPixel(x, y, color);
                }
            }

            // Apply the changes to the texture
            texture.Apply();

            return texture;
        }
    }
}