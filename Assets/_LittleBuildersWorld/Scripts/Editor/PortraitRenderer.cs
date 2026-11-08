using System.Collections.Generic;
using System.IO;
using LittleBuildersWorld.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Renders each vehicle prefab into a round portrait sprite (selector, Drive bubbles, activity markers) and the
    /// app icon. Uses a throwaway scene so nothing leaks into game scenes.
    /// </summary>
    public static class PortraitRenderer
    {
        public const string Folder = E.Root + "/Art/UI/Portraits";
        public const string AppIconPath = E.Root + "/Art/UI/AppIcon.png";

        private static readonly Dictionary<VehicleType, string> Backgrounds = new Dictionary<VehicleType, string>
        {
            { VehicleType.Excavator, "#FFF1C1" },
            { VehicleType.DumpTruck, "#CDEBFF" },
            { VehicleType.Bulldozer, "#FFE0C2" },
            { VehicleType.RoadRoller, "#D9F5DC" }
        };

        public static Dictionary<VehicleType, Sprite> RenderAll()
        {
            E.EnsureFolder(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lightGo = new GameObject("Key", typeof(Light));
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = E.Hex("#FFF4E0");
            light.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = E.Hex("#B8C4D6");

            var camGo = new GameObject("PortraitCam", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            var result = new Dictionary<VehicleType, Sprite>();
            foreach (VehicleType type in new[] { VehicleType.Excavator, VehicleType.DumpTruck, VehicleType.Bulldozer, VehicleType.RoadRoller })
            {
                GameObject prefab = VehicleBuilder.Load(type);
                if (prefab == null) continue;
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 215f, 0f));
                Bounds b = WorldBounds(go);
                cam.backgroundColor = E.Hex(Backgrounds[type]);
                Frame(cam, b, 1.08f);
                string path = $"{Folder}/Portrait_{type}.png";
                File.WriteAllBytes(path, Capture(cam, 256, true));
                if (type == VehicleType.Excavator)
                {
                    cam.backgroundColor = E.Hex("#FFC93C");
                    Frame(cam, b, 1.0f);
                    File.WriteAllBytes(AppIconPath, Capture(cam, 512, false));
                }
                Object.DestroyImmediate(go);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Sprite;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 256;
                imp.SaveAndReimport();
                result[type] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            AssetDatabase.ImportAsset(AppIconPath);
            return result;
        }

        private static Bounds WorldBounds(GameObject go)
        {
            var b = new Bounds(go.transform.position + Vector3.up, Vector3.one);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            return b;
        }

        private static void Frame(Camera cam, Bounds b, float margin)
        {
            Vector3 dir = Quaternion.Euler(14f, 0f, 0f) * Vector3.back;
            float radius = b.extents.magnitude * margin;
            float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.transform.position = b.center - dir * -dist;
            cam.transform.LookAt(b.center);
        }

        private static byte[] Capture(Camera cam, int size, bool roundMask)
        {
            var rt = new RenderTexture(size * 2, size * 2, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            for (int i = 0; i < 3; i++) cam.Render();
            RenderTexture.active = rt;
            var big = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            big.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            big.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // 2x2 downsample for smooth edges
                Color col = (big.GetPixel(x * 2, y * 2) + big.GetPixel(x * 2 + 1, y * 2) + big.GetPixel(x * 2, y * 2 + 1) + big.GetPixel(x * 2 + 1, y * 2 + 1)) * 0.25f;
                col.a = 1f;
                if (roundMask)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float outer = c - 1f, ring = outer - 10f;
                    if (d > ring) col = Color.Lerp(col, Color.white, Mathf.Clamp01(d - ring));
                    col.a = Mathf.Clamp01(outer - d + 0.5f);
                }
                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(big);
            Object.DestroyImmediate(tex);
            return png;
        }
    }
}
