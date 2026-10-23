using System.IO;
using UnityEditor;
using UnityEngine;

namespace GlitchRun.EditorScripts
{
    public static class CyberpunkBackgroundGenerator
    {
        private const string OutputDir = "Assets/Art/Environments/Background";

        [MenuItem("Glitch Run/Generate Seamless Cyberpunk Backgrounds")]
        public static void GenerateAllBackgrounds()
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            GenerateFarSkyline(Path.Combine(OutputDir, "bg_cyberpunk_far.png"), 2048, 1024);
            GenerateMidSkyline(Path.Combine(OutputDir, "bg_cyberpunk_mid.png"), 2048, 1024);
            GenerateNearSkyline(Path.Combine(OutputDir, "bg_cyberpunk_near.png"), 2048, 512);

            AssetDatabase.Refresh();

            ConfigureSpriteImport(Path.Combine(OutputDir, "bg_cyberpunk_far.png"));
            ConfigureSpriteImport(Path.Combine(OutputDir, "bg_cyberpunk_mid.png"));
            ConfigureSpriteImport(Path.Combine(OutputDir, "bg_cyberpunk_near.png"));

            Debug.Log("[GlitchRun] Seamless cyberpunk background textures successfully generated and imported!");
        }

        private static void ConfigureSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static void GenerateFarSkyline(string filePath, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color colTop = new Color(0.02f, 0.03f, 0.08f, 1f);     // Deep midnight void
            Color colMid = new Color(0.06f, 0.04f, 0.16f, 1f);     // Deep cyberpunk violet
            Color colBottom = new Color(0.01f, 0.06f, 0.14f, 1f);  // Cyan smog haze

            // 1. Sky Gradient & Ambient Haze
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color sky = t > 0.5f ? Color.Lerp(colMid, colTop, (t - 0.5f) * 2f) : Color.Lerp(colBottom, colMid, t * 2f);
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, sky);
                }
            }

            // 2. Far Mega-Skyscrapers (Harmonic sinusoidal wrapping for seamless loop)
            int numBuildings = 32;
            for (int b = 0; b < numBuildings; b++)
            {
                float bPhase = (float)b / numBuildings;
                int bWidth = (int)(width / (numBuildings * 0.9f));
                int bCenterX = (int)(bPhase * width);
                int bHeight = (int)(height * (0.35f + 0.35f * Mathf.Abs(Mathf.Sin(bPhase * Mathf.PI * 4f + 0.5f))));

                Color bColor = new Color(0.03f, 0.02f, 0.09f, 0.95f);
                Color winColor = (b % 3 == 0) ? new Color(0f, 0.9f, 1f, 0.7f) : new Color(0.85f, 0.1f, 0.9f, 0.65f);

                for (int dx = -bWidth / 2; dx <= bWidth / 2; dx++)
                {
                    int px = (bCenterX + dx + width) % width; // Seamless wrapping
                    for (int py = 0; py < bHeight; py++)
                    {
                        // Window grid
                        if (py > 80 && py < bHeight - 40 && (py % 18 < 6) && (Mathf.Abs(dx) % 8 < 4))
                        {
                            tex.SetPixel(px, py, winColor);
                        }
                        else
                        {
                            tex.SetPixel(px, py, bColor);
                        }
                    }

                    // Antenna spire with blinking beacon
                    if (dx == 0 && b % 2 == 0)
                    {
                        for (int sp = bHeight; sp < bHeight + 40 && sp < height; sp++)
                        {
                            tex.SetPixel(px, sp, new Color(0.05f, 0.05f, 0.12f, 0.9f));
                        }
                        if (bHeight + 40 < height)
                        {
                            tex.SetPixel(px, bHeight + 40, new Color(1f, 0.1f, 0.3f, 1f));
                        }
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateMidSkyline(string filePath, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            // Fill transparent
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, clear);

            // Mid Towers with Neon Billboards
            int numTowers = 24;
            for (int t = 0; t < numTowers; t++)
            {
                float phase = (float)t / numTowers;
                int tWidth = (int)(width / (numTowers * 0.75f));
                int tCenterX = (int)(phase * width);
                int tHeight = (int)(height * (0.28f + 0.38f * Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 6f + 1.2f))));

                Color towerBody = new Color(0.015f, 0.02f, 0.05f, 0.98f);
                Color neonBlue = new Color(0f, 0.85f, 1f, 0.95f);
                Color neonPink = new Color(1f, 0.05f, 0.65f, 0.95f);

                for (int dx = -tWidth / 2; dx <= tWidth / 2; dx++)
                {
                    int px = (tCenterX + dx + width) % width; // Seamless wrapping
                    for (int py = 0; py < tHeight; py++)
                    {
                        // Holographic billboard band
                        if (t % 2 == 0 && py > tHeight * 0.5f && py < tHeight * 0.65f && Mathf.Abs(dx) < tWidth / 2 - 4)
                        {
                            Color holo = (t % 4 == 0) ? neonBlue : neonPink;
                            tex.SetPixel(px, py, holo);
                        }
                        // Vertical neon edge trim
                        else if (Mathf.Abs(dx) == tWidth / 2 - 2 && py < tHeight - 10)
                        {
                            tex.SetPixel(px, py, neonBlue);
                        }
                        // Illuminated office windows
                        else if (py > 50 && (py % 14 < 5) && (Mathf.Abs(dx) % 10 < 5))
                        {
                            tex.SetPixel(px, py, new Color(0.2f, 0.7f, 0.9f, 0.8f));
                        }
                        else
                        {
                            tex.SetPixel(px, py, towerBody);
                        }
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateNearSkyline(string filePath, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, clear);

            // Near Rooftops, Industrial Pipes, and Hazard Stripes
            int numBlocks = 16;
            for (int b = 0; b < numBlocks; b++)
            {
                float phase = (float)b / numBlocks;
                int bWidth = (int)(width / (numBlocks * 0.85f));
                int bCenterX = (int)(phase * width);
                int bHeight = (int)(height * (0.35f + 0.35f * Mathf.Abs(Mathf.Cos(phase * Mathf.PI * 4f))));

                Color blockCol = new Color(0.008f, 0.012f, 0.025f, 1f);
                Color hazardYellow = new Color(1f, 0.8f, 0.1f, 0.9f);
                Color neonCyan = new Color(0f, 1f, 0.85f, 0.95f);

                for (int dx = -bWidth / 2; dx <= bWidth / 2; dx++)
                {
                    int px = (bCenterX + dx + width) % width; // Seamless wrapping
                    for (int py = 0; py < bHeight; py++)
                    {
                        // Hazard caution line across top parapet
                        if (py > bHeight - 12 && py < bHeight - 2)
                        {
                            bool isStripe = ((px + py) / 8) % 2 == 0;
                            tex.SetPixel(px, py, isStripe ? hazardYellow : Color.black);
                        }
                        // Glowing conduit pipe
                        else if (py > 25 && py < 35)
                        {
                            tex.SetPixel(px, py, neonCyan);
                        }
                        else
                        {
                            tex.SetPixel(px, py, blockCol);
                        }
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
