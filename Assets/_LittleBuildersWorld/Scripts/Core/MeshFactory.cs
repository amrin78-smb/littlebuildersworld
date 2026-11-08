using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleBuildersWorld.Core
{
    /// <summary>
    /// Procedural soft, toy-like meshes: rounded boxes, lathe shapes (buns, cups, pots),
    /// spheres, tori and rings. Used by the Editor builder to bake mesh assets and at runtime for effects.
    /// </summary>
    public static class MeshFactory
    {
        // ------------------------------------------------------------------ Rounded box
        /// <summary>Box with rounded edges/corners. Pivot at centre.</summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int cornerSegments = 4, string name = "RoundedBox")
        {
            Vector3 h = size * 0.5f;
            radius = Mathf.Clamp(radius, 0.001f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) - 0.0001f);
            Vector3 inner = h - Vector3.one * radius;

            float[] xs = AxisSamples(h.x, radius, cornerSegments);
            float[] ys = AxisSamples(h.y, radius, cornerSegments);
            float[] zs = AxisSamples(h.z, radius, cornerSegments);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // 6 faces: each defined by (normal axis, sign), iterating the two other axes.
            AddFace(verts, normals, uvs, tris, xs, zs, (a, b) => new Vector3(a, h.y, b), inner, radius, false); // top
            AddFace(verts, normals, uvs, tris, xs, zs, (a, b) => new Vector3(a, -h.y, b), inner, radius, true); // bottom
            AddFace(verts, normals, uvs, tris, xs, ys, (a, b) => new Vector3(a, b, -h.z), inner, radius, false); // front (-z)
            AddFace(verts, normals, uvs, tris, xs, ys, (a, b) => new Vector3(a, b, h.z), inner, radius, true); // back
            AddFace(verts, normals, uvs, tris, zs, ys, (a, b) => new Vector3(h.x, b, a), inner, radius, false); // right
            AddFace(verts, normals, uvs, tris, zs, ys, (a, b) => new Vector3(-h.x, b, a), inner, radius, true); // left

            return Build(name, verts, normals, uvs, tris);
        }

        private static float[] AxisSamples(float half, float radius, int seg)
        {
            var list = new List<float>();
            for (int i = 0; i <= seg; i++) list.Add(-half + radius * (1f - Mathf.Cos(i / (float)seg * Mathf.PI * 0.5f)));
            for (int i = seg; i >= 0; i--)
            {
                float v = half - radius * (1f - Mathf.Cos(i / (float)seg * Mathf.PI * 0.5f));
                if (v > list[list.Count - 1] + 1e-5f) list.Add(v);
            }
            return list.ToArray();
        }

        private static void AddFace(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris,
            float[] aSamples, float[] bSamples, Func<float, float, Vector3> point, Vector3 inner, float radius, bool flip)
        {
            int start = verts.Count;
            int na = aSamples.Length, nb = bSamples.Length;
            for (int j = 0; j < nb; j++)
            {
                for (int i = 0; i < na; i++)
                {
                    Vector3 p = point(aSamples[i], bSamples[j]);
                    Vector3 clamped = new Vector3(
                        Mathf.Clamp(p.x, -inner.x, inner.x),
                        Mathf.Clamp(p.y, -inner.y, inner.y),
                        Mathf.Clamp(p.z, -inner.z, inner.z));
                    Vector3 n = (p - clamped);
                    n = n.sqrMagnitude < 1e-10f ? Vector3.up : n.normalized;
                    verts.Add(clamped + n * radius);
                    normals.Add(n);
                    uvs.Add(new Vector2(i / (float)(na - 1), j / (float)(nb - 1)));
                }
            }
            for (int j = 0; j < nb - 1; j++)
            {
                for (int i = 0; i < na - 1; i++)
                {
                    int a = start + j * na + i;
                    int b = a + 1;
                    int c = a + na;
                    int d = c + 1;
                    if (!flip) { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d); }
                    else { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(b); tris.Add(d); tris.Add(c); }
                }
            }
        }

        // ------------------------------------------------------------------ Lathe
        /// <summary>
        /// Revolves a profile (x = radius, y = height) around the Y axis. Profile goes bottom to top.
        /// Radius 0 at the ends closes the shape. Optional wobble(angle01, profile01) adds frilly edges (lettuce).
        /// </summary>
        public static Mesh Lathe(IList<Vector2> profile, int segments = 32, string name = "Lathe",
            Func<float, float, float> wobble = null, Func<float, float, float> heightWobble = null)
        {
            int rows = profile.Count;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int r = 0; r < rows; r++)
            {
                float v = r / (float)(rows - 1);
                for (int s = 0; s <= segments; s++)
                {
                    float u = s / (float)segments;
                    float ang = u * Mathf.PI * 2f;
                    float rad = profile[r].x * (wobble != null ? 1f + wobble(u, v) : 1f);
                    float y = profile[r].y + (heightWobble != null ? heightWobble(u, v) : 0f);
                    verts.Add(new Vector3(Mathf.Cos(ang) * rad, y, Mathf.Sin(ang) * rad));
                    uvs.Add(new Vector2(u, v));
                }
            }
            int stride = segments + 1;
            for (int r = 0; r < rows - 1; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * stride + s;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(WhiteColors(verts.Count));
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            WeldSeamNormals(mesh, stride, rows);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        // The first and last column share positions; average their normals so the seam is invisible.
        private static void WeldSeamNormals(Mesh mesh, int stride, int rows)
        {
            Vector3[] n = mesh.normals;
            for (int r = 0; r < rows; r++)
            {
                int a = r * stride;
                int b = a + stride - 1;
                Vector3 avg = (n[a] + n[b]).normalized;
                n[a] = avg;
                n[b] = avg;
            }
            mesh.normals = n;
        }

        /// <summary>Profile helper: a rounded disc (cylinder with soft edges), bottom at y=0.</summary>
        public static List<Vector2> RoundedDiscProfile(float radius, float height, float bevel, int bevelSteps = 4)
        {
            bevel = Mathf.Min(bevel, height * 0.5f, radius);
            var p = new List<Vector2> { new Vector2(0f, 0f) };
            for (int i = 0; i <= bevelSteps; i++)
            {
                float a = -Mathf.PI * 0.5f + i / (float)bevelSteps * Mathf.PI * 0.5f;
                p.Add(new Vector2(radius - bevel + Mathf.Cos(a) * bevel, bevel + Mathf.Sin(a) * bevel));
            }
            for (int i = 0; i <= bevelSteps; i++)
            {
                float a = i / (float)bevelSteps * Mathf.PI * 0.5f;
                p.Add(new Vector2(radius - bevel + Mathf.Cos(a) * bevel, height - bevel + Mathf.Sin(a) * bevel));
            }
            p.Add(new Vector2(0f, height));
            return p;
        }

        /// <summary>Profile helper: a dome (top bun, cream), bottom at y=0.</summary>
        public static List<Vector2> DomeProfile(float radius, float height, float baseBevel, int steps = 10)
        {
            var p = new List<Vector2> { new Vector2(0f, 0f), new Vector2(radius - baseBevel, 0f) };
            for (int i = 1; i <= 3; i++)
            {
                float a = -Mathf.PI * 0.5f + i / 3f * Mathf.PI * 0.5f;
                p.Add(new Vector2(radius - baseBevel + Mathf.Cos(a) * baseBevel, baseBevel + Mathf.Sin(a) * baseBevel));
            }
            for (int i = 1; i <= steps; i++)
            {
                float a = i / (float)steps * Mathf.PI * 0.5f;
                float x = Mathf.Cos(a) * (radius);
                float y = baseBevel + Mathf.Sin(a) * (height - baseBevel);
                p.Add(new Vector2(Mathf.Max(0f, x), y));
            }
            p[p.Count - 1] = new Vector2(0f, height);
            return p;
        }

        // ------------------------------------------------------------------ Sphere / capsule-ish
        public static Mesh Sphere(float radius, int lon = 24, int lat = 16, string name = "Sphere")
        {
            var profile = new List<Vector2>();
            for (int i = 0; i <= lat; i++)
            {
                float a = -Mathf.PI * 0.5f + i / (float)lat * Mathf.PI;
                profile.Add(new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius));
            }
            profile[0] = new Vector2(0f, -radius);
            profile[lat] = new Vector2(0f, radius);
            return Lathe(profile, lon, name);
        }

        /// <summary>Vertical capsule centred at the origin.</summary>
        public static Mesh Capsule(float radius, float height, int lon = 20, int capSteps = 8, string name = "Capsule")
        {
            float half = Mathf.Max(0f, height * 0.5f - radius);
            var profile = new List<Vector2>();
            for (int i = 0; i <= capSteps; i++)
            {
                float a = -Mathf.PI * 0.5f + i / (float)capSteps * Mathf.PI * 0.5f;
                profile.Add(new Vector2(Mathf.Cos(a) * radius, -half + Mathf.Sin(a) * radius));
            }
            for (int i = 0; i <= capSteps; i++)
            {
                float a = i / (float)capSteps * Mathf.PI * 0.5f;
                profile.Add(new Vector2(Mathf.Cos(a) * radius, half + Mathf.Sin(a) * radius));
            }
            profile[0] = new Vector2(0f, -half - radius);
            profile[profile.Count - 1] = new Vector2(0f, half + radius);
            return Lathe(profile, lon, name);
        }

        // ------------------------------------------------------------------ Torus
        /// <summary>Torus in the XZ plane. arc &lt; 1 makes a partial torus (e.g. a smile).</summary>
        public static Mesh Torus(float majorRadius, float minorRadius, int majorSegments = 32, int minorSegments = 12,
            float arc = 1f, string name = "Torus")
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float startAngle = -arc * Mathf.PI; // centred on -X... rotated by callers as needed
            for (int i = 0; i <= majorSegments; i++)
            {
                float u = i / (float)majorSegments;
                float a = startAngle + u * arc * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(a) * majorRadius, 0f, Mathf.Sin(a) * majorRadius);
                Vector3 outDir = center.normalized;
                for (int j = 0; j <= minorSegments; j++)
                {
                    float v = j / (float)minorSegments;
                    float b = v * Mathf.PI * 2f;
                    Vector3 n = outDir * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    verts.Add(center + n * minorRadius);
                    normals.Add(n);
                    uvs.Add(new Vector2(u, v));
                }
            }
            int stride = minorSegments + 1;
            for (int i = 0; i < majorSegments; i++)
            {
                for (int j = 0; j < minorSegments; j++)
                {
                    int a = i * stride + j;
                    int b = a + stride;
                    int c = a + 1;
                    int d = b + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
            return Build(name, verts, normals, uvs, tris);
        }

        // ------------------------------------------------------------------ Flat ring / disc
        /// <summary>Flat ring lying in the XZ plane facing up (highlight rings, plates).</summary>
        public static Mesh Ring(float innerRadius, float outerRadius, int segments = 48, string name = "Ring")
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts.Add(d * innerRadius);
                verts.Add(d * outerRadius);
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(i / (float)segments, 0f));
                uvs.Add(new Vector2(i / (float)segments, 1f));
            }
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2;
                tris.Add(a); tris.Add(a + 2); tris.Add(a + 1);
                tris.Add(a + 1); tris.Add(a + 2); tris.Add(a + 3);
            }
            return Build(name, verts, normals, uvs, tris);
        }

        /// <summary>Flat quad in the XZ plane facing up, centred.</summary>
        public static Mesh FlatQuad(float width, float depth, string name = "FlatQuad")
        {
            float w = width * 0.5f, d = depth * 0.5f;
            var verts = new List<Vector3> { new Vector3(-w, 0, -d), new Vector3(w, 0, -d), new Vector3(-w, 0, d), new Vector3(w, 0, d) };
            var normals = new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            var uvs = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            var tris = new List<int> { 0, 2, 1, 1, 2, 3 };
            return Build(name, verts, normals, uvs, tris);
        }

        /// <summary>Upright quad in the XY plane facing -Z (towards a default camera), bottom-centre pivot optional.</summary>
        public static Mesh UprightQuad(float width, float height, bool bottomPivot = false, string name = "Quad")
        {
            float w = width * 0.5f;
            float y0 = bottomPivot ? 0f : -height * 0.5f;
            float y1 = y0 + height;
            var verts = new List<Vector3> { new Vector3(-w, y0, 0), new Vector3(w, y0, 0), new Vector3(-w, y1, 0), new Vector3(w, y1, 0) };
            var normals = new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            var uvs = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            var tris = new List<int> { 0, 2, 1, 1, 2, 3 };
            return Build(name, verts, normals, uvs, tris);
        }

        /// <summary>Extruded triangle pennant (bunting flag), hanging down from the top edge.</summary>
        public static Mesh Pennant(float width, float height, float thickness, string name = "Pennant")
        {
            float w = width * 0.5f, t = thickness * 0.5f;
            var a = new Vector3(-w, 0, 0); var b = new Vector3(w, 0, 0); var c = new Vector3(0, -height, 0);
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            void Tri(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 n)
            {
                int s = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(p2);
                normals.Add(n); normals.Add(n); normals.Add(n);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.right); uvs.Add(Vector2.up);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            }
            Vector3 f = new Vector3(0, 0, -t), k = new Vector3(0, 0, t);
            Tri(a + f, b + f, c + f, Vector3.back);
            Tri(a + k, c + k, b + k, Vector3.forward);
            return Build(name, verts, normals, uvs, tris);
        }

        // ------------------------------------------------------------------ Utilities
        /// <summary>Combines several meshes (with local transforms) into one mesh with a single submesh.</summary>
        public static Mesh Combine(string name, params (Mesh mesh, Matrix4x4 matrix)[] parts)
        {
            var combine = new CombineInstance[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                combine[i] = new CombineInstance { mesh = parts[i].mesh, transform = parts[i].matrix };
            }
            var m = new Mesh { name = name };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.CombineMeshes(combine, true, true);
            m.RecalculateBounds();
            return m;
        }

        private static List<Color> WhiteColors(int n)
        {
            var c = new List<Color>(n);
            for (int i = 0; i < n; i++) c.Add(Color.white);
            return c;
        }

        private static Mesh Build(string name, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            var mesh = new Mesh { name = name };
            if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetColors(WhiteColors(v.Count));
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
