using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GlitchRun.EditorScripts
{
    public static class AssetGenerator
    {
        [MenuItem("Glitch Run/Generate All Art & Audio Assets", priority = 2)]
        public static void GenerateAllAssets()
        {
            Debug.Log("[GlitchRun] Generating Art & Audio assets...");
            EnsureDirectories();
            GenerateSprites();
            GenerateAudioClips();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GlitchRun] All Art & Audio assets generated successfully!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                "Assets/Art/Sprites",
                "Assets/Art/UI",
                "Assets/Art/Decor",
                "Assets/Audio/SFX",
                "Assets/Audio/Music"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
        }

        #region Sprite Generation
        private static void GenerateSprites()
        {
            // 1. Drone Hazard Sprite (32x32)
            CreatePng("Assets/Art/Sprites/spr_drone.png", 32, 32, (x, y) =>
            {
                float dx = x - 15.5f;
                float dy = y - 15.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Body chassis
                if (dist < 11f && Mathf.Abs(dy) < 7f)
                {
                    // Core scanning eye in center
                    if (dist < 4.5f) return new Color(1f, 0.1f, 0.25f, 1f); // Neon red sensor
                    if (x < 6 || x > 25) return new Color(0.12f, 0.85f, 1f, 1f); // Cyan thruster tips
                    return new Color(0.15f, 0.17f, 0.24f, 1f); // Dark metallic chassis
                }
                // Rotor antennae/wings
                if (Mathf.Abs(dy) < 1.5f && dist < 14f)
                {
                    return new Color(0.0f, 0.9f, 1f, 0.8f);
                }
                return Color.clear;
            }, FilterMode.Point, true);

            // 2. Glitch Barrier Sprite (32x96)
            CreatePng("Assets/Art/Sprites/spr_glitch_barrier.png", 32, 96, (x, y) =>
            {
                // Holographic grid / mesh
                bool border = x < 3 || x > 28;
                bool grid = (x % 6 == 0) || (y % 8 == 0);
                float alpha = border ? 0.9f : (grid ? 0.7f : 0.25f);
                float glow = Mathf.Sin(y * 0.15f) * 0.2f + 0.8f;
                return new Color(0.85f * glow, 0.1f, 0.95f * glow, alpha);
            }, FilterMode.Bilinear, true);

            // 3. Checkpoint Beacon (32x48)
            CreatePng("Assets/Art/Sprites/spr_checkpoint.png", 32, 48, (x, y) =>
            {
                // Terminal pillar base
                if (y < 12 && x >= 4 && x <= 27) return new Color(0.14f, 0.16f, 0.22f, 1f);
                // Center column
                if (y >= 12 && y < 34 && x >= 10 && x <= 21)
                {
                    if (x >= 13 && x <= 18) return new Color(0.0f, 1f, 0.85f, 0.9f); // Cyan glowing conduit
                    return new Color(0.18f, 0.2f, 0.28f, 1f);
                }
                // Floating holographic diamond emitter at top
                if (y >= 34)
                {
                    float d = Mathf.Abs(x - 15.5f) + Mathf.Abs(y - 41f);
                    if (d < 6f) return new Color(0.1f, 1f, 0.6f, 0.95f); // Glowing diamond
                }
                return Color.clear;
            }, FilterMode.Point, true);

            // 4. UI Life Heart / Node (24x24)
            CreatePng("Assets/Art/UI/spr_heart.png", 24, 24, (x, y) =>
            {
                float nx = (x - 11.5f) / 10f;
                float ny = (y - 12.5f) / 10f;
                // Heart formula: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                float a = nx * nx + ny * ny - 0.7f;
                if (a * a * a - nx * nx * ny * ny * ny <= 0.05f)
                {
                    return new Color(0.0f, 1f, 0.8f, 0.95f); // Bright cyan cyber heart
                }
                return Color.clear;
            }, FilterMode.Point, true);

            // 5. UI Button Frame (128x40) - 9-sliced capable
            CreatePng("Assets/Art/UI/spr_ui_button.png", 128, 40, (x, y) =>
            {
                bool isBorder = (x < 3 || x >= 125 || y < 3 || y >= 37);
                bool isCorner = (x < 6 && y < 6) || (x >= 122 && y >= 34); // Chamfered corners
                if (isCorner) return Color.clear;
                if (isBorder) return new Color(0.0f, 0.95f, 1f, 0.95f); // Cyan border
                // Subtle scanline fill
                float scan = (y % 4 == 0) ? 0.18f : 0.22f;
                return new Color(0.08f, 0.1f, 0.16f, scan);
            }, FilterMode.Bilinear, false);

            // 6. UI Meter Fill (64x16)
            CreatePng("Assets/Art/UI/spr_meter_fill.png", 64, 16, (x, y) =>
            {
                float t = (float)x / 63f;
                // Gradient cyan to purple
                Color c = Color.Lerp(new Color(0.0f, 1f, 0.85f, 1f), new Color(0.9f, 0.1f, 0.95f, 1f), t);
                if (y == 0 || y == 15) c *= 0.7f;
                return c;
            }, FilterMode.Bilinear, false);

            // 7. Cyberpunk Holo Sign / Warning (48x48)
            CreatePng("Assets/Art/Decor/spr_holo_warning.png", 48, 48, (x, y) =>
            {
                // Warning triangle
                float nx = (x - 23.5f) / 20f;
                float ny = (y - 12f) / 30f;
                if (ny >= 0 && ny <= 1 && Mathf.Abs(nx) <= (1f - ny))
                {
                    // Exclamation mark inside
                    if (Mathf.Abs(x - 23.5f) < 2.5f)
                    {
                        if (y >= 20 && y <= 34) return new Color(0.05f, 0.05f, 0.08f, 1f);
                        if (y >= 14 && y <= 17) return new Color(0.05f, 0.05f, 0.08f, 1f);
                    }
                    return new Color(1f, 0.85f, 0.0f, 0.85f); // Holo amber
                }
                return Color.clear;
            }, FilterMode.Point, true);
        }

        private static void CreatePng(string path, int width, int height, Func<int, int, Color> colorFunc, FilterMode filter, bool isSprite)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, colorFunc(x, y));
                }
            }
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = filter;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
        #endregion

        #region Procedural Audio Generation
        private static void GenerateAudioClips()
        {
            // Synthesize all SFX and music loops
            SaveWav("Assets/Audio/SFX/sfx_jump.wav", SynthesizeJump());
            SaveWav("Assets/Audio/SFX/sfx_pickup.wav", SynthesizePickup());
            SaveWav("Assets/Audio/SFX/sfx_glitch_on.wav", SynthesizeGlitchOn());
            SaveWav("Assets/Audio/SFX/sfx_glitch_off.wav", SynthesizeGlitchOff());
            SaveWav("Assets/Audio/SFX/sfx_damage.wav", SynthesizeDamage());
            SaveWav("Assets/Audio/SFX/sfx_death.wav", SynthesizeDeath());
            SaveWav("Assets/Audio/SFX/sfx_checkpoint.wav", SynthesizeCheckpoint());
            SaveWav("Assets/Audio/SFX/sfx_victory.wav", SynthesizeVictory());
            SaveWav("Assets/Audio/SFX/sfx_ui_click.wav", SynthesizeClick());
            SaveWav("Assets/Audio/Music/bgm_gameplay.wav", SynthesizeGameplayMusic());
            SaveWav("Assets/Audio/Music/bgm_menu.wav", SynthesizeMenuMusic());
        }

        private static float[] SynthesizeJump()
        {
            int sampleRate = 44100;
            float duration = 0.18f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(160f, 540f, Mathf.Sqrt(progress));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                float env = 1f - progress;
                samples[i] = wave * env * 0.45f;
            }
            return samples;
        }

        private static float[] SynthesizePickup()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = (progress < 0.5f) ? 880f : 1320f; // A5 -> E6 chime
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                float env = Mathf.Exp(-6f * (progress % 0.5f));
                samples[i] = wave * env * 0.4f;
            }
            return samples;
        }

        private static float[] SynthesizeGlitchOn()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            System.Random rng = new System.Random(42);
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(900f, 150f, progress);
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)); // Square wave
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.4f;
                float env = 1f - progress * progress;
                samples[i] = (wave * 0.6f + noise) * env * 0.35f;
            }
            return samples;
        }

        private static float[] SynthesizeGlitchOff()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(220f, 660f, progress);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                float env = 1f - progress;
                samples[i] = wave * env * 0.35f;
            }
            return samples;
        }

        private static float[] SynthesizeDamage()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            System.Random rng = new System.Random(1337);
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float bass = Mathf.Sin(2f * Mathf.PI * 90f * t);
                float env = Mathf.Exp(-7f * progress);
                samples[i] = (noise * 0.7f + bass * 0.5f) * env * 0.5f;
            }
            return samples;
        }

        private static float[] SynthesizeDeath()
        {
            int sampleRate = 44100;
            float duration = 0.65f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            System.Random rng = new System.Random(999);
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(300f, 40f, progress);
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - progress);
                float env = 1f - progress;
                samples[i] = (wave * 0.5f + noise * 0.5f) * env * 0.5f;
            }
            return samples;
        }

        private static float[] SynthesizeCheckpoint()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] notes = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5 arpeggio
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIdx = Mathf.Clamp((int)(t / 0.11f), 0, notes.Length - 1);
                float noteT = t - (noteIdx * 0.11f);
                float wave = Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t);
                float env = Mathf.Exp(-8f * noteT);
                samples[i] = wave * env * 0.4f;
            }
            return samples;
        }

        private static float[] SynthesizeVictory()
        {
            int sampleRate = 44100;
            float duration = 0.9f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] notes = { 440f, 554.37f, 659.25f, 880f }; // A4, C#5, E5, A5
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIdx = Mathf.Clamp((int)(t / 0.22f), 0, notes.Length - 1);
                float noteT = t - (noteIdx * 0.22f);
                float wave = Mathf.Sin(2f * Mathf.PI * notes[noteIdx] * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * notes[noteIdx] * t);
                float env = Mathf.Exp(-4f * noteT);
                samples[i] = wave * env * 0.35f;
            }
            return samples;
        }

        private static float[] SynthesizeClick()
        {
            int sampleRate = 44100;
            float duration = 0.04f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                float wave = Mathf.Sin(2f * Mathf.PI * 2200f * t);
                float env = 1f - progress;
                samples[i] = wave * env * 0.35f;
            }
            return samples;
        }

        private static float[] SynthesizeGameplayMusic()
        {
            int sampleRate = 44100;
            float bpm = 124f;
            float beatDur = 60f / bpm;
            float duration = beatDur * 16f; // 4-bar seamless loop (~7.74s)
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // 16-step bassline sequence (A minor cyberpunk groove: A1, A1, C2, D2, A1, G1, A1...)
            float[] bassPitches = { 55f, 55f, 65.4f, 73.4f, 55f, 49f, 55f, 65.4f, 55f, 55f, 82.4f, 73.4f, 55f, 49f, 55f, 65.4f };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float stepTime = t / (beatDur * 0.5f);
                int step = (int)stepTime % 16;
                float stepFrac = stepTime - Mathf.Floor(stepTime);

                // Bass synth
                float bassFreq = bassPitches[step];
                float bassEnv = Mathf.Exp(-5f * stepFrac);
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) + 0.5f * Mathf.Sin(4f * Mathf.PI * bassFreq * t);

                // Hi-hat noise on 16ths
                float hatEnv = Mathf.Exp(-28f * stepFrac);
                float hat = ((i * 19) % 100 / 50f - 1f) * hatEnv * 0.08f;

                // Kick pulse on beats 0, 4, 8, 12
                float kick = 0f;
                if (step % 4 == 0)
                {
                    float kickT = (t % beatDur);
                    float kickFreq = Mathf.Lerp(120f, 40f, kickT / beatDur);
                    kick = Mathf.Sin(2f * Mathf.PI * kickFreq * kickT) * Mathf.Exp(-12f * kickT) * 0.4f;
                }

                samples[i] = Mathf.Clamp((bass * bassEnv * 0.28f + kick + hat), -1f, 1f);
            }
            return samples;
        }

        private static float[] SynthesizeMenuMusic()
        {
            int sampleRate = 44100;
            float duration = 8.0f; // 8-second seamless ambient drone loop
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float lfo = Mathf.Sin(2f * Mathf.PI * (1f / duration) * t); // seamless LFO

                // Layered low root (A1 55Hz) + fifth (E2 82.4Hz) + octave shimmer
                float sub = Mathf.Sin(2f * Mathf.PI * 55f * t);
                float fifth = Mathf.Sin(2f * Mathf.PI * 82.4f * t + 0.5f) * 0.5f;
                float shimmer = Mathf.Sin(2f * Mathf.PI * (220f + lfo * 8f) * t) * (0.15f + lfo * 0.08f);

                samples[i] = (sub * 0.35f + fifth * 0.2f + shimmer) * 0.4f;
            }
            return samples;
        }

        private static void SaveWav(string path, float[] samples)
        {
            int sampleRate = 44100;
            short channels = 1;
            short bitsPerSample = 16;
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            short blockAlign = (short)(channels * (bitsPerSample / 8));
            int subChunk2Size = samples.Length * (bitsPerSample / 8);
            int chunkSize = 36 + subChunk2Size;

            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // RIFF header
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(chunkSize);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt subchunk
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // Subchunk1Size for PCM
                writer.Write((short)1); // AudioFormat 1 = PCM
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);

                // data subchunk
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(subChunk2Size);

                for (int i = 0; i < samples.Length; i++)
                {
                    short sample = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    writer.Write(sample);
                }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        #endregion
    }
}
