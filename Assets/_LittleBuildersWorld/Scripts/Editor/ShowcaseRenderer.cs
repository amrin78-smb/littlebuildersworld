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
    /// Art review tool (not part of the game): renders every vehicle from four sides, the props, and a few town
    /// viewpoints into PNG contact sheets under Logs/Showcase. Batch: Tools/showcase.sh (after Tools/setup.sh).
    /// </summary>
    public static class ShowcaseRenderer
    {
        private const string OutDir = "Logs/Showcase";
        private const int Cell = 480;

        private static readonly string[] Props =
        {
            "TreeRound", "TreePine", "Bush", "Fence", "FlagPole", "Lamp", "SignPost", "Barrier", "Shed", "WaterTower",
            "Windmill", "TownHouseA", "TownHouseB", "Fountain", "Hopper", "BrickPallet", "Skip", "SoilMound",
            "DebrisRock", "DebrisLog", "DebrisCrate", "DebrisCone", "Resident", "Foreman", "KidA", "KidB",
            "Sandpit", "Slide", "Swings", "ClimbingFrame", "Roundabout", "Bench", "Rainbow", "PartsPallet",
            "Bunting", "BalloonArch"
        };

        // Town viewpoints: camera position, look-at point.
        private static readonly (string name, Vector3 pos, Vector3 look)[] Views =
        {
            ("yard", new Vector3(0f, 14f, -22f), new Vector3(0f, 0f, 0f)),
            ("yard_low", new Vector3(-6f, 4.5f, -12f), new Vector3(2f, 1f, 2f)),
            ("house", new Vector3(34f, 16f, 60f), new Vector3(34f, 0f, 82f)),
            ("roadworks", new Vector3(60f, 10f, -30f), new Vector3(60f, 0f, -5f)),
            ("playground", new Vector3(-34f, 16f, -60f), new Vector3(-34f, 0f, -82f)),
            ("town_wide", new Vector3(0f, 55f, -95f), new Vector3(0f, 0f, 0f)),
            ("pond", new Vector3(-36f, 5f, 24f), new Vector3(-36f, 0f, 36f)),
            ("houses", new Vector3(-58f, 4f, -20f), new Vector3(-75f, 2f, -10f)),
            ("playground_close", new Vector3(-24f, 6f, -70f), new Vector3(-32f, 1f, -79f)),
            ("house_close", new Vector3(31f, 4f, 70f), new Vector3(34f, 2.5f, 82f)),
            ("roadworks_close", new Vector3(56f, 4f, -18f), new Vector3(60f, 0f, -8f)),
            ("drive_view", new Vector3(0f, 5f, -42f), new Vector3(0f, 1.5f, -30f)),
        };

        [MenuItem(ProjectSetup.MenuRoot + "Art/Render Showcase", priority = 60)]
        public static void Render()
        {
            Directory.CreateDirectory(OutDir);
            RenderVehiclesAndProps();
            RenderTown();
            Debug.Log("[LBW] Showcase written to " + Path.GetFullPath(OutDir));
        }

        public static void RunBatch()
        {
            try
            {
                Render();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static Camera StudioScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lightGo = new GameObject("Sun", typeof(Light));
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = E.Hex("#FFF2DC");
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            lightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = E.Hex("#C9E8FF");
            RenderSettings.ambientEquatorColor = E.Hex("#E9E2D0");
            RenderSettings.ambientGroundColor = E.Hex("#9DB98A");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 10f;
            ground.GetComponent<Renderer>().sharedMaterial = ArtLibrary.Sand;
            var camGo = new GameObject("Cam", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = E.Hex("#A8DEFF");
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            return cam;
        }

        private static void RenderVehiclesAndProps()
        {
            Camera cam = StudioScene();
            // Yaw, pitch, framing: front 3/4, rear (the driving view), side, rear 3/4 from high, front close, face close.
            var angles = new[] { (215f, 14f, 1.05f), (0f, 18f, 1.05f), (90f, 8f, 1.05f), (140f, 35f, 1.05f), (180f, 6f, 0.62f), (205f, 12f, 0.5f) };
            foreach (VehicleType type in new[] { VehicleType.Excavator, VehicleType.DumpTruck, VehicleType.Bulldozer, VehicleType.RoadRoller })
            {
                GameObject prefab = VehicleBuilder.Load(type);
                if (prefab == null) continue;
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var cells = new List<Texture2D>();
                foreach (var (yaw, pitch, margin) in angles)
                {
                    go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    Frame(cam, Bounds(go), pitch, margin);
                    cells.Add(Capture(cam, Cell));
                }
                Object.DestroyImmediate(go);
                Sheet($"veh_{type}", cells, 2);
            }

            var propCells = new List<Texture2D>();
            int sheet = 0;
            foreach (string name in Props)
            {
                GameObject prefab = PropBuilder.Get(name);
                if (prefab == null) { Debug.LogWarning("[LBW] Showcase: no prop " + name); continue; }
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
                Frame(cam, Bounds(go), 18f, 1.0f);
                propCells.Add(Capture(cam, Cell / 2 * 3 / 2));
                Object.DestroyImmediate(go);
                if (propCells.Count == 12)
                {
                    Sheet($"props_{sheet++}", propCells, 4);
                    propCells.Clear();
                }
            }
            if (propCells.Count > 0) Sheet($"props_{sheet}", propCells, 4);
        }

        private static void RenderTown()
        {
            if (!File.Exists(TownBuilder.ScenePath)) return;
            EditorSceneManager.OpenScene(TownBuilder.ScenePath, OpenSceneMode.Single);
            Camera main = Camera.main;
            var camGo = new GameObject("ShowcaseCam", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            if (main != null)
            {
                cam.CopyFrom(main);
                main.enabled = false;
            }
            // Start-of-game look: the roadworks' finished road lines are hidden until that project is done.
            GameObject freshLines = GameObject.Find("FreshLines");
            if (freshLines != null) freshLines.SetActive(false);
            int renderers = 0, staticR = 0;
            long verts = 0;
            var byMesh = new Dictionary<string, (int count, long verts)>();
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                renderers++;
                if (r.gameObject.isStatic) staticR++;
                verts += mf.sharedMesh.vertexCount;
                byMesh.TryGetValue(mf.sharedMesh.name, out var e);
                byMesh[mf.sharedMesh.name] = (e.count + 1, e.verts + mf.sharedMesh.vertexCount);
            }
            var top = new List<KeyValuePair<string, (int count, long verts)>>(byMesh);
            top.Sort((x, y) => y.Value.verts.CompareTo(x.Value.verts));
            for (int i = 0; i < Mathf.Min(25, top.Count); i++)
                Debug.Log($"[LBW] Showcase mesh {top[i].Key}: {top[i].Value.count} x = {top[i].Value.verts} verts");
            Debug.Log($"[LBW] Showcase town stats: {renderers} active mesh renderers ({staticR} static), {verts} vertices");
            cam.fieldOfView = 50f;
            cam.farClipPlane = 400f;
            foreach (var (name, pos, look) in Views)
            {
                cam.transform.position = pos;
                cam.transform.LookAt(look);
                Texture2D t = Capture(cam, 0, 1280, 720);
                File.WriteAllBytes($"{OutDir}/town_{name}.png", t.EncodeToPNG());
                Object.DestroyImmediate(t);
            }
        }

        private static Bounds Bounds(GameObject go)
        {
            var b = new Bounds(go.transform.position + Vector3.up, Vector3.one);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                if (!(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
            return b;
        }

        private static void Frame(Camera cam, Bounds b, float pitch, float margin)
        {
            Vector3 dir = Quaternion.Euler(pitch, 0f, 0f) * Vector3.forward;
            float radius = b.extents.magnitude * margin;
            float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.transform.position = b.center - dir * dist;
            cam.transform.LookAt(b.center);
        }

        private static Texture2D Capture(Camera cam, int size, int w = 0, int h = 0)
        {
            if (w == 0) { w = size; h = size; }
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            for (int i = 0; i < 2; i++) cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            return tex;
        }

        private static void Sheet(string name, List<Texture2D> cells, int cols)
        {
            int cw = cells[0].width, ch = cells[0].height;
            int rows = (cells.Count + cols - 1) / cols;
            var sheet = new Texture2D(cw * cols, ch * rows, TextureFormat.RGB24, false);
            var fill = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(40, 40, 48, 255);
            sheet.SetPixels32(fill);
            for (int i = 0; i < cells.Count; i++)
            {
                int x = i % cols, y = rows - 1 - i / cols;
                sheet.SetPixels(x * cw, y * ch, cw, ch, cells[i].GetPixels());
                Object.DestroyImmediate(cells[i]);
            }
            sheet.Apply();
            File.WriteAllBytes($"{OutDir}/{name}.png", sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }
    }
}
