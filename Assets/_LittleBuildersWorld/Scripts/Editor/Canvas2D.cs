using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>A 2D shape described by a signed distance (pixels, negative inside) and a bounding box.</summary>
    public sealed class Shape
    {
        public Func<Vector2, float> Dist;
        public Rect Bounds;

        public Shape(Func<Vector2, float> d, Rect b) { Dist = d; Bounds = b; }

        public static Shape Circle(float cx, float cy, float r) =>
            new Shape(p => Vector2.Distance(p, new Vector2(cx, cy)) - r, new Rect(cx - r, cy - r, r * 2, r * 2));

        public static Shape Ellipse(float cx, float cy, float rx, float ry, float rotDeg = 0f)
        {
            float a = rotDeg * Mathf.Deg2Rad;
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float m = Mathf.Max(rx, ry);
            return new Shape(p =>
            {
                float x = p.x - cx, y = p.y - cy;
                float lx = x * ca + y * sa, ly = -x * sa + y * ca;
                float k = Mathf.Sqrt((lx * lx) / (rx * rx) + (ly * ly) / (ry * ry));
                return (k - 1f) * Mathf.Min(rx, ry);
            }, new Rect(cx - m, cy - m, m * 2, m * 2));
        }

        public static Shape RoundRect(float cx, float cy, float hw, float hh, float r, float rotDeg = 0f)
        {
            float a = rotDeg * Mathf.Deg2Rad;
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            r = Mathf.Min(r, Mathf.Min(hw, hh));
            float ext = Mathf.Sqrt(hw * hw + hh * hh);
            return new Shape(p =>
            {
                float x = p.x - cx, y = p.y - cy;
                float lx = Mathf.Abs(x * ca + y * sa), ly = Mathf.Abs(-x * sa + y * ca);
                float qx = lx - hw + r, qy = ly - hh + r;
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            }, new Rect(cx - ext, cy - ext, ext * 2, ext * 2));
        }

        public static Shape Capsule(float ax, float ay, float bx, float by, float r)
        {
            var A = new Vector2(ax, ay);
            var B = new Vector2(bx, by);
            return new Shape(p =>
            {
                Vector2 pa = p - A, ba = B - A;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
                return (pa - ba * h).magnitude - r;
            }, Rect.MinMaxRect(Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r));
        }

        public static Shape Polygon(params Vector2[] pts)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (Vector2 v in pts) { minX = Mathf.Min(minX, v.x); minY = Mathf.Min(minY, v.y); maxX = Mathf.Max(maxX, v.x); maxY = Mathf.Max(maxY, v.y); }
            return new Shape(p =>
            {
                float d = Vector2.Dot(p - pts[0], p - pts[0]);
                float s = 1f;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i, i++)
                {
                    Vector2 e = pts[j] - pts[i];
                    Vector2 w = p - pts[i];
                    Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                    d = Mathf.Min(d, Vector2.Dot(b, b));
                    bool c1 = p.y >= pts[i].y, c2 = p.y < pts[j].y, c3 = e.x * w.y > e.y * w.x;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                }
                return s * Mathf.Sqrt(d);
            }, Rect.MinMaxRect(minX, minY, maxX, maxY));
        }

        public static Shape Star(float cx, float cy, float ro, float ri, int points, float rotDeg = 90f)
        {
            var pts = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float a = (rotDeg + i * 180f / points) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? ro : ri;
                pts[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return Polygon(pts);
        }

        public static Shape Heart(float cx, float cy, float size)
        {
            Shape a = Circle(cx - size * 0.5f, cy + size * 0.25f, size * 0.55f);
            Shape b = Circle(cx + size * 0.5f, cy + size * 0.25f, size * 0.55f);
            Shape c = Polygon(new Vector2(cx - size * 1.02f, cy + size * 0.05f), new Vector2(cx + size * 1.02f, cy + size * 0.05f),
                new Vector2(cx, cy - size * 1.05f));
            return Union(a, b, c).Round(size * 0.05f);
        }

        public static Shape Ring(float cx, float cy, float r, float thickness) =>
            Circle(cx, cy, r).Annular(thickness);

        public static Shape Union(params Shape[] s)
        {
            Rect b = s[0].Bounds;
            foreach (Shape x in s) b = Rect.MinMaxRect(Mathf.Min(b.xMin, x.Bounds.xMin), Mathf.Min(b.yMin, x.Bounds.yMin),
                Mathf.Max(b.xMax, x.Bounds.xMax), Mathf.Max(b.yMax, x.Bounds.yMax));
            return new Shape(p =>
            {
                float d = float.MaxValue;
                foreach (Shape x in s) d = Mathf.Min(d, x.Dist(p));
                return d;
            }, b);
        }

        public Shape Subtract(Shape other) => new Shape(p => Mathf.Max(Dist(p), -other.Dist(p)), Bounds);
        public Shape Union(Shape other) => Union(this, other);
        public Shape Intersect(Shape other) => new Shape(p => Mathf.Max(Dist(p), other.Dist(p)), Bounds);
        public Shape Round(float r) => new Shape(p => Dist(p) - r, Grow(Bounds, r));
        public Shape Annular(float thickness) => new Shape(p => Mathf.Abs(Dist(p)) - thickness * 0.5f, Grow(Bounds, thickness));

        public Shape Offset(float dx, float dy)
        {
            var o = new Vector2(dx, dy);
            Func<Vector2, float> d = Dist;
            return new Shape(p => d(p - o), new Rect(Bounds.x + dx, Bounds.y + dy, Bounds.width, Bounds.height));
        }

        private static Rect Grow(Rect r, float g) => new Rect(r.x - g, r.y - g, r.width + g * 2, r.height + g * 2);
    }

    /// <summary>Tiny anti-aliased rasterizer used to paint all original 2D art (icons, stickers, textures).</summary>
    public sealed class Canvas2D
    {
        public readonly int Width;
        public readonly int Height;
        private readonly Color[] m_Px;
        private readonly Stack<(Vector2 o, float s)> m_Stack = new Stack<(Vector2, float)>();
        private Vector2 m_Offset = Vector2.zero;
        private float m_Scale = 1f;

        public static readonly Color Outline = new Color(0.35f, 0.22f, 0.15f, 1f);

        public Canvas2D(int w, int h, Color? background = null)
        {
            Width = w;
            Height = h;
            m_Px = new Color[w * h];
            Color bg = background ?? new Color(1f, 1f, 1f, 0f);
            for (int i = 0; i < m_Px.Length; i++) m_Px[i] = bg;
        }

        /// <summary>Draw subsequent shapes (authored in a 256×256 space) at an offset/scale.</summary>
        public void Push(float ox, float oy, float scale)
        {
            m_Stack.Push((m_Offset, m_Scale));
            m_Offset = new Vector2(m_Offset.x + ox * m_Scale, m_Offset.y + oy * m_Scale);
            m_Scale *= scale;
        }

        public void Pop()
        {
            (Vector2 o, float s) = m_Stack.Pop();
            m_Offset = o;
            m_Scale = s;
        }

        public void Fill(Shape s, Color c, float soften = 0f)
        {
            Rect b = s.Bounds;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(m_Offset.x + (b.xMin - 2 - soften) * m_Scale));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(m_Offset.y + (b.yMin - 2 - soften) * m_Scale));
            int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(m_Offset.x + (b.xMax + 2 + soften) * m_Scale));
            int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(m_Offset.y + (b.yMax + 2 + soften) * m_Scale));
            float inv = 1f / m_Scale;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2((x + 0.5f - m_Offset.x) * inv, (y + 0.5f - m_Offset.y) * inv);
                    float d = s.Dist(p) * m_Scale; // to pixels
                    float cov = soften > 0f ? Mathf.Clamp01(0.5f - d / (soften * m_Scale)) : Mathf.Clamp01(0.5f - d);
                    if (cov <= 0f) continue;
                    Blend(x, y, c, cov);
                }
            }
        }

        /// <summary>Fills with a dark cartoon outline around the shape.</summary>
        public void FillOutlined(Shape s, Color c, float outline = 6f, Color? outlineColor = null)
        {
            Fill(s.Round(outline), outlineColor ?? Outline);
            Fill(s, c);
        }

        public void Stroke(Shape s, float width, Color c) => Fill(s.Annular(width), c);

        /// <summary>Soft radial gradient (glows, particles).</summary>
        public void RadialGlow(float cx, float cy, float r, Color c, float power = 2f)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(m_Offset.x + (cx - r) * m_Scale));
            int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(m_Offset.x + (cx + r) * m_Scale));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(m_Offset.y + (cy - r) * m_Scale));
            int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(m_Offset.y + (cy + r) * m_Scale));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f - m_Offset.x) / m_Scale, py = (y + 0.5f - m_Offset.y) / m_Scale;
                    float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy)) / r;
                    if (d >= 1f) continue;
                    Blend(x, y, c, Mathf.Pow(1f - d, power));
                }
            }
        }

        private void Blend(int x, int y, Color c, float cov)
        {
            int i = y * Width + x;
            Color dst = m_Px[i];
            float a = c.a * cov;
            float outA = a + dst.a * (1f - a);
            if (outA <= 0f) { m_Px[i] = new Color(0, 0, 0, 0); return; }
            m_Px[i] = new Color(
                (c.r * a + dst.r * dst.a * (1f - a)) / outA,
                (c.g * a + dst.g * dst.a * (1f - a)) / outA,
                (c.b * a + dst.b * dst.a * (1f - a)) / outA,
                outA);
        }

        public byte[] ToPng()
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            // Fill transparent pixels' colour from neighbours so bilinear filtering has no dark fringes.
            var px = (Color[])m_Px.Clone();
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a > 0f) continue;
                int x = i % Width, y = i / Width;
                for (int dy = -2; dy <= 2 && px[i].a <= 0f; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                        Color n = m_Px[ny * Width + nx];
                        if (n.a > 0.2f) { px[i] = new Color(n.r, n.g, n.b, 0f); break; }
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            return png;
        }
    }
}
