using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Synthesises all original sound effects and two cheerful music loops as WAV files.
    /// Deterministic (seeded), so re-running produces identical audio. No copyrighted material.
    /// </summary>
    public static class AudioSynth
    {
        public const int Rate = 22050;
        public const string SfxFolder = EditorAssetUtil.Root + "/Audio/SFX";
        public const string MusicFolder = EditorAssetUtil.Root + "/Audio/Music";

        private static System.Random s_Rng = new System.Random(1234);

        private enum Wave { Sine, Triangle, Square, Saw }

        public static void GenerateAll()
        {
            EditorAssetUtil.EnsureFolder(SfxFolder);
            EditorAssetUtil.EnsureFolder(MusicFolder);
            s_Rng = new System.Random(1234);

            var sfx = new Dictionary<string, float[]>
            {
                { "sfx_button", ButtonTap() },
                { "sfx_pop", Pop() },
                { "sfx_snap", Snap() },
                { "sfx_whoosh", Whoosh() },
                { "sfx_star", Bell(1318.5f, 0.6f, 0.45f) },
                { "sfx_celebration", Fanfare() },
                { "sfx_sparkle", Sparkle() },
                { "sfx_horn", Horn() },
                { "sfx_hydraulic", Hydraulic() },
                { "sfx_soil_dump", SoilDump() },
                { "sfx_scoop", Scoop() },
                { "sfx_load", LoadClunk() },
                { "sfx_bed_tip", Slide(260f, 160f, 0.45f) },
                { "sfx_thud", Thud() },
                { "sfx_boing", Boing() },
                { "sfx_unlock", Jingle() },
                { "sfx_engine_start", EngineStart() }
            };
            foreach (var kv in sfx) WriteWav($"{SfxFolder}/{kv.Key}.wav", kv.Value);

            WriteWav($"{SfxFolder}/sfx_engine_loop.wav", EngineLoop());
            WriteWav($"{MusicFolder}/music_town_loop.wav", Song(104f, false));
            WriteWav($"{MusicFolder}/music_menu_loop.wav", Song(88f, true));

            AssetDatabase.Refresh();
            foreach (var kv in sfx) ConfigureClip($"{SfxFolder}/{kv.Key}.wav", false);
            ConfigureClip($"{SfxFolder}/sfx_engine_loop.wav", false);
            ConfigureClip($"{MusicFolder}/music_town_loop.wav", true);
            ConfigureClip($"{MusicFolder}/music_menu_loop.wav", true);
        }

        // ------------------------------------------------------------------ construction sounds (Little Builders World)
        /// <summary>Soft, low engine hum: a few detuned low harmonics with a gentle chug, seamless loop.</summary>
        private static float[] EngineLoop()
        {
            float[] b = Buf(2.2f);
            const float f0 = 55f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float chug = 0.75f + 0.25f * Mathf.Sin(t * Mathf.PI * 2f * 9f);
                float v = Mathf.Sin(t * f0 * Mathf.PI * 2f) * 0.6f + Mathf.Sin(t * f0 * 2.01f * Mathf.PI * 2f) * 0.3f +
                          Osc(Wave.Triangle, t * f0 * 3f) * 0.12f;
                b[i] = v * chug + Noise() * 0.04f;
            }
            LowPass(b, 500f);
            return Normalize(MakeLoop(b, Rate / 5), 0.6f, false);
        }

        private static float[] EngineStart()
        {
            float[] b = Buf(0.7f);
            Tone(b, 0f, 0.7f, t => Mathf.Lerp(40f, 75f, Mathf.Clamp01(t / 0.4f)), k => Mathf.Sin(k * Mathf.PI) * (0.7f + 0.3f * Mathf.Sin(k * 60f)), Wave.Triangle, 0.8f);
            NoiseBurst(b, 0f, 0.7f, k => Mathf.Sin(k * Mathf.PI) * 0.3f, 0.3f);
            LowPass(b, 700f);
            return Normalize(b, 0.55f);
        }

        /// <summary>Gentle "beep-beep": two soft rounded tones, never a harsh blast.</summary>
        private static float[] Horn()
        {
            float[] b = Buf(0.62f);
            for (int n = 0; n < 2; n++)
            {
                float s0 = n * 0.3f;
                Tone(b, s0, 0.24f, t => 392f, Env(0.12f, 1.4f), Wave.Triangle, 0.7f);
                Tone(b, s0, 0.24f, t => 494f, Env(0.12f, 1.4f), Wave.Sine, 0.5f);
            }
            LowPass(b, 2200f);
            return Normalize(b, 0.55f);
        }

        private static float[] Hydraulic()
        {
            float[] b = Buf(0.6f);
            NoiseBurst(b, 0f, 0.6f, k => Mathf.Sin(k * Mathf.PI), 0.6f);
            LowPass(b, 900f);
            Tone(b, 0f, 0.6f, t => Mathf.Lerp(140f, 210f, t / 0.6f), k => Mathf.Sin(k * Mathf.PI) * 0.5f, Wave.Triangle, 0.4f);
            return Normalize(b, 0.4f);
        }

        private static float[] SoilDump()
        {
            float[] b = Buf(0.7f);
            for (int k = 0; k < 14; k++)
            {
                float st = 0.02f + k * 0.04f + (float)s_Rng.NextDouble() * 0.03f;
                NoiseBurst(b, st, 0.08f, e => Mathf.Pow(1f - e, 2f), 0.5f);
            }
            LowPass(b, 600f);
            Tone(b, 0.05f, 0.3f, t => Mathf.Lerp(120f, 60f, t / 0.3f), Env(0.05f, 2f), Wave.Sine, 0.6f);
            return Normalize(b, 0.6f);
        }

        private static float[] Scoop()
        {
            float[] b = Buf(0.35f);
            NoiseBurst(b, 0f, 0.3f, k => Mathf.Sin(k * Mathf.PI), 0.5f);
            LowPass(b, 800f);
            Tone(b, 0.15f, 0.18f, t => 300f, Env(0.05f, 3f), Wave.Sine, 0.4f);
            return Normalize(b, 0.55f);
        }

        private static float[] LoadClunk()
        {
            float[] b = Buf(0.4f);
            Tone(b, 0f, 0.25f, t => Mathf.Lerp(180f, 90f, t / 0.25f), Env(0.02f, 2.5f), Wave.Triangle, 0.8f);
            Tone(b, 0.12f, 0.22f, t => Mathf.Lerp(160f, 80f, t / 0.22f), Env(0.02f, 2.5f), Wave.Triangle, 0.6f);
            NoiseBurst(b, 0f, 0.3f, k => Mathf.Pow(1f - k, 3f), 0.2f);
            LowPass(b, 1200f);
            return Normalize(b, 0.6f);
        }

        private static float[] Thud()
        {
            float[] b = Buf(0.3f);
            Tone(b, 0f, 0.28f, t => Mathf.Lerp(130f, 55f, t / 0.28f), Env(0.02f, 2.2f), Wave.Sine, 1f);
            NoiseBurst(b, 0f, 0.1f, k => 1f - k, 0.2f);
            LowPass(b, 900f);
            return Normalize(b, 0.6f);
        }

        public static AudioClip Get(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/{name}.wav") ??
                   AssetDatabase.LoadAssetAtPath<AudioClip>($"{MusicFolder}/{name}.wav");
        }

        internal static void ConfigureClip(string path, bool music)
        {
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) return;
            imp.forceToMono = true;
            imp.loadInBackground = music;
            AudioImporterSampleSettings s = imp.defaultSampleSettings;
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            s.quality = 0.6f;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------ primitives
        private static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

        private static float Osc(Wave w, float phase)
        {
            phase -= Mathf.Floor(phase);
            switch (w)
            {
                case Wave.Triangle: return 1f - 4f * Mathf.Abs(phase - 0.5f);
                case Wave.Square: return phase < 0.5f ? 0.6f : -0.6f;
                case Wave.Saw: return 2f * phase - 1f;
                default: return Mathf.Sin(phase * Mathf.PI * 2f);
            }
        }

        private static float Noise() => (float)(s_Rng.NextDouble() * 2.0 - 1.0);

        private static float MidiToHz(float n) => 440f * Mathf.Pow(2f, (n - 69f) / 12f);

        /// <summary>Adds a tone with frequency curve f(t), envelope env(t) into buf.</summary>
        private static void Tone(float[] buf, float start, float dur, Func<float, float> freq, Func<float, float> env,
            Wave wave, float vol, float vibratoHz = 0f, float vibratoDepth = 0f)
        {
            int s0 = Mathf.Max(0, (int)(start * Rate));
            int n = (int)(dur * Rate);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) idx %= buf.Length; // wrap (seamless loops)
                float t = i / (float)Rate;
                float f = freq(t) * (1f + (vibratoHz > 0f ? Mathf.Sin(t * vibratoHz * Mathf.PI * 2f) * vibratoDepth : 0f));
                phase += f / Rate;
                buf[idx] += Osc(wave, (float)phase) * env(t / dur) * vol;
            }
        }

        private static Func<float, float> Env(float attack, float decayPow = 2f) => k =>
            k < attack ? k / attack : Mathf.Pow(1f - (k - attack) / (1f - attack), decayPow);

        private static float[] Normalize(float[] b, float peak = 0.85f, bool fadeEdges = true)
        {
            float m = 0.0001f;
            foreach (float v in b) m = Mathf.Max(m, Mathf.Abs(v));
            float k = peak / m;
            for (int i = 0; i < b.Length; i++) b[i] = Mathf.Clamp(b[i] * k, -1f, 1f);
            if (!fadeEdges) return b;
            // tiny fade in/out to avoid clicks on one-shots
            int f = Mathf.Min(64, b.Length / 4);
            for (int i = 0; i < f; i++) { b[i] *= i / (float)f; b[b.Length - 1 - i] *= i / (float)f; }
            return b;
        }

        private static float[] LowPass(float[] b, float cutoff)
        {
            float rc = 1f / (cutoff * 2f * Mathf.PI);
            float dt = 1f / Rate;
            float a = dt / (rc + dt);
            float y = 0f;
            for (int i = 0; i < b.Length; i++) { y += a * (b[i] - y); b[i] = y; }
            return b;
        }

        private static float[] HighPass(float[] b, float cutoff)
        {
            float rc = 1f / (cutoff * 2f * Mathf.PI);
            float dt = 1f / Rate;
            float a = rc / (rc + dt);
            float prevX = 0f, prevY = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float y = a * (prevY + b[i] - prevX);
                prevX = b[i];
                prevY = y;
                b[i] = y;
            }
            return b;
        }

        private static void NoiseBurst(float[] buf, float start, float dur, Func<float, float> env, float vol)
        {
            int s0 = (int)(start * Rate);
            int n = (int)(dur * Rate);
            for (int i = 0; i < n && s0 + i < buf.Length; i++) buf[s0 + i] += Noise() * env(i / (float)n) * vol;
        }

        // ------------------------------------------------------------------ effects
        private static float[] Pop()
        {
            float[] b = Buf(0.12f);
            Tone(b, 0, 0.1f, t => Mathf.Lerp(520f, 1250f, t / 0.1f), Env(0.05f, 3f), Wave.Sine, 1f);
            Tone(b, 0, 0.06f, t => 2400f, Env(0.02f, 4f), Wave.Sine, 0.15f);
            return Normalize(b, 0.7f);
        }

        private static float[] Plop()
        {
            float[] b = Buf(0.18f);
            Tone(b, 0, 0.16f, t => Mathf.Lerp(380f, 150f, t / 0.16f), Env(0.03f, 2.5f), Wave.Sine, 1f);
            return Normalize(b, 0.75f);
        }

        private static float[] Snap()
        {
            float[] b = Buf(0.22f);
            Tone(b, 0, 0.06f, t => 880f, Env(0.02f, 3f), Wave.Triangle, 0.8f);
            Tone(b, 0.05f, 0.15f, t => 1318f, Env(0.02f, 2.5f), Wave.Sine, 0.9f);
            return Normalize(b, 0.7f);
        }

        private static float[] Whoosh()
        {
            float[] b = Buf(0.35f);
            NoiseBurst(b, 0, 0.35f, k => Mathf.Sin(k * Mathf.PI), 1f);
            LowPass(b, 1400f);
            return Normalize(b, 0.5f);
        }

        private static float[] SizzleLoop()
        {
            float[] b = Buf(2f);
            for (int i = 0; i < b.Length; i++) b[i] = Noise() * (0.6f + 0.4f * Mathf.Sin(i / (float)Rate * Mathf.PI * 2f * 3f));
            HighPass(b, 2500f);
            for (int k = 0; k < 60; k++)
            {
                int p = s_Rng.Next(0, b.Length - 200);
                for (int i = 0; i < 120; i++) b[p + i] += Noise() * (1f - i / 120f) * 1.5f;
            }
            return Normalize(MakeLoop(b, 2000), 0.45f, false);
        }

        private static float[] Flip()
        {
            float[] b = Buf(0.3f);
            Tone(b, 0, 0.28f, t => Mathf.Lerp(300f, 900f, t / 0.28f), Env(0.05f, 1.5f), Wave.Triangle, 1f, 18f, 0.04f);
            NoiseBurst(b, 0, 0.08f, k => 1f - k, 0.2f);
            return Normalize(b, 0.65f);
        }

        private static float[] Bell(float f, float dur, float vol)
        {
            float[] b = Buf(dur);
            Tone(b, 0, dur, t => f, Env(0.005f, 3f), Wave.Sine, 1f);
            Tone(b, 0, dur * 0.6f, t => f * 2.76f, Env(0.005f, 4f), Wave.Sine, 0.35f);
            Tone(b, 0, dur * 0.4f, t => f * 5.4f, Env(0.005f, 5f), Wave.Sine, 0.15f);
            return Normalize(b, vol + 0.2f);
        }

        private static float[] BubbleLoop()
        {
            float[] b = Buf(2f);
            for (int k = 0; k < 28; k++)
            {
                float st = (float)s_Rng.NextDouble() * 1.95f;
                float f0 = 350f + (float)s_Rng.NextDouble() * 500f;
                Tone(b, st, 0.05f, t => f0 * (1f + t * 18f), Env(0.1f, 2f), Wave.Sine, 0.6f);
            }
            float[] n = Buf(2f);
            NoiseBurst(n, 0, 2f, k => 0.15f, 1f);
            LowPass(n, 500f);
            for (int i = 0; i < b.Length; i++) b[i] += n[i];
            return Normalize(MakeLoop(b, 1500), 0.45f, false);
        }

        private static float[] PourLoop()
        {
            float[] b = Buf(1.5f);
            NoiseBurst(b, 0, 1.5f, k => 1f, 1f);
            LowPass(b, 900f);
            for (int i = 0; i < b.Length; i++) b[i] *= 0.7f + 0.3f * Mathf.Sin(i / (float)Rate * Mathf.PI * 2f * 7f);
            for (int k = 0; k < 14; k++)
            {
                float st = (float)s_Rng.NextDouble() * 1.4f;
                Tone(b, st, 0.06f, t => 300f + t * 3000f, Env(0.2f, 2f), Wave.Sine, 0.25f);
            }
            return Normalize(MakeLoop(b, 1500), 0.45f, false);
        }

        private static float[] Cream()
        {
            float[] b = Buf(0.45f);
            NoiseBurst(b, 0, 0.42f, k => (k < 0.1f ? k / 0.1f : 1f - (k - 0.1f) / 0.9f), 1f);
            HighPass(b, 1800f);
            Tone(b, 0.05f, 0.3f, t => 700f - t * 600f, Env(0.1f, 2f), Wave.Sine, 0.2f);
            return Normalize(b, 0.5f);
        }

        /// <summary>Soft, seamless rain (gentle, not stormy).</summary>
        private static float[] RainLoop()
        {
            float[] b = Buf(3f);
            var rnd = new System.Random(11);
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float n = (float)(rnd.NextDouble() * 2 - 1);
                lp += (n - lp) * 0.35f;
                b[i] = lp * 0.35f;
            }
            for (int d = 0; d < 90; d++)
            {
                float t = (float)rnd.NextDouble() * 2.9f;
                Tone(b, t, 0.03f, x => 1800f + (float)rnd.NextDouble() * 1500f, Env(0.002f, 8f), Wave.Sine, 0.12f);
            }
            int fade = (int)(0.05f * Rate);
            for (int i = 0; i < fade; i++) { float k = i / (float)fade; b[i] *= k; b[b.Length - 1 - i] *= k; }
            return Normalize(b, 0.35f);
        }

        /// <summary>Squishy "pfft-splort" of a sauce bottle.</summary>
        private static float[] Squeeze()
        {
            float[] b = Buf(0.42f);
            NoiseBurst(b, 0, 0.3f, k => Mathf.Sin(k * Mathf.PI), 0.7f);
            HighPass(b, 900f);
            Tone(b, 0.12f, 0.26f, t => 220f + Mathf.Sin(t * 60f) * 40f, Env(0.02f, 3f), Wave.Sine, 0.6f);
            return Normalize(b, 0.5f);
        }

        /// <summary>Soft bubbly scrubbing swish.</summary>
        private static float[] Scrub()
        {
            float[] b = Buf(0.3f);
            NoiseBurst(b, 0, 0.28f, k => Mathf.Sin(k * Mathf.PI) * (0.6f + 0.4f * Mathf.Sin(k * 40f)), 0.6f);
            HighPass(b, 2200f);
            Tone(b, 0.05f, 0.12f, t => 900f + t * 2000f, Env(0.02f, 4f), Wave.Sine, 0.15f);
            return Normalize(b, 0.4f);
        }

        /// <summary>Plastic capsule rattling down a chute, then a little thunk.</summary>
        private static float[] CapsuleRattle()
        {
            float[] b = Buf(0.8f);
            for (int i = 0; i < 6; i++)
                Tone(b, i * 0.08f, 0.05f, t => 1400f - i * 120f, Env(0.002f, 6f), Wave.Triangle, 0.35f);
            Tone(b, 0.55f, 0.2f, t => 180f - t * 300f, Env(0.005f, 4f), Wave.Sine, 0.8f);
            return Normalize(b, 0.6f);
        }

        private static float[] Slide(float from, float to, float dur)
        {
            float[] b = Buf(dur + 0.05f);
            Tone(b, 0, dur, t => Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / dur)), Env(0.08f, 1.5f), Wave.Sine, 1f, 9f, 0.02f);
            return Normalize(b, 0.6f);
        }

        private static float[] Arp(int[] notes, float step, Wave w, float vol)
        {
            float[] b = Buf(step * notes.Length + 0.4f);
            for (int i = 0; i < notes.Length; i++)
            {
                float f = MidiToHz(notes[i]);
                Tone(b, i * step, 0.35f, t => f, Env(0.02f, 2.5f), w, 1f);
                Tone(b, i * step, 0.35f, t => f * 2f, Env(0.02f, 3f), Wave.Sine, 0.2f);
            }
            return Normalize(b, vol + 0.2f);
        }

        private static float[] Jingle()
        {
            float[] b = Buf(1.1f);
            int[] notes = { 72, 76, 79, 84 };
            for (int i = 0; i < notes.Length; i++)
            {
                float f = MidiToHz(notes[i]);
                Tone(b, i * 0.1f, 0.5f, t => f, Env(0.01f, 3f), Wave.Sine, 1f);
                Tone(b, i * 0.1f, 0.3f, t => f * 3.98f, Env(0.01f, 4f), Wave.Sine, 0.25f); // marimba-ish partial
            }
            foreach (int n in new[] { 72, 76, 79 })
            {
                float f = MidiToHz(n + 12);
                Tone(b, 0.45f, 0.6f, t => f, Env(0.01f, 2.5f), Wave.Triangle, 0.45f);
            }
            return Normalize(b, 0.75f);
        }

        private static float[] Fanfare()
        {
            float[] b = Buf(1.8f);
            int[] notes = { 67, 72, 76, 79 };
            float[] times = { 0f, 0.12f, 0.24f, 0.36f };
            for (int i = 0; i < notes.Length; i++)
            {
                float f = MidiToHz(notes[i]);
                Tone(b, times[i], i == 3 ? 0.9f : 0.2f, t => f, Env(0.03f, 1.5f), Wave.Triangle, 0.8f, 6f, 0.008f);
            }
            foreach (int n in new[] { 60, 64, 67, 72 })
            {
                float f = MidiToHz(n);
                Tone(b, 0.5f, 1.2f, t => f, Env(0.05f, 2f), Wave.Triangle, 0.35f);
            }
            for (int k = 0; k < 10; k++)
            {
                float f = MidiToHz(84 + (k % 4) * 3);
                Tone(b, 0.6f + k * 0.07f, 0.25f, t => f, Env(0.01f, 3f), Wave.Sine, 0.25f);
            }
            return Normalize(b, 0.75f);
        }

        private static float[] ThankYou()
        {
            float[] b = Buf(0.6f);
            Tone(b, 0, 0.22f, t => Mathf.Lerp(520f, 640f, t / 0.22f), Env(0.1f, 1.2f), Wave.Triangle, 1f, 7f, 0.03f);
            Tone(b, 0.24f, 0.32f, t => Mathf.Lerp(880f, 760f, t / 0.32f), Env(0.08f, 1.6f), Wave.Triangle, 1f, 7f, 0.03f);
            Tone(b, 0.24f, 0.32f, t => Mathf.Lerp(1760f, 1520f, t / 0.32f), Env(0.08f, 2f), Wave.Sine, 0.2f);
            return Normalize(b, 0.65f);
        }

        private static float[] Munch()
        {
            float[] b = Buf(0.3f);
            NoiseBurst(b, 0, 0.08f, k => 1f - k, 1f);
            NoiseBurst(b, 0.13f, 0.09f, k => 1f - k, 0.9f);
            LowPass(b, 1800f);
            Tone(b, 0, 0.08f, t => 160f, Env(0.05f, 2f), Wave.Sine, 0.6f);
            Tone(b, 0.13f, 0.08f, t => 140f, Env(0.05f, 2f), Wave.Sine, 0.6f);
            return Normalize(b, 0.6f);
        }

        private static float[] Happy()
        {
            float[] b = Buf(0.5f);
            Tone(b, 0, 0.45f, t => Mathf.Lerp(600f, 1150f, Mathf.SmoothStep(0, 1, t / 0.45f)), Env(0.1f, 1.4f), Wave.Triangle, 1f, 11f, 0.05f);
            Tone(b, 0, 0.45f, t => Mathf.Lerp(1200f, 2300f, t / 0.45f), Env(0.1f, 2f), Wave.Sine, 0.15f);
            return Normalize(b, 0.6f);
        }

        private static float[] ButtonTap()
        {
            float[] b = Buf(0.1f);
            Tone(b, 0, 0.07f, t => 1250f, Env(0.02f, 4f), Wave.Sine, 1f);
            Tone(b, 0, 0.05f, t => 620f, Env(0.02f, 4f), Wave.Triangle, 0.4f);
            return Normalize(b, 0.55f);
        }

        private static float[] Sparkle()
        {
            float[] b = Buf(0.8f);
            for (int k = 0; k < 8; k++)
            {
                float f = MidiToHz(88 + k * 2);
                Tone(b, k * 0.05f, 0.35f, t => f, Env(0.01f, 3f), Wave.Sine, 0.6f);
            }
            return Normalize(b, 0.5f);
        }

        private static float[] BalloonPop()
        {
            float[] b = Buf(0.25f);
            NoiseBurst(b, 0, 0.06f, k => 1f - k, 1f);
            Tone(b, 0, 0.12f, t => 180f - t * 600f, Env(0.02f, 2f), Wave.Sine, 0.7f);
            return Normalize(b, 0.7f);
        }

        private static float[] DoorChime()
        {
            float[] b = Buf(1.4f);
            float[] a = Bell(783.99f, 0.8f, 0.6f);
            float[] c = Bell(659.25f, 0.9f, 0.6f);
            for (int i = 0; i < a.Length && i < b.Length; i++) b[i] += a[i];
            int off = (int)(0.32f * Rate);
            for (int i = 0; i < c.Length && off + i < b.Length; i++) b[off + i] += c[i];
            return Normalize(b, 0.55f);
        }

        private static float[] Boing()
        {
            float[] b = Buf(0.4f);
            Tone(b, 0, 0.38f, t => 220f + Mathf.Sin(t * 60f) * 60f * (1f - t / 0.38f) + t * 300f, Env(0.03f, 1.5f), Wave.Sine, 1f);
            return Normalize(b, 0.5f);
        }

        private static float[] Footstep()
        {
            float[] b = Buf(0.1f);
            Tone(b, 0, 0.08f, t => 120f, Env(0.05f, 3f), Wave.Sine, 1f);
            NoiseBurst(b, 0, 0.03f, k => 1f - k, 0.2f);
            return Normalize(b, 0.4f);
        }

        /// <summary>Crossfades the tail into the head and drops the tail, giving a click-free loop point.</summary>
        private static float[] MakeLoop(float[] b, int fade)
        {
            fade = Mathf.Min(fade, b.Length / 3);
            int len = b.Length - fade;
            var o = new float[len];
            Array.Copy(b, o, len);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                o[i] = b[i] * k + b[len + i] * (1f - k);
            }
            return o;
        }

        // ------------------------------------------------------------------ music
        private static readonly int[][] s_Melody =
        {
            new[] { 76, 1, 76, 1, 79, 2, 76, 1, 74, 1, 72, 2 },
            new[] { 72, 1, 74, 1, 76, 2, 72, 2, 69, 2 },
            new[] { 72, 1, 72, 1, 77, 2, 76, 1, 74, 1, 72, 2 },
            new[] { 74, 1, 76, 1, 74, 1, 71, 1, 67, 4 },
            new[] { 76, 1, 79, 1, 84, 2, 81, 1, 79, 1, 76, 2 },
            new[] { 76, 1, 77, 1, 76, 2, 72, 2, 69, 2 },
            new[] { 77, 1, 77, 1, 76, 1, 74, 1, 72, 2, 74, 2 },
            new[] { 71, 2, 74, 2, 72, 4 }
        };

        // C, Am, F, G (root, third, fifth)
        private static readonly int[][] s_Chords =
        {
            new[] { 48, 52, 55 }, new[] { 45, 48, 52 }, new[] { 41, 45, 48 }, new[] { 43, 47, 50 }
        };

        /// <summary>16-bar loop: melody for 8 bars, then a music-box arpeggio variation.</summary>
        private static float[] Song(float bpm, bool gentle)
        {
            float beat = 60f / bpm;
            float eighth = beat * 0.5f;
            float bar = beat * 4f;
            int bars = 16;
            float[] b = Buf(bar * bars);

            for (int bi = 0; bi < bars; bi++)
            {
                float t0 = bi * bar;
                int[] chord = s_Chords[bi % 4];

                // bass: root on 1 and 3, fifth on 2 and 4 (plucky sine)
                for (int q = 0; q < 4; q++)
                {
                    int n = q % 2 == 0 ? chord[0] - 12 : chord[2] - 12;
                    float f = MidiToHz(n);
                    Tone(b, t0 + q * beat, beat * 0.9f, t => f, Env(0.02f, 2.2f), Wave.Sine, gentle ? 0.32f : 0.42f);
                    Tone(b, t0 + q * beat, beat * 0.6f, t => f * 2f, Env(0.02f, 3f), Wave.Triangle, 0.08f);
                }

                // soft pad chord
                foreach (int n in chord)
                {
                    float f = MidiToHz(n + 12);
                    Tone(b, t0, bar, t => f, k => Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 0.9f, Wave.Triangle, gentle ? 0.06f : 0.05f, 4f, 0.004f);
                }

                // percussion (restaurant loop only): soft kick, light shaker
                if (!gentle)
                {
                    for (int q = 0; q < 4; q++)
                    {
                        if (q % 2 == 0) Tone(b, t0 + q * beat, 0.16f, t => Mathf.Lerp(140f, 50f, t / 0.16f), Env(0.01f, 3f), Wave.Sine, 0.35f);
                        NoiseHat(b, t0 + q * beat + eighth, 0.05f, 0.05f);
                        NoiseHat(b, t0 + q * beat, 0.03f, 0.025f);
                    }
                }

                if (bi < 8)
                {
                    // lead melody
                    int[] m = s_Melody[bi];
                    float tt = t0;
                    for (int i = 0; i < m.Length; i += 2)
                    {
                        float f = MidiToHz(m[i] - (gentle ? 0 : 0));
                        float d = m[i + 1] * eighth;
                        if (gentle)
                        {
                            Tone(b, tt, d + 0.3f, t => f, Env(0.01f, 2.5f), Wave.Sine, 0.3f);
                            Tone(b, tt, d * 0.6f, t => f * 3f, Env(0.01f, 4f), Wave.Sine, 0.06f);
                        }
                        else
                        {
                            Tone(b, tt, d * 0.95f, t => f, Env(0.04f, 1.2f), Wave.Triangle, 0.3f, 5.5f, 0.006f);
                            Tone(b, tt, d * 0.8f, t => f * 2f, Env(0.04f, 2f), Wave.Sine, 0.05f);
                        }
                        tt += d;
                    }
                }
                else
                {
                    // music-box arpeggio variation
                    int[] pattern = { 0, 1, 2, 1, 0, 1, 2, 1 };
                    for (int i = 0; i < 8; i++)
                    {
                        float f = MidiToHz(chord[pattern[i]] + 24 + (i == 4 ? 12 : 0));
                        Tone(b, t0 + i * eighth, eighth * 2.5f, t => f, Env(0.01f, 3f), Wave.Sine, gentle ? 0.18f : 0.16f);
                    }
                }
            }
            if (!gentle) LowPass(b, 7000f);
            return Normalize(b, 0.8f, false);
        }

        private static void NoiseHat(float[] b, float start, float dur, float vol)
        {
            int s0 = (int)(start * Rate);
            int n = (int)(dur * Rate);
            float prev = 0f;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float x = Noise();
                float hp = x - prev; // crude high-pass
                prev = x;
                b[s0 + i] += hp * Mathf.Pow(1f - i / (float)n, 3f) * vol;
            }
        }

        // ------------------------------------------------------------------ WAV writer
        private static void WriteWav(string path, float[] samples)
        {
            using var fs = new FileStream(path, FileMode.Create);
            using var w = new BinaryWriter(fs);
            int byteRate = Rate * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + samples.Length * 2);
            w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(Rate);
            w.Write(byteRate);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(samples.Length * 2);
            foreach (float s in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
        }
    }
}
