using System.Collections.Generic;
using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Progression;
using LittleBuildersWorld.Vehicles;
using UnityEditor;
using UnityEngine;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>ScriptableObject data: vehicle profiles (all handling numbers live here), unlocks, paints, audio, config.</summary>
    public static class DataBuilder
    {
        public const string VehicleData = E.Root + "/ScriptableObjects/Vehicles";
        public const string ConfigPath = E.Root + "/Resources/" + GameConfig.ResourcePath + ".asset";
        public const string AudioPath = E.Root + "/ScriptableObjects/Audio/AudioLibrary.asset";
        public const string GuidePath = E.Root + "/ScriptableObjects/Audio/GuideLibrary.asset";
        public const string VoiceFolder = E.Root + "/Audio/Voice";
        public const string VoiceLinesPath = "Tools/voice/voice_lines.json";

        [System.Serializable]
        private sealed class VoiceLinesFile
        {
            public string[] languages;
            public VoiceLine[] lines;
        }

        [System.Serializable]
        private sealed class VoiceLine
        {
            public string key;
            public string en;
            public string th;
        }

        /// <summary>
        /// The helper's words and voice: reads Tools/voice/voice_lines.json (the single source) and pairs each line
        /// with its generated clip in Audio/Voice/&lt;lang&gt;/&lt;key&gt;.wav (made offline by Tools/voice/generate_voice.sh).
        /// Also returns every Thai character used, for the font atlas.
        /// </summary>
        public static Guide.GuideLibrary BuildGuide(out string thaiCharacters)
        {
            thaiCharacters = "";
            var lib = E.LoadOrCreateSO<Guide.GuideLibrary>(GuidePath);
            if (!System.IO.File.Exists(VoiceLinesPath))
            {
                Debug.LogError("[LBW] Missing " + VoiceLinesPath);
                return lib;
            }
            var file = JsonUtility.FromJson<VoiceLinesFile>(System.IO.File.ReadAllText(VoiceLinesPath));
            var lines = new List<Guide.GuideLibrary.Line>();
            var thai = new HashSet<char>();
            int missing = 0;
            foreach (VoiceLine l in file.lines)
            {
                var line = new Guide.GuideLibrary.Line { key = l.key, en = l.en, th = l.th, voiceEn = Voice("en", l.key), voiceTh = Voice("th", l.key) };
                if (line.voiceEn == null) missing++;
                if (!string.IsNullOrEmpty(l.th) && line.voiceTh == null) missing++;
                if (l.th != null) foreach (char ch in l.th) thai.Add(ch);
                lines.Add(line);
            }
            foreach (char ch in "ภาษาไทย") thai.Add(ch);
            if (missing > 0) Debug.LogWarning($"[LBW] {missing} voice clips missing: run Tools/voice/generate_voice.sh (the bubble text still works).");
            lib.lines = lines.ToArray();
            EditorUtility.SetDirty(lib);
            var sb = new System.Text.StringBuilder();
            foreach (char ch in thai) sb.Append(ch);
            thaiCharacters = sb.ToString();
            return lib;
        }

        private static AudioClip Voice(string lang, string key)
        {
            string path = $"{VoiceFolder}/{lang}/{key}.wav";
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) return null;
            // Small and quick: mono Vorbis kept compressed in memory (≈1 MB for every line in both languages).
            AudioImporterSampleSettings st = imp.defaultSampleSettings;
            bool dirty = !imp.forceToMono || st.loadType != AudioClipLoadType.CompressedInMemory || st.compressionFormat != AudioCompressionFormat.Vorbis
                         || Mathf.Abs(st.quality - 0.55f) > 0.01f || imp.loadInBackground;
            if (dirty)
            {
                imp.forceToMono = true;
                imp.loadInBackground = false;
                st.loadType = AudioClipLoadType.CompressedInMemory;
                st.compressionFormat = AudioCompressionFormat.Vorbis;
                st.quality = 0.55f;
                st.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                imp.defaultSampleSettings = st;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        public static Dictionary<VehicleType, VehicleProfile> BuildProfiles()
        {
            var d = new Dictionary<VehicleType, VehicleProfile>();
            AudioClip engine = AudioSynth.Get("sfx_engine_loop");

            d[VehicleType.Excavator] = Profile("Excavator", "excavator", "Excavator", "#FFC93C", 3200f, 0.4f, engine, 0.72f, 1.1f, 0.9f, new DriveTuning
            {
                MaxForwardSpeed = 4.6f, MaxReverseSpeed = 2.2f, Acceleration = 2.6f, ReverseAcceleration = 2f,
                BrakeDeceleration = 8f, CoastDeceleration = 4.5f, TurnRate = 62f, FullTurnSpeed = 1.5f, TurnInPlace = 0.7f, SteerResponse = 4f
            });
            d[VehicleType.DumpTruck] = Profile("DumpTruck", "dumptruck", "Dump Truck", "#FF595E", 2500f, 0.62f, engine, 0.9f, 1.45f, 1.05f, new DriveTuning
            {
                MaxForwardSpeed = 8f, MaxReverseSpeed = 3f, Acceleration = 3.2f, ReverseAcceleration = 2.5f,
                BrakeDeceleration = 9f, CoastDeceleration = 4f, TurnRate = 66f, FullTurnSpeed = 3f, TurnInPlace = 0.25f, SteerResponse = 5f
            });
            d[VehicleType.Bulldozer] = Profile("Bulldozer", "bulldozer", "Bulldozer", "#FF9F1C", 3600f, 0.45f, engine, 0.68f, 1.05f, 0.8f, new DriveTuning
            {
                MaxForwardSpeed = 5f, MaxReverseSpeed = 2.5f, Acceleration = 2.6f, ReverseAcceleration = 2.2f,
                BrakeDeceleration = 8f, CoastDeceleration = 4.5f, TurnRate = 64f, FullTurnSpeed = 1.5f, TurnInPlace = 0.75f, SteerResponse = 4f
            });
            d[VehicleType.RoadRoller] = Profile("RoadRoller", "roller", "Road Roller", "#6BCB77", 3000f, 0.8f, engine, 0.8f, 1.2f, 1.2f, new DriveTuning
            {
                MaxForwardSpeed = 5f, MaxReverseSpeed = 2.5f, Acceleration = 2.5f, ReverseAcceleration = 2.2f,
                BrakeDeceleration = 8f, CoastDeceleration = 4.5f, TurnRate = 58f, FullTurnSpeed = 2f, TurnInPlace = 0.3f, SteerResponse = 4.5f
            });
            d[VehicleType.Excavator].type = VehicleType.Excavator;
            d[VehicleType.DumpTruck].type = VehicleType.DumpTruck;
            d[VehicleType.Bulldozer].type = VehicleType.Bulldozer;
            d[VehicleType.RoadRoller].type = VehicleType.RoadRoller;
            foreach (VehicleProfile p in d.Values) EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return d;
        }

        private static VehicleProfile Profile(string file, string id, string display, string colour, float mass, float wheelRadius,
            AudioClip engine, float idlePitch, float topPitch, float hornPitch, DriveTuning tuning)
        {
            var p = E.LoadOrCreateSO<VehicleProfile>($"{VehicleData}/VP_{file}.asset");
            p.id = id;
            p.displayName = display;
            p.markerColour = E.Hex(colour);
            p.tuning = tuning;
            p.mass = mass;
            p.wheelRadius = wheelRadius;
            p.trackScroll = 1f / (0.35f * 8f);
            p.maxBodyLean = 3.5f;
            p.starsRequired = 0; // every core vehicle is available from the start (no grinding)
            p.engineLoop = engine;
            p.engineIdlePitch = idlePitch;
            p.engineTopPitch = topPitch;
            p.hornPitch = hornPitch;
            return p;
        }

        /// <summary>Reloads profiles from disk (scene changes in batch mode unload cached asset handles).</summary>
        public static Dictionary<VehicleType, VehicleProfile> LoadProfiles()
        {
            var d = new Dictionary<VehicleType, VehicleProfile>();
            foreach (var (t, file) in new[] { (VehicleType.Excavator, "Excavator"), (VehicleType.DumpTruck, "DumpTruck"), (VehicleType.Bulldozer, "Bulldozer"), (VehicleType.RoadRoller, "RoadRoller") })
                d[t] = AssetDatabase.LoadAssetAtPath<VehicleProfile>($"{VehicleData}/VP_{file}.asset");
            return d;
        }

        public static void AssignPortraits(Dictionary<VehicleType, VehicleProfile> profiles, Dictionary<VehicleType, Sprite> portraits)
        {
            foreach (var kv in profiles)
            {
                if (portraits.TryGetValue(kv.Key, out Sprite s)) kv.Value.portrait = s;
                EditorUtility.SetDirty(kv.Value);
            }
            AssetDatabase.SaveAssets();
        }

        public static AudioLibrary BuildAudio()
        {
            var lib = E.LoadOrCreateSO<AudioLibrary>(AudioPath);
            (SfxId id, string clip, float vol)[] map =
            {
                (SfxId.ButtonTap, "sfx_button", 0.6f), (SfxId.Pop, "sfx_pop", 0.6f), (SfxId.Snap, "sfx_snap", 0.7f),
                (SfxId.Whoosh, "sfx_whoosh", 0.6f), (SfxId.Star, "sfx_star", 0.7f), (SfxId.Celebration, "sfx_celebration", 0.75f),
                (SfxId.Sparkle, "sfx_sparkle", 0.55f), (SfxId.Horn, "sfx_horn", 0.6f), (SfxId.Hydraulic, "sfx_hydraulic", 0.45f),
                (SfxId.SoilDump, "sfx_soil_dump", 0.7f), (SfxId.Scoop, "sfx_scoop", 0.65f), (SfxId.Load, "sfx_load", 0.7f),
                (SfxId.BedTip, "sfx_bed_tip", 0.5f), (SfxId.Thud, "sfx_thud", 0.7f), (SfxId.Boing, "sfx_boing", 0.6f),
                (SfxId.Unlock, "sfx_unlock", 0.75f), (SfxId.EngineStart, "sfx_engine_start", 0.6f)
            };
            var entries = new List<AudioLibrary.Entry>();
            foreach (var m in map)
            {
                AudioClip c = AudioSynth.Get(m.clip);
                if (c == null) Debug.LogError("[LBW] Missing clip " + m.clip);
                entries.Add(new AudioLibrary.Entry { id = m.id, clip = c, volume = m.vol });
            }
            lib.sfx = entries.ToArray();
            lib.menuMusic = AudioSynth.Get("music_menu_loop");
            lib.townMusic = AudioSynth.Get("music_town_loop");
            lib.engineLoop = AudioSynth.Get("sfx_engine_loop");
            EditorUtility.SetDirty(lib);
            return lib;
        }

        public static GameConfig BuildConfig(Dictionary<VehicleType, VehicleProfile> profiles, AudioLibrary audio, Guide.GuideLibrary guide)
        {
            var cfg = E.LoadOrCreateSO<GameConfig>(ConfigPath);
            cfg.vehicles = new[] { profiles[VehicleType.Excavator], profiles[VehicleType.DumpTruck], profiles[VehicleType.Bulldozer], profiles[VehicleType.RoadRoller] };
            cfg.audio = audio;
            cfg.iconScoop = IconPainter.Sprite("icon_scoop");
            cfg.iconDump = IconPainter.Sprite("icon_dump");
            cfg.iconLoad = IconPainter.Sprite("icon_load");
            cfg.iconUnload = IconPainter.Sprite("icon_unload");
            cfg.iconStar = IconPainter.Sprite("icon_star");
            cfg.guide = guide;
            cfg.mascot = IconPainter.Sprite("mascot_foreman");
            cfg.iconHouse = IconPainter.Sprite("icon_house");
            cfg.iconRoad = IconPainter.Sprite("icon_road");
            cfg.iconPlayground = IconPainter.Sprite("icon_slide");
            cfg.iconExit = IconPainter.Sprite("icon_exit");
            cfg.iconReset = IconPainter.Sprite("icon_reset");
            cfg.iconArrow = IconPainter.Sprite("icon_go");
            cfg.targetFrameRate = 60;

            // Stars come from: yard dig pit 1, house 6 stages + 3 bonus, roadworks 3 stages + 3 bonus,
            // playground 5 stages + 3 bonus = 24.
            Sprite paint = IconPainter.Sprite("icon_paint");
            cfg.unlocks = new[]
            {
                new UnlockDefinition { id = "paint_sunny", kind = UnlockKind.Paint, starsRequired = 2, icon = paint, colour = E.Hex("#FFD447") },
                new UnlockDefinition { id = "decor_flags", kind = UnlockKind.YardDecoration, starsRequired = 4, icon = IconPainter.Sprite("icon_flags"), colour = E.Hex("#FF595E") },
                new UnlockDefinition { id = "area_playground", kind = UnlockKind.Area, starsRequired = 6, icon = IconPainter.Sprite("icon_slide"), colour = E.Hex("#6BCB77") },
                new UnlockDefinition { id = "paint_ocean", kind = UnlockKind.Paint, starsRequired = 8, icon = paint, colour = E.Hex("#4EA8DE") },
                new UnlockDefinition { id = "decor_balloons", kind = UnlockKind.YardDecoration, starsRequired = 11, icon = IconPainter.Sprite("icon_balloons"), colour = E.Hex("#FFC93C") },
                new UnlockDefinition { id = "paint_candy", kind = UnlockKind.Paint, starsRequired = 15, icon = paint, colour = E.Hex("#FF8FC7") },
                new UnlockDefinition { id = "paint_forest", kind = UnlockKind.Paint, starsRequired = 19, icon = paint, colour = E.Hex("#2A9D8F") },
                new UnlockDefinition { id = "decor_rainbow", kind = UnlockKind.YardDecoration, starsRequired = 23, icon = IconPainter.Sprite("icon_rainbow"), colour = E.Hex("#FF9F1C") }
            };
            cfg.paints = new[]
            {
                new PaintScheme { id = "factory", unlockId = "", primary = Color.white, secondary = Color.white },
                new PaintScheme { id = "sunny", unlockId = "paint_sunny", primary = E.Hex("#FFD447"), secondary = E.Hex("#FFFFFF") },
                new PaintScheme { id = "ocean", unlockId = "paint_ocean", primary = E.Hex("#4EA8DE"), secondary = E.Hex("#9BF6FF") },
                new PaintScheme { id = "candy", unlockId = "paint_candy", primary = E.Hex("#FF8FC7"), secondary = E.Hex("#B388EB") },
                new PaintScheme { id = "forest", unlockId = "paint_forest", primary = E.Hex("#2A9D8F"), secondary = E.Hex("#E9F5DB") }
            };
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            return cfg;
        }
    }
}
