using System.Collections.Generic;
using LittleBuildersWorld.Core;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Shared palette materials and baked mesh assets. Every prop and vehicle draws from the same small set of
    /// materials (good for batching on mobile) and the same mesh assets (GUIDs survive re-runs).
    /// </summary>
    public static class ArtLibrary
    {
        public const string MaterialFolder = E.Root + "/Art/Materials";
        public const string MeshFolder = E.Root + "/Art/Models/Meshes";

        private static readonly Dictionary<string, Material> s_Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Mesh> s_Meshes = new Dictionary<string, Mesh>();

        public static void ClearCache()
        {
            s_Materials.Clear();
            s_Meshes.Clear();
        }

        // ------------------------------------------------------------------ palette (see ART_DIRECTION.md)
        public static Material Lit(string name, string hex, float smoothness = 0.25f, Texture2D tex = null, Vector2? tiling = null, float glow = 0f)
        {
            string key = "lit_" + name;
            if (s_Materials.TryGetValue(key, out Material m) && m != null) return m;
            string path = $"{MaterialFolder}/M_{name}.mat";
            E.EnsureFolder(MaterialFolder);
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_BaseColor", E.Hex(hex));
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_SpecularHighlights", smoothness > 0.3f ? 1f : 0f);
            m.SetTexture("_BaseMap", tex);
            if (tiling.HasValue) m.SetTextureScale("_BaseMap", tiling.Value);
            // Glow: a little emission so lights read as "on" in shade (no bloom: too costly on the budget phone).
            m.SetColor("_EmissionColor", glow > 0f ? E.Hex(hex) * glow : Color.black);
            m.globalIlluminationFlags = glow > 0f ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            if (glow > 0f) m.EnableKeyword("_EMISSION"); else m.DisableKeyword("_EMISSION");
            m.enableInstancing = true;
            BaseShaderGUI.SetMaterialKeywords(m);
            EditorUtility.SetDirty(m);
            s_Materials[key] = m;
            return m;
        }

        /// <summary>Unlit, alpha-clipped (cheap cut-out) material for icons, markers and the guidance arrow.</summary>
        public static Material UnlitCutout(string name, Texture2D tex, string hex = "#FFFFFF")
        {
            string key = "unlit_" + name;
            if (s_Materials.TryGetValue(key, out Material m) && m != null) return m;
            string path = $"{MaterialFolder}/MU_{name}.mat";
            E.EnsureFolder(MaterialFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_BaseColor", E.Hex(hex));
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_AlphaClip", tex != null ? 1f : 0f);
            m.SetFloat("_Cutoff", 0.5f);
            m.SetFloat("_Cull", 0f); // two-sided
            BaseShaderGUI.SetMaterialKeywords(m);
            EditorUtility.SetDirty(m);
            s_Materials[key] = m;
            return m;
        }

        /// <summary>Particle material (alpha or additive), for sparkles, confetti, dust and smoke.</summary>
        public static Material Particle(string name, Texture2D tex, bool additive)
        {
            string key = "fx_" + name;
            if (s_Materials.TryGetValue(key, out Material m) && m != null) return m;
            string path = $"{MaterialFolder}/MFX_{name}.mat";
            E.EnsureFolder(MaterialFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetColor("_BaseColor", Color.white);
            BaseShaderGUI.SetMaterialKeywords(m);
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            s_Materials[key] = m;
            return m;
        }

        // Named palette entries used across builders.
        public static Material Grass => Lit("Grass", "#FFFFFF", 0.05f, IconPainter.Texture("tex_grass"), new Vector2(1f, 1f));
        public static Material Sand => Lit("YardSand", "#FFFFFF", 0.05f, IconPainter.Texture("tex_sand"));
        public static Material Dirt => Lit("Dirt", "#FFFFFF", 0.05f, IconPainter.Texture("tex_dirt"));
        public static Material Road => Lit("Road", "#FFFFFF", 0.15f, IconPainter.Texture("tex_road"));
        public static Material RoadPlain => Lit("RoadPlain", "#FFFFFF", 0.15f, IconPainter.Texture("tex_road_plain"));
        public static Material Track => Lit("Track", "#FFFFFF", 0.1f, IconPainter.Texture("tex_track"));
        public static Material Soil => Lit("Soil", "#8B5A3C", 0.05f);
        public static Material SoilLight => Lit("SoilLight", "#A87149", 0.05f);
        public static Material Gravel => Lit("Gravel", "#B9B6AC", 0.05f);
        public static Material Tarmac => Lit("Tarmac", "#4A4E58", 0.2f);
        public static Material Pothole => Lit("Pothole", "#3B3631", 0.05f);
        public static Material Rubber => Lit("Rubber", "#2F2E3A", 0.15f);
        public static Material Steel => Lit("Steel", "#A7B1BC", 0.45f);
        public static Material DarkSteel => Lit("DarkSteel", "#596270", 0.35f);
        public static Material Glass => Lit("Glass", "#A8E6FF", 0.7f);
        public static Material White => Lit("White", "#FFFFFF", 0.3f);
        public static Material Ink => Lit("Ink", "#2B2523", 0.4f);
        public static Material Wood => Lit("Wood", "#C98B5A", 0.1f);
        public static Material DarkWood => Lit("DarkWood", "#8A5A3A", 0.1f);
        public static Material Leaf => Lit("Leaf", "#4CB050", 0.1f);
        public static Material LeafLight => Lit("LeafLight", "#7BCB5A", 0.1f);
        public static Material Rock => Lit("Rock", "#9C9A96", 0.1f);
        public static Material Cloud => Lit("Cloud", "#FFFFFF", 0.0f);
        public static Material Beacon => Lit("Beacon", "#FFA62B", 0.6f);
        public static Material Concrete => Lit("Concrete", "#D7D3CB", 0.1f);
        public static Material Brick => Lit("Brick", "#E76F51", 0.1f);
        public static Material RoofRed => Lit("RoofRed", "#E63946", 0.2f);
        public static Material ConeOrange => Lit("Cone", "#FF7A1A", 0.3f);

        public static Material TailLight => Lit("TailLight", "#F0384A", 0.6f, null, null, 0.35f);
        public static Material Indicator => Lit("Indicator", "#FFA62B", 0.6f, null, null, 0.35f);
        public static Material HeadLight => Lit("HeadLight", "#FFF6C8", 0.8f, null, null, 0.45f);
        public static Material Cheek => Lit("Cheek", "#FF9AAE", 0.2f);
        public static Material Hazard => Lit("HazardYellow", "#FFD447", 0.3f);
        public static Material Skin => Lit("ResidentSkin", "#F1C27D", 0.3f);

        public static Material Colour(string name, string hex, float smoothness = 0.3f) => Lit(name, hex, smoothness);

        // ------------------------------------------------------------------ meshes
        public static Mesh SaveMesh(string name, Mesh mesh)
        {
            if (s_Meshes.TryGetValue(name, out Mesh cached) && cached != null) return cached;
            mesh.name = name;
            Mesh saved = E.CreateOrReplace(mesh, $"{MeshFolder}/{name}.asset");
            s_Meshes[name] = saved;
            return saved;
        }

        public static Mesh Box(float x, float y, float z, float radius)
        {
            string name = $"RBox_{x:0.##}x{y:0.##}x{z:0.##}_r{radius:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            // Bevel detail follows the bevel size: a tiny bevel needs no more than a chamfer (keeps vertex counts low on
            // the budget phone; fences alone are hundreds of boxes).
            int segments = radius <= 0.05f ? 1 : radius <= 0.1f ? 2 : 3;
            return SaveMesh(name, MeshFactory.RoundedBox(new Vector3(x, y, z), radius, segments));
        }

        /// <summary>Wheel/drum: a rounded disc whose axle is the X axis, centred.</summary>
        public static Mesh Wheel(float radius, float width, float bevel)
        {
            string name = $"Wheel_r{radius:0.##}_w{width:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            Mesh disc = MeshFactory.Lathe(MeshFactory.RoundedDiscProfile(radius, width, bevel, 3), 24, name + "_src");
            Matrix4x4 toAxle = Matrix4x4.TRS(new Vector3(-width * 0.5f, 0f, 0f), Quaternion.Euler(0f, 0f, -90f), Vector3.one);
            Mesh combined = MeshFactory.Combine(name, (disc, toAxle));
            combined.RecalculateNormals();
            Object.DestroyImmediate(disc);
            return SaveMesh(name, combined);
        }

        /// <summary>A tyre with chunky tread blocks (axle on X), like <see cref="Wheel"/>.</summary>
        public static Mesh Tyre(float radius, float width)
        {
            string name = $"Tyre_r{radius:0.##}_w{width:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            float core = radius - 0.05f;
            Mesh disc = MeshFactory.Lathe(MeshFactory.RoundedDiscProfile(core, width, Mathf.Min(0.15f, width * 0.3f), 3), 24, name + "_src");
            Mesh lug = MeshFactory.RoundedBox(new Vector3(width * 0.8f, 0.12f, 0.17f), 0.035f, 1);
            const int n = 16;
            var parts = new (Mesh, Matrix4x4)[n + 1];
            parts[0] = (disc, Matrix4x4.TRS(new Vector3(-width * 0.5f, 0f, 0f), Quaternion.Euler(0f, 0f, -90f), Vector3.one));
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n;
                float r = a * Mathf.Deg2Rad;
                // Stagger the blocks left/right for a chunky toy tread.
                float x = (i % 2 == 0 ? -1f : 1f) * width * 0.08f;
                parts[i + 1] = (lug, Matrix4x4.TRS(new Vector3(x, Mathf.Cos(r) * (core - 0.01f), Mathf.Sin(r) * (core - 0.01f)), Quaternion.Euler(a, 0f, 0f), Vector3.one));
            }
            Mesh combined = MeshFactory.Combine(name, parts);
            Object.DestroyImmediate(disc);
            Object.DestroyImmediate(lug);
            return SaveMesh(name, combined);
        }

        public static Mesh Disc(float radius, float height, float bevel, int segments = 24)
        {
            string name = $"Disc_r{radius:0.##}_h{height:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            return SaveMesh(name, MeshFactory.Lathe(MeshFactory.RoundedDiscProfile(radius, height, bevel, 3), segments, name));
        }

        public static Mesh Dome(float radius, float height)
        {
            string name = $"Dome_r{radius:0.##}_h{height:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            return SaveMesh(name, MeshFactory.Lathe(MeshFactory.DomeProfile(radius, height, Mathf.Min(0.15f, height * 0.3f), 8), 20, name));
        }

        public static Mesh Sphere(float radius)
        {
            string name = $"Sphere_r{radius:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            // Small spheres (eyes, flowers, bolts) need far fewer facets than tree crowns.
            if (radius < 0.12f) return SaveMesh(name, MeshFactory.Sphere(radius, 10, 6, name));
            if (radius < 0.3f) return SaveMesh(name, MeshFactory.Sphere(radius, 12, 8, name));
            return SaveMesh(name, MeshFactory.Sphere(radius, 16, 10, name));
        }

        public static Mesh Cone(float radius, float height)
        {
            string name = $"Cone_r{radius:0.##}_h{height:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            var profile = new List<Vector2> { new Vector2(0, 0), new Vector2(radius, 0), new Vector2(radius * 0.96f, 0.06f), new Vector2(0.04f, height), new Vector2(0, height) };
            return SaveMesh(name, MeshFactory.Lathe(profile, 16, name));
        }

        public static Mesh Ring(float inner, float outer)
        {
            string name = $"Ring_{inner:0.##}_{outer:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            return SaveMesh(name, MeshFactory.Ring(inner, outer, 40, name));
        }

        public static Mesh Quad(float w, float d)
        {
            string name = $"Quad_{w:0.##}x{d:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            return SaveMesh(name, MeshFactory.FlatQuad(w, d, name));
        }

        public static Mesh UprightQuad(float w, float h)
        {
            string name = $"UQuad_{w:0.##}x{h:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            return SaveMesh(name, MeshFactory.UprightQuad(w, h, false, name));
        }

        public static Mesh Pennant(float w, float h)
        {
            string name = $"Pennant_{w:0.##}x{h:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            return SaveMesh(name, MeshFactory.Pennant(w, h, 0.03f, name));
        }

        /// <summary>
        /// A flat strip on the ground from a to b (XZ), width w, with V running along its length at one tile per
        /// <paramref name="tileLength"/> metres, so road markings tile correctly.
        /// </summary>
        public static Mesh Strip(string name, float length, float width, float tileLength)
        {
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            float w = width * 0.5f, l = length * 0.5f, v = length / tileLength;
            var mesh = new Mesh { name = name };
            mesh.SetVertices(new List<Vector3> { new Vector3(-w, 0, -l), new Vector3(w, 0, -l), new Vector3(-w, 0, l), new Vector3(w, 0, l) });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, v), new Vector2(1, v) });
            mesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return SaveMesh(name, mesh);
        }

        /// <summary>Big tiled ground plane (UVs in metres / tile).</summary>
        public static Mesh Ground(string name, float size, float tile)
        {
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            float h = size * 0.5f, t = size / tile;
            var mesh = new Mesh { name = name };
            mesh.SetVertices(new List<Vector3> { new Vector3(-h, 0, -h), new Vector3(h, 0, -h), new Vector3(-h, 0, h), new Vector3(h, 0, h) });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(t, 0), new Vector2(0, t), new Vector2(t, t) });
            mesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return SaveMesh(name, mesh);
        }

        /// <summary>Rectangular ground patch w×d metres, UVs tiled every <paramref name="tile"/> metres.</summary>
        public static Mesh GroundRect(string name, float w, float d, float tile)
        {
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            float hw = w * 0.5f, hd = d * 0.5f, u = w / tile, v = d / tile;
            var mesh = new Mesh { name = name };
            mesh.SetVertices(new List<Vector3> { new Vector3(-hw, 0, -hd), new Vector3(hw, 0, -hd), new Vector3(-hw, 0, hd), new Vector3(hw, 0, hd) });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(u, 0), new Vector2(0, v), new Vector2(u, v) });
            mesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return SaveMesh(name, mesh);
        }

        /// <summary>Smooth gentle mound (for the practice hill) with a cosine profile, base at y=0.</summary>
        public static Mesh Hill(string name, float radius, float height)
        {
            if (s_Meshes.TryGetValue(name, out Mesh m) && m != null) return m;
            var profile = new List<Vector2>();
            const int steps = 14;
            for (int i = 0; i <= steps; i++)
            {
                float r = radius * (1f - i / (float)steps);
                float y = height * 0.5f * (1f + Mathf.Cos(Mathf.PI * (r / radius)));
                profile.Add(new Vector2(r, y));
            }
            profile[0] = new Vector2(radius, 0f);
            profile.Add(new Vector2(0f, height));
            return SaveMesh(name, MeshFactory.Lathe(profile, 32, name));
        }

        /// <summary>
        /// Crawler track: a stadium-shaped belt (submesh 0, UV v runs around the loop so cleats can scroll) with flat
        /// side plates (submesh 1). Length along Z, height along Y (bottom at 0), width along X, centred.
        /// </summary>
        public static Mesh TrackLoop(float length, float height, float width, float cleatSpacing = 0.35f)
        {
            string name = $"Track_{length:0.##}x{height:0.##}x{width:0.##}".Replace('.', 'p');
            if (s_Meshes.TryGetValue(name, out Mesh cached) && cached != null) return cached;
            float r = height * 0.5f, zb = -(length * 0.5f - r), zf = length * 0.5f - r, w = width * 0.5f;
            var path = new List<Vector2>();   // (z, y)
            var normal = new List<Vector2>();
            const int arc = 10;
            const int straight = 8;
            for (int i = 0; i < straight; i++) { path.Add(new Vector2(Mathf.Lerp(zb, zf, i / (float)straight), 0f)); normal.Add(Vector2.down); }
            for (int i = 0; i <= arc; i++)
            {
                float a = -Mathf.PI * 0.5f + i / (float)arc * Mathf.PI;
                var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                path.Add(new Vector2(zf, r) + n * r); normal.Add(n);
            }
            for (int i = 1; i < straight; i++) { path.Add(new Vector2(Mathf.Lerp(zf, zb, i / (float)straight), height)); normal.Add(Vector2.up); }
            for (int i = 0; i <= arc; i++)
            {
                float a = Mathf.PI * 0.5f + i / (float)arc * Mathf.PI;
                var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                path.Add(new Vector2(zb, r) + n * r); normal.Add(n);
            }
            path.Add(path[0]); normal.Add(normal[0]);
            float perimeter = 0f;
            var along = new List<float> { 0f };
            for (int i = 1; i < path.Count; i++) { perimeter += Vector2.Distance(path[i - 1], path[i]); along.Add(perimeter); }
            float repeats = Mathf.Max(1f, Mathf.Round(perimeter / (cleatSpacing * 8f)));
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>();
            var belt = new List<int>(); var sides = new List<int>();
            for (int i = 0; i < path.Count; i++)
            {
                float v = along[i] / perimeter * repeats;
                verts.Add(new Vector3(-w, path[i].y, path[i].x)); norms.Add(new Vector3(0, normal[i].y, normal[i].x)); uvs.Add(new Vector2(0, v));
                verts.Add(new Vector3(w, path[i].y, path[i].x)); norms.Add(new Vector3(0, normal[i].y, normal[i].x)); uvs.Add(new Vector2(1, v));
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                int a = i * 2;
                belt.Add(a); belt.Add(a + 1); belt.Add(a + 2);
                belt.Add(a + 1); belt.Add(a + 3); belt.Add(a + 2);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * (w + 0.005f);
                int centre = verts.Count;
                verts.Add(new Vector3(x, r, 0f)); norms.Add(new Vector3(side, 0, 0)); uvs.Add(new Vector2(0.5f, 0.5f));
                int first = verts.Count;
                float inset = 0f; // plates meet the belt edge: no see-through slit
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Vector2 p = path[i] - normal[i] * inset;
                    verts.Add(new Vector3(x, p.y, p.x)); norms.Add(new Vector3(side, 0, 0)); uvs.Add(Vector2.zero);
                }
                int count = path.Count - 1;
                for (int i = 0; i < count; i++)
                {
                    int a = first + i, b = first + (i + 1) % count;
                    // Wound so each plate faces outward (+X plate toward +X); back-face culling keeps the outer face.
                    if (side > 0) { sides.Add(centre); sides.Add(b); sides.Add(a); }
                    else { sides.Add(centre); sides.Add(a); sides.Add(b); }
                }
            }
            var mesh = new Mesh { name = name, subMeshCount = 2 };
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs);
            mesh.SetTriangles(belt, 0); mesh.SetTriangles(sides, 1);
            mesh.RecalculateBounds();
            return SaveMesh(name, mesh);
        }

        // ------------------------------------------------------------------ object helpers
        public static GameObject Part(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3? euler = null, Vector3? scale = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = scale ?? Vector3.one;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            return go;
        }

        public static Transform Pivot(string name, Transform parent, Vector3 pos, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return go.transform;
        }

        public static void SetLayer(GameObject root, int layer)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        public static void NoShadows(GameObject root)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
