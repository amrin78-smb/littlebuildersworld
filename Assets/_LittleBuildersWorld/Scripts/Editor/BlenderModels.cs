using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using A = LittleBuildersWorld.EditorTools.ArtLibrary;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Reads the smooth, textured vehicle models made by our own Blender script (Tools/blender/build_models.sh):
    /// a mesh pack (Art/Models/Vehicles/&lt;Name&gt;.bytes: pivots + one mesh per pivot and paint slot) and a baked
    /// texture (colour x soft shadow, smoothness in alpha) plus a glow mask. Builds the pivots under the vehicle body
    /// and returns them by name so VehicleBuilder can wire wheels, bed and beacon exactly as before.
    /// </summary>
    public static class BlenderModels
    {
        public const string ModelFolder = E.Root + "/Art/Models/Vehicles";
        public const string TextureFolder = E.Root + "/Art/Textures/Vehicles";

        public sealed class Result
        {
            public readonly Dictionary<string, Transform> Nodes = new Dictionary<string, Transform>();
            public readonly List<Renderer> Primary = new List<Renderer>();
            public readonly List<Renderer> Secondary = new List<Renderer>();
            public readonly List<Renderer> Tracks = new List<Renderer>();
            public int Vertices;
        }

        public static bool Has(string vehicle) => File.Exists($"{ModelFolder}/{vehicle}.bytes") && File.Exists($"{TextureFolder}/T_{vehicle}.png");

        public static Result Build(string vehicle, Transform body, string primaryHex, string secondaryHex)
        {
            Texture2D tex = Texture($"{TextureFolder}/T_{vehicle}.png", 1024);
            Texture2D glow = Texture($"{TextureFolder}/T_{vehicle}_E.png", 512);
            var mats = new Dictionary<string, Material>
            {
                ["Primary"] = MakeMaterial($"Vehicle_{vehicle}_Primary", primaryHex, tex, null),
                ["Secondary"] = MakeMaterial($"Vehicle_{vehicle}_Secondary", secondaryHex, tex, null),
                ["Trim"] = MakeMaterial($"Vehicle_{vehicle}_Trim", "#FFFFFF", tex, glow),
                ["Track"] = A.Track // crawler belt: own UVs, the tread texture scrolls (VehicleVisuals)
            };

            Result result = Load($"{ModelFolder}/{vehicle}.bytes", vehicle, body, mats);
            Debug.Log($"[LBW] {vehicle}: Blender model, {result.Vertices} vertices");
            return result;
        }

        // ------------------------------------------------------------------ environment props (Tools/blender/build_env.sh)
        public const string PropModelFolder = E.Root + "/Art/Models/Props";
        public const string PropTextureFolder = E.Root + "/Art/Textures/Props";

        public static bool HasProp(string prop, string group) =>
            File.Exists($"{PropModelFolder}/{prop}.bytes") && File.Exists($"{PropTextureFolder}/T_Env{group}.png");

        /// <summary>
        /// Builds a prop from its mesh pack under <paramref name="root"/> (pack node "Body" = the root). Every prop of a
        /// group shares one material/texture, so static batching merges them across the whole town.
        /// </summary>
        public static Result BuildProp(string prop, string group, Transform root)
        {
            var mats = new Dictionary<string, Material> { ["Trim"] = GroupMaterial(group) };
            return Load($"{PropModelFolder}/{prop}.bytes", prop, root, mats);
        }

        public static Material GroupMaterial(string group)
        {
            Texture2D tex = Texture($"{PropTextureFolder}/T_Env{group}.png", group == "Buildings" ? 2048 : 1024);
            Texture2D glow = Texture($"{PropTextureFolder}/T_Env{group}_E.png", 256);
            return MakeMaterial($"Env_{group}", "#FFFFFF", tex, glow);
        }

        private static Result Load(string path, string prefix, Transform body, Dictionary<string, Material> mats)
        {
            var result = new Result();
            result.Nodes["Body"] = body;
            using (var r = new BinaryReader(File.OpenRead(path)))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "LBWM" || r.ReadInt32() != 1)
                    throw new IOException($"[LBW] {path} is not a version 1 mesh pack");
                int nodeCount = r.ReadInt32();
                for (int i = 0; i < nodeCount; i++)
                {
                    string name = Str(r), parent = Str(r);
                    Vector3 pos = V3(r);
                    result.Nodes[name] = A.Pivot(name, result.Nodes[parent], pos);
                }
                int meshCount = r.ReadInt32();
                for (int i = 0; i < meshCount; i++)
                {
                    string name = Str(r), node = Str(r), slot = Str(r);
                    int vc = r.ReadInt32();
                    var verts = new Vector3[vc];
                    var normals = new Vector3[vc];
                    var uvs = new Vector2[vc];
                    for (int v = 0; v < vc; v++) verts[v] = V3(r);
                    for (int v = 0; v < vc; v++) normals[v] = V3(r);
                    for (int v = 0; v < vc; v++) uvs[v] = new Vector2(r.ReadSingle(), r.ReadSingle());
                    int ic = r.ReadInt32();
                    var tris = new int[ic];
                    for (int t = 0; t < ic; t++) tris[t] = r.ReadInt32();
                    var mesh = new Mesh { indexFormat = vc > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                    mesh.vertices = verts;
                    mesh.normals = normals;
                    mesh.uv = uvs;
                    mesh.triangles = tris;
                    mesh.RecalculateBounds();
                    mesh.RecalculateTangents();
                    mesh = A.SaveMesh($"BM_{prefix}_{name}", mesh);
                    GameObject go = A.Part($"{node}_{slot}", result.Nodes[node], mesh, mats[slot], Vector3.zero);
                    var renderer = go.GetComponent<Renderer>();
                    if (slot == "Primary") result.Primary.Add(renderer);
                    else if (slot == "Secondary") result.Secondary.Add(renderer);
                    else if (slot == "Track") result.Tracks.Add(renderer);
                    result.Vertices += vc;
                }
            }
            return result;
        }

        private static string Str(BinaryReader r) => Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));

        private static Vector3 V3(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        private static Texture2D Texture(string path, int maxSize)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (imp.maxTextureSize != maxSize || !imp.mipmapEnabled || imp.wrapMode != TextureWrapMode.Clamp))
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.alphaIsTransparency = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.mipmapEnabled = true;
                imp.anisoLevel = 2;
                imp.maxTextureSize = maxSize;
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Simple Lit with the baked map: tint = paint colour, smoothness from the map's alpha, optional glow mask.</summary>
        private static Material MakeMaterial(string name, string hex, Texture2D tex, Texture2D glow)
        {
            Material m = A.Lit(name, hex, 0.5f, tex);
            m.SetFloat("_SmoothnessSource", 1f);
            m.SetFloat("_SpecularHighlights", 1f);
            m.SetColor("_SpecColor", new Color(0.32f, 0.32f, 0.32f, 1f));
            if (glow != null)
            {
                m.SetTexture("_EmissionMap", glow);
                m.SetColor("_EmissionColor", new Color(0.7f, 0.7f, 0.7f, 1f));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.EnableKeyword("_EMISSION");
            }
            BaseShaderGUI.SetMaterialKeywords(m);
            SimpleLitGUI.SetMaterialKeywords(m); // specular colour + smoothness from the map's alpha
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
