using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Paints every 2D asset: HUD icons (chunky shapes with a dark outline so they read on any background), UI
    /// shapes (circle, rounded panel, ring) and tiling textures (grass, sand, road, tracks). All original.
    /// </summary>
    public static class IconPainter
    {
        public const string IconFolder = E.Root + "/Art/UI/Icons";
        public const string TextureFolder = E.Root + "/Art/Textures";

        public static readonly Color Ink = E.Hex("#3A2E2A");
        public static readonly Color White = Color.white;
        public static readonly Color Yellow = E.Hex("#FFC93C");
        public static readonly Color Orange = E.Hex("#FF9F1C");
        public static readonly Color Red = E.Hex("#FF595E");
        public static readonly Color Green = E.Hex("#6BCB77");
        public static readonly Color Blue = E.Hex("#1982C4");
        public static readonly Color Sky = E.Hex("#8AD8FF");
        public static readonly Color Soil = E.Hex("#8B5A3C");
        public static readonly Color Steel = E.Hex("#9AA5B1");
        public static readonly Color Brick = E.Hex("#E76F51");

        private static readonly Dictionary<string, Action<Canvas2D>> Icons = new Dictionary<string, Action<Canvas2D>>
        {
            { "icon_left", c => Arrow(c, 180f) },
            { "icon_right", c => Arrow(c, 0f) },
            { "icon_go", c => Arrow(c, 90f) },
            { "icon_back", c => Arrow(c, 270f) },
            { "icon_exit", Exit },
            { "icon_horn", Horn },
            { "icon_reset", ResetIcon },
            { "icon_home", Home },
            { "icon_paint", Paint },
            { "icon_gear", Gear },
            { "icon_star", c => c.FillOutlined(Shape.Star(128, 124, 112, 52, 5), Yellow, 10) },
            { "icon_lock", Lock },
            { "icon_play", c => c.FillOutlined(Shape.Polygon(new Vector2(80, 40), new Vector2(214, 128), new Vector2(80, 216)).Round(14), White, 10) },
            { "icon_close", Close },
            { "icon_scoop", Scoop },
            { "icon_dump", Dump },
            { "icon_load", Load },
            { "icon_unload", Unload },
            { "icon_house", House },
            { "icon_road", Road },
            { "icon_slide", Slide },
            { "icon_flags", Flags },
            { "icon_balloons", Balloons },
            { "icon_arrow3d", c => c.Fill(Shape.Polygon(new Vector2(128, 248), new Vector2(240, 120), new Vector2(170, 120), new Vector2(170, 8), new Vector2(86, 8), new Vector2(86, 120), new Vector2(16, 120)).Round(6), White) },
            // UI shapes (tinted at runtime)
            { "ui_circle", c => c.Fill(Shape.Circle(128, 128, 124), White) },
            { "ui_circle_outlined", c => { c.Fill(Shape.Circle(128, 128, 124), Ink); c.Fill(Shape.Circle(128, 128, 110), White); } },
            { "ui_ring", c => c.Fill(Shape.Ring(128, 128, 112, 22), White) },
            { "ui_panel", c => c.Fill(Shape.RoundRect(128, 128, 124, 124, 48), White) },
            { "ui_shadow", c => c.RadialGlow(128, 128, 126, new Color(0, 0, 0, 0.55f), 1.6f) },
            { "fx_spark", c => c.RadialGlow(128, 128, 126, White, 2.2f) },
            { "fx_confetti", c => c.Fill(Shape.RoundRect(128, 128, 90, 60, 20), White) }
        };

        public static void GenerateAll()
        {
            E.EnsureFolder(IconFolder);
            E.EnsureFolder(TextureFolder);
            foreach (var kv in Icons)
            {
                var c = new Canvas2D(256, 256);
                kv.Value(c);
                Write($"{IconFolder}/{kv.Key}.png", c.ToPng());
            }
            WriteTextures();
            AssetDatabase.Refresh();
            foreach (var kv in Icons) ConfigureSprite($"{IconFolder}/{kv.Key}.png", kv.Key == "ui_panel" ? 60 : 0);
            ConfigureTexture($"{TextureFolder}/tex_grass.png", true);
            ConfigureTexture($"{TextureFolder}/tex_sand.png", true);
            ConfigureTexture($"{TextureFolder}/tex_road.png", true);
            ConfigureTexture($"{TextureFolder}/tex_road_plain.png", true);
            ConfigureTexture($"{TextureFolder}/tex_track.png", true);
            ConfigureTexture($"{TextureFolder}/tex_dirt.png", true);
        }

        public static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{name}.png");
        public static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{name}.png");

        private static void Write(string path, byte[] png) => File.WriteAllBytes(path, png);

        private static void ConfigureSprite(string path, int border)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 256;
            imp.textureCompression = TextureImporterCompression.Compressed;
            if (border > 0) imp.spriteBorder = new Vector4(border, border, border, border);
            imp.SaveAndReimport();
        }

        private static void ConfigureTexture(string path, bool repeat)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Default;
            imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.anisoLevel = 2;
            imp.maxTextureSize = 512;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------ icons
        private static void Arrow(Canvas2D c, float angle)
        {
            Vector2[] pts =
            {
                new Vector2(236, 128), new Vector2(128, 236), new Vector2(128, 172), new Vector2(26, 172),
                new Vector2(26, 84), new Vector2(128, 84), new Vector2(128, 20)
            };
            float a = angle * Mathf.Deg2Rad;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 p = pts[i] - new Vector2(128, 128);
                pts[i] = new Vector2(128 + p.x * Mathf.Cos(a) - p.y * Mathf.Sin(a), 128 + p.x * Mathf.Sin(a) + p.y * Mathf.Cos(a));
            }
            c.FillOutlined(Shape.Polygon(pts).Round(10), White, 10);
        }

        private static void Exit(Canvas2D c)
        {
            c.FillOutlined(Shape.RoundRect(96, 128, 62, 104, 14), E.Hex("#C98B5A"), 9);
            c.Fill(Shape.Circle(132, 128, 9), Ink);
            Shape arrow = Shape.Polygon(new Vector2(244, 128), new Vector2(184, 188), new Vector2(184, 152), new Vector2(124, 152),
                new Vector2(124, 104), new Vector2(184, 104), new Vector2(184, 68)).Round(6);
            c.FillOutlined(arrow, Yellow, 9);
        }

        private static void Horn(Canvas2D c)
        {
            Shape body = Shape.Polygon(new Vector2(40, 108), new Vector2(40, 148), new Vector2(150, 200), new Vector2(150, 56)).Round(10);
            c.FillOutlined(body, Yellow, 9);
            c.FillOutlined(Shape.RoundRect(32, 128, 18, 30, 8), Orange, 8);
            for (int i = 0; i < 2; i++)
            {
                float r = 60 + i * 34;
                Shape arc = Shape.Ring(150, 128, r, 14).Intersect(Shape.Polygon(new Vector2(150, 128), new Vector2(256, 30), new Vector2(256, 226)));
                c.Fill(arc.Round(5), Ink);
                c.Fill(arc, White);
            }
        }

        private static void ResetIcon(Canvas2D c)
        {
            Shape ring = Shape.Ring(128, 128, 82, 34).Subtract(Shape.Polygon(new Vector2(128, 128), new Vector2(256, 190), new Vector2(256, 256), new Vector2(200, 256)));
            c.FillOutlined(ring, White, 9);
            Shape head = Shape.Polygon(new Vector2(176, 236), new Vector2(240, 162), new Vector2(150, 150)).Round(6);
            c.FillOutlined(head, White, 9);
        }

        private static void Home(Canvas2D c)
        {
            c.FillOutlined(Shape.RoundRect(128, 92, 78, 66, 10), E.Hex("#FFE3B3"), 9);
            c.FillOutlined(Shape.Polygon(new Vector2(128, 236), new Vector2(240, 144), new Vector2(16, 144)).Round(10), Red, 9);
            c.FillOutlined(Shape.RoundRect(128, 68, 22, 42, 6), E.Hex("#8B5A3C"), 6);
        }

        private static void Paint(Canvas2D c)
        {
            c.FillOutlined(Shape.Capsule(84, 60, 168, 176, 18), E.Hex("#C98B5A"), 9);
            c.FillOutlined(Shape.RoundRect(186, 200, 34, 40, 14, -36f), Steel, 9);
            c.FillOutlined(Shape.Ellipse(214, 232, 30, 22, -36f), Red, 8);
            c.FillOutlined(Shape.Circle(70, 192, 30), Blue, 8);
            c.FillOutlined(Shape.Circle(56, 120, 22), Yellow, 8);
        }

        private static void Gear(Canvas2D c)
        {
            var teeth = new List<Shape> { Shape.Circle(128, 128, 82) };
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                teeth.Add(Shape.RoundRect(128 + Mathf.Cos(a) * 92, 128 + Mathf.Sin(a) * 92, 22, 26, 8, i * 45f));
            }
            Shape gear = Shape.Union(teeth.ToArray()).Subtract(Shape.Circle(128, 128, 34));
            c.FillOutlined(gear, Steel, 9);
        }

        private static void Lock(Canvas2D c)
        {
            c.FillOutlined(Shape.Ring(128, 160, 52, 26).Subtract(Shape.RoundRect(128, 100, 120, 60, 1)), Steel, 9);
            c.FillOutlined(Shape.RoundRect(128, 96, 84, 70, 18), Yellow, 9);
            c.Fill(Shape.Circle(128, 104, 14), Ink);
            c.Fill(Shape.RoundRect(128, 82, 6, 18, 3), Ink);
        }

        private static void Close(Canvas2D c)
        {
            Shape x = Shape.Union(Shape.Capsule(60, 60, 196, 196, 24), Shape.Capsule(60, 196, 196, 60, 24));
            c.FillOutlined(x, White, 9);
        }

        private static Shape BucketShape(float cx, float cy, float rot)
        {
            return Shape.Polygon(Rot(new[] { new Vector2(-80, 50), new Vector2(80, 50), new Vector2(60, -50), new Vector2(-50, -60) }, cx, cy, rot)).Round(12);
        }

        private static Vector2[] Rot(Vector2[] pts, float cx, float cy, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            var o = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++)
                o[i] = new Vector2(cx + pts[i].x * Mathf.Cos(a) - pts[i].y * Mathf.Sin(a), cy + pts[i].x * Mathf.Sin(a) + pts[i].y * Mathf.Cos(a));
            return o;
        }

        private static void Scoop(Canvas2D c)
        {
            c.FillOutlined(Shape.Ellipse(128, 156, 84, 46), Soil, 9);
            c.FillOutlined(BucketShape(128, 110, 0f), Yellow, 9);
            for (int i = 0; i < 4; i++) c.FillOutlined(Shape.RoundRect(76 + i * 32, 46, 9, 16, 4), Steel, 6);
            c.Fill(Shape.Circle(100, 176, 12), E.Hex("#A06B47"));
            c.Fill(Shape.Circle(150, 184, 10), E.Hex("#A06B47"));
        }

        private static void Dump(Canvas2D c)
        {
            c.FillOutlined(BucketShape(110, 170, 150f), Yellow, 9);
            for (int i = 0; i < 4; i++) c.FillOutlined(Shape.Circle(120 + i * 22 - (i % 2) * 18, 80 - i * 16, 18 - i * 2), Soil, 7);
            c.FillOutlined(Shape.Ellipse(150, 30, 80, 22), Soil, 7);
        }

        private static void Bed(Canvas2D c, float cx, float cy, float rot, Color colour)
        {
            Shape bed = Shape.Polygon(Rot(new[] { new Vector2(-100, 40), new Vector2(-84, -40), new Vector2(84, -40), new Vector2(100, 40), new Vector2(76, 40), new Vector2(66, -16), new Vector2(-66, -16), new Vector2(-76, 40) }, cx, cy, rot)).Round(6);
            c.FillOutlined(bed, colour, 9);
        }

        private static void Load(Canvas2D c)
        {
            for (int i = 0; i < 3; i++) c.FillOutlined(Shape.RoundRect(86 + i * 42, 84, 18, 14, 4), i == 1 ? Yellow : Brick, 6);
            Bed(c, 128, 60, 0f, Blue);
            Shape arrow = Shape.Polygon(new Vector2(128, 112), new Vector2(190, 170), new Vector2(150, 170), new Vector2(150, 240), new Vector2(106, 240), new Vector2(106, 170), new Vector2(66, 170)).Round(6);
            c.FillOutlined(arrow, Green, 9);
        }

        private static void Unload(Canvas2D c)
        {
            Bed(c, 136, 150, 34f, Blue);
            for (int i = 0; i < 3; i++) c.FillOutlined(Shape.Circle(62 - i * 4, 110 - i * 26, 20 - i * 3), Soil, 7);
            c.FillOutlined(Shape.Ellipse(70, 30, 62, 20), Soil, 7);
            c.FillOutlined(Shape.Circle(200, 70, 26), Ink);
            c.Fill(Shape.Circle(200, 70, 12), Steel);
        }

        private static void House(Canvas2D c)
        {
            c.FillOutlined(Shape.RoundRect(128, 92, 82, 70, 8), E.Hex("#8AD8FF"), 9);
            c.FillOutlined(Shape.Polygon(new Vector2(128, 230), new Vector2(240, 150), new Vector2(16, 150)).Round(8), Red, 9);
            c.FillOutlined(Shape.RoundRect(150, 70, 20, 46, 4), E.Hex("#8B5A3C"), 6);
            c.FillOutlined(Shape.RoundRect(90, 100, 22, 20, 4), White, 6);
        }

        private static void Road(Canvas2D c)
        {
            c.FillOutlined(Shape.Polygon(new Vector2(90, 240), new Vector2(166, 240), new Vector2(236, 16), new Vector2(20, 16)).Round(8), E.Hex("#5A5F6B"), 9);
            for (int i = 0; i < 3; i++) c.Fill(Shape.RoundRect(128, 50 + i * 66, 8 - i, 20 - i * 3, 3), Yellow);
        }

        private static void Slide(Canvas2D c)
        {
            c.FillOutlined(Shape.Capsule(60, 40, 60, 210, 12), Steel, 8);
            for (int i = 0; i < 4; i++) c.FillOutlined(Shape.RoundRect(80, 70 + i * 40, 26, 6, 3), Steel, 6);
            c.FillOutlined(Shape.Capsule(100, 210, 220, 40, 22), Red, 9);
        }

        private static void Flags(Canvas2D c)
        {
            c.Fill(Shape.Capsule(20, 200, 236, 170, 6), Ink);
            Color[] cols = { Red, Yellow, Blue, Green, Orange };
            for (int i = 0; i < 5; i++)
            {
                float x = 40 + i * 44, y = 196 - i * 6;
                c.FillOutlined(Shape.Polygon(new Vector2(x - 20, y), new Vector2(x + 20, y - 4), new Vector2(x, y - 70)).Round(4), cols[i], 6);
            }
        }

        private static void Balloons(Canvas2D c)
        {
            Color[] cols = { Red, Yellow, Blue };
            for (int i = 0; i < 3; i++)
            {
                float x = 70 + i * 58, y = 160 + (i == 1 ? 26 : 0);
                c.Fill(Shape.Capsule(x, y - 50, 128, 20, 3), Ink);
                c.FillOutlined(Shape.Ellipse(x, y, 40, 50), cols[i], 8);
                c.Fill(Shape.Ellipse(x - 12, y + 16, 9, 14), new Color(1, 1, 1, 0.6f));
            }
        }

        // ------------------------------------------------------------------ textures
        private static void WriteTextures()
        {
            var rng = new System.Random(7);
            Write($"{TextureFolder}/tex_grass.png", Speckle(256, E.Hex("#8ED46F"), new[] { E.Hex("#7CC45E"), E.Hex("#A2DE84"), E.Hex("#86CD67") }, 900, 3, 7, rng));
            Write($"{TextureFolder}/tex_sand.png", Speckle(256, E.Hex("#E2CB98"), new[] { E.Hex("#CDB27C"), E.Hex("#EED9AC"), E.Hex("#BFA06A") }, 500, 2, 6, rng));
            Write($"{TextureFolder}/tex_dirt.png", Speckle(256, E.Hex("#B98355"), new[] { E.Hex("#A26F45"), E.Hex("#C99467"), E.Hex("#8E5F3B") }, 600, 2, 7, rng));

            // Road: asphalt, white edge lines, yellow centre dashes. V runs along the road (1 tile = 10 m).
            var road = new Canvas2D(128, 256, E.Hex("#5C616D"));
            road.Fill(Shape.RoundRect(10, 128, 3, 140, 1), new Color(1, 1, 1, 0.9f));
            road.Fill(Shape.RoundRect(118, 128, 3, 140, 1), new Color(1, 1, 1, 0.9f));
            road.Fill(Shape.RoundRect(64, 128, 4, 52, 3), E.Hex("#FFD447"));
            Write($"{TextureFolder}/tex_road.png", AddNoise(road, 0.025f, rng));
            var plain = new Canvas2D(128, 256, E.Hex("#5C616D"));
            Write($"{TextureFolder}/tex_road_plain.png", AddNoise(plain, 0.025f, rng));

            // Tracks: dark rubber with lighter cleats every 1/8 of the texture.
            var track = new Canvas2D(64, 256, E.Hex("#3D3B4F"));
            for (int i = 0; i < 8; i++) track.Fill(Shape.RoundRect(32, 16 + i * 32, 30, 7, 3), E.Hex("#5E5B73"));
            Write($"{TextureFolder}/tex_track.png", track.ToPng());
        }

        private static byte[] Speckle(int size, Color bg, Color[] dots, int count, float rMin, float rMax, System.Random rng)
        {
            var c = new Canvas2D(size, size, bg);
            for (int i = 0; i < count; i++)
            {
                float x = (float)rng.NextDouble() * size, y = (float)rng.NextDouble() * size;
                float r = rMin + (float)rng.NextDouble() * (rMax - rMin);
                Color col = dots[rng.Next(dots.Length)];
                col.a = 0.55f;
                // Draw wrapped copies so the texture tiles seamlessly.
                for (int ox = -1; ox <= 1; ox++)
                for (int oy = -1; oy <= 1; oy++)
                    c.Fill(Shape.Circle(x + ox * size, y + oy * size, r), col, 1.5f);
            }
            return c.ToPng();
        }

        private static byte[] AddNoise(Canvas2D c, float amount, System.Random rng)
        {
            for (int i = 0; i < 400; i++)
            {
                float x = (float)rng.NextDouble() * c.Width, y = (float)rng.NextDouble() * c.Height;
                c.Fill(Shape.Circle(x, y, 1.5f), new Color(1, 1, 1, amount * 3f));
            }
            return c.ToPng();
        }
    }
}
