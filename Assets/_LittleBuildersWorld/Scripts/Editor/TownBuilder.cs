using System.Collections.Generic;
using LittleBuildersWorld.Activities;
using LittleBuildersWorld.CameraSystem;
using LittleBuildersWorld.Input;
using LittleBuildersWorld.Vehicles;
using LittleBuildersWorld.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using A = LittleBuildersWorld.EditorTools.ArtLibrary;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Builds ConstructionTown.unity: one compact connected town. Hub-and-spoke: the Construction Yard in the middle,
    /// a square loop road (±60 m) with spurs to it, the House Plot north of the loop, the Roadworks on the east road,
    /// and the fenced (coming later) Playground site to the south. See Documentation/ART_DIRECTION.md for scale.
    /// </summary>
    public static class TownBuilder
    {
        public const string ScenePath = E.Root + "/Scenes/ConstructionTown.unity";
        private const int BuildingMask = 1 << ProjectConfigurator.BuildingLayer;

        private static Transform s_Env;
        private static readonly List<AmbientMotion.Item> s_Ambient = new List<AmbientMotion.Item>();
        private static readonly List<(Vector3 a, Vector3 b, float w)> s_Roads = new List<(Vector3, Vector3, float)>();
        private static readonly List<(Vector3 c, float r)> s_Keepout = new List<(Vector3, float)>();
        private static Dictionary<VehicleType, VehicleProfile> s_Profiles;

        public static void Build(Dictionary<VehicleType, VehicleProfile> profiles)
        {
            s_Profiles = profiles;
            s_Ambient.Clear();
            s_Roads.Clear();
            s_Keepout.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            s_Env = new GameObject("Environment").transform;

            Lighting();
            Camera cam = MainCamera();
            Ground();
            Roads();
            Transform yardView = Yard(out Dictionary<VehicleType, Vehicle> vehicles, out LoadingBay gravelBay, out TownSession.Decoration[] decorations);
            HousePlot();
            Roadworks();
            Playground();
            Landmarks();
            Trees();
            Clouds();
            Boundaries();
            RecoveryPoints();

            // ---------------- systems
            var systems = new GameObject("Systems");
            var input = systems.AddComponent<TouchInputController>();
            var actions = systems.AddComponent<VehicleActionController>();
            var interaction = systems.AddComponent<VehicleInteraction>();
            E.Set(interaction, "m_Camera", cam.GetComponent<VehicleCameraController>());
            E.Set(interaction, "m_Input", input);
            E.Set(interaction, "m_Actions", actions);
            E.Set(interaction, "m_YardView", yardView);

            var session = systems.AddComponent<TownSession>();
            var so = new SerializedObject(session);
            SerializedProperty deco = so.FindProperty("m_Decorations");
            deco.arraySize = decorations.Length;
            for (int i = 0; i < decorations.Length; i++)
            {
                deco.GetArrayElementAtIndex(i).FindPropertyRelative("unlockId").stringValue = decorations[i].unlockId;
                deco.GetArrayElementAtIndex(i).FindPropertyRelative("root").objectReferenceValue = decorations[i].root;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var pool = systems.AddComponent<SoilPilePool>();
            E.Set(pool, "m_PilePrefab", PropBuilder.Get("SoilPile"));
            E.SetArray(pool, "m_KindMaterials", new Object[] { A.Soil, A.Gravel, A.Brick });

            // Guidance arrow: a flat, slightly tilted arrow floating beside the vehicle.
            Transform arrowRoot = new GameObject("GuidanceArrow").transform;
            Transform arrowTilt = A.Pivot("Tilt", arrowRoot, Vector3.zero, new Vector3(-50f, 0f, 0f)); // tilted toward the camera so it reads as an arrow
            GameObject arrowOutline = A.Part("Outline", arrowTilt, A.Quad(2.6f, 2.6f), A.UnlitCutout("ArrowOutline", IconPainter.Sprite("icon_arrow3d").texture, "#3A2E2A"), new Vector3(0f, -0.01f, 0f));
            GameObject arrowFill = A.Part("Fill", arrowTilt, A.Quad(2.2f, 2.2f), A.UnlitCutout("ArrowFill", IconPainter.Sprite("icon_arrow3d").texture, "#FFD447"), Vector3.zero);
            A.NoShadows(arrowRoot.gameObject);
            var arrow = systems.AddComponent<GuidanceArrow>();
            E.Set(arrow, "m_Interaction", interaction);
            E.Set(arrow, "m_Arrow", arrowRoot);
            arrowRoot.gameObject.SetActive(false);

            var fx = systems.AddComponent<CelebrationFX>();
            E.Set(fx, "m_Sparkle", Instance("FX_Sparkle", systems.transform, Vector3.zero).GetComponent<ParticleSystem>());
            E.Set(fx, "m_Confetti", Instance("FX_Confetti", systems.transform, Vector3.zero).GetComponent<ParticleSystem>());
            E.Set(fx, "m_Poof", Instance("FX_Poof", systems.transform, Vector3.zero).GetComponent<ParticleSystem>());

            var ambient = systems.AddComponent<AmbientMotion>();
            var ambientSo = new SerializedObject(ambient);
            SerializedProperty items = ambientSo.FindProperty("m_Items");
            items.arraySize = s_Ambient.Count;
            for (int i = 0; i < s_Ambient.Count; i++)
            {
                SerializedProperty e = items.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("target").objectReferenceValue = s_Ambient[i].target;
                e.FindPropertyRelative("kind").enumValueIndex = (int)s_Ambient[i].kind;
                e.FindPropertyRelative("speed").floatValue = s_Ambient[i].speed;
                e.FindPropertyRelative("amount").floatValue = s_Ambient[i].amount;
            }
            ambientSo.ApplyModifiedPropertiesWithoutUndo();

            var ordered = new List<Vehicle> { vehicles[VehicleType.Excavator], vehicles[VehicleType.DumpTruck], vehicles[VehicleType.Bulldozer], vehicles[VehicleType.RoadRoller] };
            UIBuilder.BuildTownHud(interaction, actions, input, cam, ordered);
            UIBuilder.EventSystem();

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            Debug.Log($"[LBW] Town built: {s_Ambient.Count} ambient items, {s_Roads.Count} road segments.");
        }

        // ------------------------------------------------------------------ helpers
        private static GameObject Instance(string prefab, Transform parent, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            GameObject p = PropBuilder.Get(prefab);
            if (p == null)
            {
                Debug.LogError("[LBW] Missing prefab " + prefab);
                return new GameObject(prefab);
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        private static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent != null ? parent : s_Env, false);
            return t;
        }

        private static void Static(GameObject go)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        private static void Ambient(Transform t, AmbientMotion.Kind kind, float speed, float amount)
        {
            if (t != null) s_Ambient.Add(new AmbientMotion.Item { target = t, kind = kind, speed = speed, amount = amount });
        }

        private static ActivityZone Zone(string name, Transform parent, Vector3 pos, float radius, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var z = go.AddComponent<ActivityZone>();
            E.Set(z, "m_Shape", (int)ActivityZone.ZoneShape.Circle);
            E.Set(z, "m_Radius", radius);
            return z;
        }

        private static ActivityZone BoxZone(string name, Transform parent, Vector3 pos, Vector2 size, float yaw)
        {
            ActivityZone z = Zone(name, parent, pos, 1f, yaw);
            E.Set(z, "m_Shape", (int)ActivityZone.ZoneShape.Box);
            E.Set(z, "m_Size", size);
            return z;
        }

        /// <summary>A pale striped band on the ground: push debris anywhere into it.</summary>
        private static ActivityZone ClearStrip(Transform parent, Vector3 centre, Vector2 size)
        {
            ActivityZone z = BoxZone("ClearStrip", parent, centre, size, 0f);
            GameObject band = A.Part("Band", z.transform, A.GroundRect($"ClearStrip_{size.x:0}x{size.y:0}", size.x, size.y, 2f), A.UnlitCutout("ClearStrip", IconPainter.Texture("tex_stripes"), "#FFFFFF"), new Vector3(0f, 0.05f, 0f));
            A.NoShadows(band);
            return z;
        }

        private static ActivityMarker Marker(Transform parent, Vector3 focus, VehicleType vehicle, float height = 6.5f)
        {
            var root = new GameObject("Marker_" + vehicle);
            root.transform.SetParent(parent, false);
            root.transform.position = focus;
            Transform icon = A.Pivot("Icon", root.transform, new Vector3(0f, height, 0f));
            Texture2D portrait = s_Profiles[vehicle].portrait != null ? s_Profiles[vehicle].portrait.texture : null;
            A.Part("Back", icon, A.UprightQuad(3.2f, 3.2f), A.UnlitCutout("MarkerBack", IconPainter.Sprite("ui_circle_outlined").texture), new Vector3(0f, 0f, 0.02f));
            A.Part("Portrait", icon, A.UprightQuad(2.8f, 2.8f), A.UnlitCutout("Marker_" + vehicle, portrait), Vector3.zero);
            A.Part("Pin", root.transform, A.Cone(0.5f, 1.2f), A.Colour("MarkerPin", "#FFC93C", 0.4f), new Vector3(0f, height - 2.6f, 0f), new Vector3(180f, 0f, 0f));
            Transform ring = A.Part("Ring", root.transform, A.Ring(3.4f, 4.1f), A.UnlitCutout("MarkerRing", null, "#FFD447"), new Vector3(0f, 0.07f, 0f)).transform;
            A.NoShadows(root);
            var m = root.AddComponent<ActivityMarker>();
            E.Set(m, "m_Icon", icon);
            E.Set(m, "m_Ring", ring);
            return m;
        }

        private static void SetupActivity(ActivityBase a, string id, VehicleType vehicle, Transform focus, ActivityMarker marker, int[] goals = null, bool repeatable = false, int bonus = 0)
        {
            E.Set(a, "m_Id", id);
            E.Set(a, "m_Vehicle", (int)vehicle);
            E.Set(a, "m_Focus", focus);
            E.Set(a, "m_Marker", marker);
            E.Set(a, "m_Repeatable", repeatable);
            E.Set(a, "m_CompletionBonus", bonus);
            if (goals != null)
            {
                var so = new SerializedObject(a);
                SerializedProperty g = so.FindProperty("m_StageGoals");
                g.arraySize = goals.Length;
                for (int i = 0; i < goals.Length; i++) g.GetArrayElementAtIndex(i).intValue = goals[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetReveals(ProjectActivity p, List<(GameObject[] show, GameObject[] hide)> reveals)
        {
            var so = new SerializedObject(p);
            SerializedProperty arr = so.FindProperty("m_Reveals");
            arr.arraySize = reveals.Count;
            for (int i = 0; i < reveals.Count; i++)
            {
                SerializedProperty e = arr.GetArrayElementAtIndex(i);
                SerializedProperty show = e.FindPropertyRelative("show");
                SerializedProperty hide = e.FindPropertyRelative("hide");
                show.arraySize = reveals[i].show.Length;
                for (int k = 0; k < reveals[i].show.Length; k++) show.GetArrayElementAtIndex(k).objectReferenceValue = reveals[i].show[k];
                hide.arraySize = reveals[i].hide.Length;
                for (int k = 0; k < reveals[i].hide.Length; k++) hide.GetArrayElementAtIndex(k).objectReferenceValue = reveals[i].hide[k];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RetintRenderers(GameObject go, Material m)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = m;
        }

        // ------------------------------------------------------------------ scene basics
        private static void Lighting()
        {
            var sun = new GameObject("Sun", typeof(Light));
            var l = sun.GetComponent<Light>();
            l.type = LightType.Directional;
            l.color = E.Hex("#FFF2DC");
            l.intensity = 1.2f;
            l.shadows = LightShadows.Hard;
            l.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = E.Hex("#C9E8FF");
            RenderSettings.ambientEquatorColor = E.Hex("#E9E2D0");
            RenderSettings.ambientGroundColor = E.Hex("#9DB98A");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = E.Hex("#BFE6FF");
            RenderSettings.fogStartDistance = 110f;
            RenderSettings.fogEndDistance = 240f;
            RenderSettings.skybox = null;
        }

        private static Camera MainCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = E.Hex("#A8DEFF");
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 260f;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            var follow = go.AddComponent<VehicleCameraController>();
            E.Set(follow, "m_Obstacles", BuildingMask);
            go.transform.position = new Vector3(0f, 14f, -22f);
            go.transform.LookAt(new Vector3(0f, 0f, 0f));
            return cam;
        }

        private static void Ground()
        {
            GameObject g = A.Part("Ground", s_Env, A.Ground("Ground_260", 260f, 8f), A.Grass, Vector3.zero);
            var c = g.AddComponent<BoxCollider>();
            c.center = new Vector3(0f, -0.5f, 0f);
            c.size = new Vector3(260f, 1f, 260f);
            g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Static(g);
            // Gentle practice hill: shows smooth handling on slopes.
            GameObject hill = A.Part("PracticeHill", s_Env, A.Hill("Hill_12_1p6", 12f, 1.6f), A.Grass, new Vector3(-35f, 0f, -30f));
            hill.AddComponent<MeshCollider>();
            Static(hill);
            s_Keepout.Add((new Vector3(-35f, 0f, -30f), 13f));
        }

        private static void Road(string name, Vector3 a, Vector3 b, float width, Material mat, float y)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            GameObject road = A.Part(name, Group("Roads"), A.Strip($"Strip_{name}", len, width, 10f), mat, (a + b) * 0.5f + Vector3.up * y);
            road.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            road.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Static(road);
            s_Roads.Add((a, b, width));
        }

        private static void Roads()
        {
            const float w = 10f;
            Road("LoopNorth", new Vector3(-55, 0, 60), new Vector3(55, 0, 60), w, A.Road, 0.04f);
            Road("LoopSouth", new Vector3(55, 0, -60), new Vector3(-55, 0, -60), w, A.Road, 0.04f);
            Road("LoopWest", new Vector3(-60, 0, -55), new Vector3(-60, 0, 55), w, A.Road, 0.04f);
            Road("LoopEastSouth", new Vector3(60, 0, -55), new Vector3(60, 0, -14), w, A.Road, 0.04f);
            Road("LoopEastWorks", new Vector3(60, 0, -14), new Vector3(60, 0, 14), w, A.RoadPlain, 0.04f);
            Road("LoopEastNorth", new Vector3(60, 0, 14), new Vector3(60, 0, 55), w, A.Road, 0.04f);
            foreach (Vector2 c in new[] { new Vector2(-60, -60), new Vector2(60, -60), new Vector2(-60, 60), new Vector2(60, 60) })
            {
                GameObject pad = A.Part("Corner", Group("Roads"), A.Disc(7.2f, 0.04f, 0.01f, 32), A.Tarmac, new Vector3(c.x, 0.02f, c.y));
                pad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                Static(pad);
            }
            Road("SpurNorth", new Vector3(0, 0, 16), new Vector3(0, 0, 55), 9f, A.Road, 0.03f);
            Road("SpurSouth", new Vector3(0, 0, -16), new Vector3(0, 0, -55), 9f, A.Road, 0.03f);
            Road("SpurEast", new Vector3(23, 0, 0), new Vector3(55, 0, 0), 9f, A.Road, 0.03f);
            Road("HouseDrive", new Vector3(34, 0, 65), new Vector3(34, 0, 70), 8f, A.RoadPlain, 0.03f);
        }

        // ------------------------------------------------------------------ construction yard
        private static Transform Yard(out Dictionary<VehicleType, Vehicle> vehicles, out LoadingBay gravelBay, out TownSession.Decoration[] decorations)
        {
            Transform yard = Group("ConstructionYard");
            GameObject sand = A.Part("YardGround", yard, A.GroundRect("Yard_46x32", 46f, 32f, 6f), A.Sand, new Vector3(0f, 0.035f, 0f));
            sand.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Static(sand);

            // Fence with gates north, south and east.
            Transform fences = Group("Fence", yard);
            for (float x = -21f; x <= 21f; x += 4.2f)
            {
                if (Mathf.Abs(x) > 5.5f) { Static(Instance("Fence", fences, new Vector3(x, 0f, 16f))); Static(Instance("Fence", fences, new Vector3(x, 0f, -16f))); }
            }
            for (float z = -14f; z <= 14f; z += 4.2f)
            {
                Static(Instance("Fence", fences, new Vector3(-23f, 0f, z), 90f));
                if (Mathf.Abs(z) > 5.5f) Static(Instance("Fence", fences, new Vector3(23f, 0f, z), 90f));
            }
            foreach (Vector3 p in new[] { new Vector3(-6.5f, 0, 16), new Vector3(6.5f, 0, 16), new Vector3(-6.5f, 0, -16), new Vector3(6.5f, 0, -16), new Vector3(23, 0, 6.5f), new Vector3(23, 0, -6.5f) })
            {
                GameObject flag = Instance("FlagPole", yard, p);
                Ambient(flag.transform.Find("Flag"), AmbientMotion.Kind.Wave, 2.4f, 9f);
            }

            // Parking bays (vehicles face the yard, toward the overview camera).
            vehicles = new Dictionary<VehicleType, Vehicle>();
            (VehicleType t, float x, string col)[] bays =
            {
                (VehicleType.Excavator, -16f, "#FFC93C"), (VehicleType.DumpTruck, -9f, "#FF595E"),
                (VehicleType.Bulldozer, 9f, "#FF9F1C"), (VehicleType.RoadRoller, 16f, "#6BCB77")
            };
            Transform parking = Group("Parking", yard);
            foreach (var bay in bays)
            {
                GameObject mark = A.Part("Bay_" + bay.t, parking, A.Ring(2.6f, 3.0f), A.UnlitCutout("Bay_" + bay.t, null, bay.col), new Vector3(bay.x, 0.06f, 9f));
                A.NoShadows(mark);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(VehicleBuilder.Load(bay.t));
                go.transform.SetPositionAndRotation(new Vector3(bay.x, 0.02f, 9f), Quaternion.Euler(0f, 180f, 0f));
                vehicles[bay.t] = go.GetComponent<Vehicle>();
            }

            // Repeatable dig pit (always open: free-play digging).
            Transform pit = Group("DigPit", yard);
            GameObject mound = Instance("SoilMound", pit, new Vector3(-17f, 0.03f, -8f));
            GameObject stock = Instance("SoilPile", pit, new Vector3(-8f, 0.03f, -12f), 0f, 1.6f);
            ActivityZone digArea = Zone("DigArea", pit, new Vector3(-14f, 0f, -6f), 8f);
            Transform pitFocus = A.Pivot("Focus", pit, new Vector3(-17f, 0f, -8f));
            var pitAct = pit.gameObject.AddComponent<ExcavationActivity>();
            var pitZone = pit.gameObject.AddComponent<DigZone>();
            E.Set(pitZone, "m_Zone", digArea);
            E.Set(pitZone, "m_Activity", pitAct);
            E.Set(pitZone, "m_Pile", mound.transform);
            E.Set(pitZone, "m_Stockpile", stock.transform);
            E.Set(pitAct, "m_DigZone", pitZone);
            SetupActivity(pitAct, "yard_dig", VehicleType.Excavator, pitFocus, Marker(pit, new Vector3(-17f, 0f, -8f), VehicleType.Excavator), new[] { 3 }, true);
            s_Keepout.Add((new Vector3(-14f, 0f, -6f), 9f));

            // Gravel hopper: trucks drive in underneath (north-south) and tap Load.
            Transform hopperGroup = Group("GravelHopper", yard);
            Instance("Hopper", hopperGroup, new Vector3(13f, 0f, -8f));
            GameObject lump = Instance("SoilPile", hopperGroup, new Vector3(13f, 4.2f, -8f), 0f, 1.2f);
            RetintRenderers(lump, A.Gravel);
            ActivityZone bayZone = Zone("LoadArea", hopperGroup, new Vector3(13f, 0f, -8f), 5.5f);
            gravelBay = hopperGroup.gameObject.AddComponent<LoadingBay>();
            E.Set(gravelBay, "m_Zone", bayZone);
            E.Set(gravelBay, "m_Kind", (int)LoadKind.Gravel);
            E.Set(gravelBay, "m_DropVisual", lump.transform);
            GameObject bayRing = A.Part("BayRing", hopperGroup, A.Ring(4.6f, 5.2f), A.UnlitCutout("LoadRing", null, "#1982C4"), new Vector3(13f, 0.06f, -8f));
            A.NoShadows(bayRing);

            Static(Instance("Shed", yard, new Vector3(-31f, 0f, 9f), 90f));
            foreach (Vector3 p in new[] { new Vector3(-22f, 0, 18f), new Vector3(22f, 0, 18f), new Vector3(-22f, 0, -18f), new Vector3(22f, 0, -18f) })
                Static(Instance("Lamp", yard, p));

            // Unlockable decorations.
            GameObject bunting = Instance("Bunting", yard, new Vector3(0f, 0f, 16.5f));
            GameObject balloons = new GameObject("Balloons");
            balloons.transform.SetParent(yard, false);
            GameObject arch = Instance("BalloonArch", balloons.transform, new Vector3(23.5f, 0f, 0f), 90f);
            Ambient(arch.transform, AmbientMotion.Kind.Bob, 1.2f, 0.15f);
            Instance("Fountain", balloons.transform, new Vector3(2f, 0.03f, -11f));
            decorations = new[]
            {
                new TownSession.Decoration { unlockId = "decor_flags", root = bunting },
                new TownSession.Decoration { unlockId = "decor_balloons", root = balloons }
            };

            s_Keepout.Add((Vector3.zero, 26f));
            Transform view = A.Pivot("YardView", yard, new Vector3(0f, 0f, -1f));
            return view;
        }

        // ------------------------------------------------------------------ house plot (first project)
        private static void HousePlot()
        {
            Transform plot = Group("HousePlot");
            Vector3 c = new Vector3(34f, 0f, 82f);
            GameObject dirt = A.Part("PlotGround", plot, A.GroundRect("Plot_26x24", 26f, 24f, 6f), A.Dirt, c + new Vector3(0f, 0.035f, -1f));
            dirt.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Static(dirt);
            Transform fence = Group("PlotFence", plot);
            for (float x = 23f; x <= 45f; x += 4.2f) Static(Instance("Fence", fence, new Vector3(x, 0f, 93.5f)));
            for (float z = 72f; z <= 92f; z += 4.2f)
            {
                Static(Instance("Fence", fence, new Vector3(21f, 0f, z), 90f));
                Static(Instance("Fence", fence, new Vector3(47f, 0f, z), 90f));
            }
            foreach (float x in new[] { 23f, 27.2f, 40.8f, 45f }) Static(Instance("Fence", fence, new Vector3(x, 0f, 70.5f)));
            foreach (Vector3 p in new[] { new Vector3(30f, 0, 70f), new Vector3(38f, 0, 70f) })
                Ambient(Instance("FlagPole", plot, p).transform.Find("Flag"), AmbientMotion.Kind.Wave, 2.2f, 9f);

            var project = plot.gameObject.AddComponent<HouseConstructionActivity>();
            Transform projectFocus = A.Pivot("Focus", plot, new Vector3(34f, 0f, 80f));

            // Stage 0: bulldozer clears rocks, logs and crates into the skip.
            Transform clear = Group("S0_Clear", plot);
            GameObject skip = Instance("Skip", clear, new Vector3(44.5f, 0.03f, 82f), 90f);
            // Forgiving target: the whole marked strip along the east side of the plot counts, not just the skip.
            ActivityZone skipZone = ClearStrip(clear, new Vector3(43.5f, 0f, 82f), new Vector2(7f, 22f));
            var slots = new List<Transform>();
            for (int i = 0; i < 6; i++)
                slots.Add(A.Pivot("Slot" + i, skip.transform, new Vector3(-1.4f + (i % 3) * 1.4f, 0.4f + (i / 3) * 0.5f, -0.4f + (i % 2) * 0.6f)));
            string[] kinds = { "DebrisRock", "DebrisLog", "DebrisCrate", "DebrisRock", "DebrisLog" };
            // Open ground west of every rock, so a bulldozer can always line up and push east.
            Vector3[] spots = { new Vector3(33f, 0.03f, 76f), new Vector3(34f, 0.03f, 80f), new Vector3(33f, 0.03f, 84f), new Vector3(34f, 0.03f, 88f), new Vector3(33f, 0.03f, 91.5f) };
            var debris = new List<Object>();
            for (int i = 0; i < kinds.Length; i++) debris.Add(Instance(kinds[i], clear, spots[i], i * 47f).GetComponent<PushableDebris>());
            var clearAct = clear.gameObject.AddComponent<BulldozerClearingActivity>();
            E.SetArray(clearAct, "m_Debris", debris.ToArray());
            E.Set(clearAct, "m_Target", skipZone);
            E.Set(clearAct, "m_BoundsRadius", 14f); // pushed out of the plot (e.g. through the fence) → pops back home
            E.SetArray(clearAct, "m_ClearSlots", slots.ToArray());
            Transform clearFocus = A.Pivot("Focus", clear, new Vector3(36f, 0f, 84f));
            SetupActivity(clearAct, "house_clear", VehicleType.Bulldozer, clearFocus, Marker(clear, new Vector3(36f, 0f, 84f), VehicleType.Bulldozer));

            // Stage 1: excavator digs the foundation; the slab is poured when it is done.
            Transform dig = Group("S1_Dig", plot);
            GameObject mound = Instance("SoilMound", dig, new Vector3(34f, 0.03f, 84f), 30f, 0.85f);
            GameObject stock = Instance("SoilPile", dig, new Vector3(25.5f, 0.03f, 76f), 0f, 1.4f);
            Transform outline = Group("FoundationOutline", dig);
            foreach (var (pos, size) in new[] { (new Vector3(34f, 0.06f, 88f), new Vector3(9f, 0.08f, 0.25f)), (new Vector3(34f, 0.06f, 80f), new Vector3(9f, 0.08f, 0.25f)), (new Vector3(29.5f, 0.06f, 84f), new Vector3(0.25f, 0.08f, 8f)), (new Vector3(38.5f, 0.06f, 84f), new Vector3(0.25f, 0.08f, 8f)) })
                A.Part("Line", outline, A.Box(size.x, size.y, size.z, 0.03f), A.White, pos);
            ActivityZone digArea = Zone("DigArea", dig, new Vector3(34f, 0f, 81f), 8.5f);
            var digAct = dig.gameObject.AddComponent<ExcavationActivity>();
            var digZone = dig.gameObject.AddComponent<DigZone>();
            E.Set(digZone, "m_Zone", digArea);
            E.Set(digZone, "m_Activity", digAct);
            E.Set(digZone, "m_Pile", mound.transform);
            E.Set(digZone, "m_Stockpile", stock.transform);
            E.Set(digAct, "m_DigZone", digZone);
            E.Set(digAct, "m_OpenVisual", outline.gameObject);
            Transform digFocus = A.Pivot("Focus", dig, new Vector3(34f, 0f, 84f));
            SetupActivity(digAct, "house_dig", VehicleType.Excavator, digFocus, Marker(dig, new Vector3(34f, 0f, 84f), VehicleType.Excavator), new[] { 3 });

            // House parts (pre-authored positions; revealed stage by stage).
            Transform house = Group("House", plot);
            GameObject slab = A.Part("FoundationSlab", house, A.Box(9.2f, 0.5f, 8.2f, 0.12f), A.Concrete, new Vector3(34f, 0.25f, 84f));
            Material wallMat = A.Colour("HouseWallMain", "#FFE3B3", 0.2f);
            GameObject[] walls =
            {
                A.Part("WallBack", house, A.Box(8f, 3.2f, 0.4f, 0.1f), wallMat, new Vector3(34f, 2.1f, 87.6f)),
                A.Part("WallLeft", house, A.Box(0.4f, 3.2f, 7.6f, 0.1f), wallMat, new Vector3(30.2f, 2.1f, 84f)),
                A.Part("WallRight", house, A.Box(0.4f, 3.2f, 7.6f, 0.1f), wallMat, new Vector3(37.8f, 2.1f, 84f)),
                A.Part("WallFrontL", house, A.Box(3f, 3.2f, 0.4f, 0.1f), wallMat, new Vector3(31.5f, 2.1f, 80.4f)),
                A.Part("WallFrontR", house, A.Box(3f, 3.2f, 0.4f, 0.1f), wallMat, new Vector3(36.5f, 2.1f, 80.4f)),
                A.Part("Lintel", house, A.Box(2.2f, 0.8f, 0.4f, 0.1f), wallMat, new Vector3(34f, 3.3f, 80.4f))
            };
            Material roofMat = A.RoofRed;
            GameObject[] roof =
            {
                A.Part("RoofFront", house, A.Box(9.4f, 0.35f, 5.2f, 0.12f), roofMat, new Vector3(34f, 4.75f, 82.1f), new Vector3(-33f, 0f, 0f)),
                A.Part("RoofBack", house, A.Box(9.4f, 0.35f, 5.2f, 0.12f), roofMat, new Vector3(34f, 4.75f, 85.9f), new Vector3(33f, 0f, 0f)),
                A.Part("GableL", house, A.Box(0.3f, 2.4f, 2.4f, 0.08f), wallMat, new Vector3(30.2f, 4.3f, 84f), new Vector3(45f, 0f, 0f)),
                A.Part("GableR", house, A.Box(0.3f, 2.4f, 2.4f, 0.08f), wallMat, new Vector3(37.8f, 4.3f, 84f), new Vector3(45f, 0f, 0f)),
                A.Part("Chimney", house, A.Box(0.8f, 1.8f, 0.8f, 0.1f), A.Brick, new Vector3(36.2f, 5.6f, 86f))
            };
            GameObject[] doors =
            {
                A.Part("Door", house, A.Box(1.6f, 2.4f, 0.16f, 0.08f), A.Colour("DoorBlue", "#1982C4", 0.3f), new Vector3(34f, 1.7f, 80.3f)),
                A.Part("Knob", house, A.Sphere(0.1f), A.Colour("Gold", "#FFC93C", 0.6f), new Vector3(34.55f, 1.6f, 80.18f)),
                A.Part("WindowL", house, A.Box(1.4f, 1.2f, 0.14f, 0.06f), A.Glass, new Vector3(31.5f, 2.4f, 80.25f)),
                A.Part("WindowR", house, A.Box(1.4f, 1.2f, 0.14f, 0.06f), A.Glass, new Vector3(36.5f, 2.4f, 80.25f)),
                A.Part("WindowSideL", house, A.Box(0.14f, 1.2f, 1.6f, 0.06f), A.Glass, new Vector3(30f, 2.4f, 84f)),
                A.Part("WindowSideR", house, A.Box(0.14f, 1.2f, 1.6f, 0.06f), A.Glass, new Vector3(38f, 2.4f, 84f))
            };
            var garden = new List<GameObject>();
            garden.Add(A.Part("Path", house, A.Box(1.6f, 0.06f, 6f, 0.02f), A.Concrete, new Vector3(34f, 0.07f, 77f)));
            foreach (Vector3 p in new[] { new Vector3(30f, 0.03f, 78.5f), new Vector3(38f, 0.03f, 78.5f), new Vector3(28.5f, 0.03f, 88f), new Vector3(39.5f, 0.03f, 88f) })
                garden.Add(Instance("Bush", house, p));
            GameObject tree = Instance("TreeRound", house, new Vector3(26.5f, 0.03f, 90f));
            garden.Add(tree);
            GameObject mailbox = A.Part("Mailbox", house, A.Box(0.5f, 0.45f, 0.7f, 0.12f), A.Colour("MailRed", "#FF595E", 0.4f), new Vector3(36.6f, 1.1f, 75f));
            A.Part("MailPost", mailbox.transform, A.Box(0.12f, 2f, 0.12f, 0.04f), A.Wood, new Vector3(0f, -1.0f, 0f));
            garden.Add(mailbox);
            GameObject resident = Instance("Resident", house, new Vector3(31.5f, 0.03f, 77.5f), 160f, 1.6f);
            Ambient(resident.transform.Find("WaveArm"), AmbientMotion.Kind.Wave, 5f, 35f);
            garden.Add(resident);
            GameObject smoke = Instance("FX_Smoke", house, new Vector3(36.2f, 6.6f, 86f));
            E.Set(project, "m_ChimneySmoke", smoke.GetComponent<ParticleSystem>());

            // Stages 2–5: dump truck deliveries at the front of the house.
            var deliveries = new List<ActivityBase>();
            string[] ids = { "house_walls", "house_roof", "house_doors", "house_garden" };
            for (int i = 0; i < ids.Length; i++)
            {
                Transform g = Group("S" + (i + 2) + "_" + ids[i], plot);
                var act = g.gameObject.AddComponent<MaterialDeliveryActivity>();
                var zone = g.gameObject.AddComponent<DeliveryZone>();
                ActivityZone area = Zone("DropArea", g, new Vector3(34f, 0f, 75.5f), 7.5f);
                Transform drop = A.Pivot("DropPoint", g, new Vector3(34f, 0f, 77.5f));
                E.Set(zone, "m_Zone", area);
                E.Set(zone, "m_Activity", act);
                E.Set(zone, "m_DropPoint", drop);
                Transform focus = A.Pivot("Focus", g, new Vector3(34f, 0f, 76f));
                SetupActivity(act, ids[i], VehicleType.DumpTruck, focus, Marker(g, new Vector3(34f, 0f, 76f), VehicleType.DumpTruck, 5.5f));
                deliveries.Add(act);
            }

            var stages = new List<ActivityBase> { clearAct, digAct };
            stages.AddRange(deliveries);
            E.SetArray(project, "m_Stages", stages.ToArray());
            E.Set(project, "m_ProjectBonus", 3);
            SetupActivity(project, "house", VehicleType.DumpTruck, projectFocus, null);
            SetReveals(project, new List<(GameObject[], GameObject[])>
            {
                (new GameObject[0], new GameObject[0]),
                (new[] { slab }, new[] { mound, outline.gameObject }),
                (walls, new GameObject[0]),
                (roof, new GameObject[0]),
                (doors, new GameObject[0]),
                (garden.ToArray(), new GameObject[0])
            });

            // Brick depot opposite the driveway, inside the loop: short, happy delivery trips.
            Transform depot = Group("BrickDepot");
            GameObject dpad = A.Part("Pad", depot, A.GroundRect("Depot_14x10", 14f, 10f, 6f), A.Sand, new Vector3(34f, 0.035f, 49f));
            dpad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Static(dpad);
            foreach (Vector3 p in new[] { new Vector3(40f, 0.03f, 47f), new Vector3(40f, 0.03f, 51f), new Vector3(28f, 0.03f, 47f) })
                Static(Instance("BrickPallet", depot, p, 15f));
            GameObject stack = Instance("BrickPallet", depot, new Vector3(40f, 0.9f, 49f));
            ActivityZone loadArea = Zone("LoadArea", depot, new Vector3(34f, 0f, 49.5f), 5.5f);
            var bricks = depot.gameObject.AddComponent<LoadingBay>();
            E.Set(bricks, "m_Zone", loadArea);
            E.Set(bricks, "m_Kind", (int)LoadKind.Bricks);
            E.Set(bricks, "m_DropVisual", stack.transform);
            GameObject ring = A.Part("BayRing", depot, A.Ring(4.6f, 5.2f), A.UnlitCutout("LoadRing", null, "#1982C4"), new Vector3(34f, 0.06f, 49.5f));
            A.NoShadows(ring);

            s_Keepout.Add((c, 17f));
            s_Keepout.Add((new Vector3(34f, 0f, 49f), 9f));
        }

        // ------------------------------------------------------------------ roadworks
        private static void Roadworks()
        {
            Transform works = Group("Roadworks");
            var project = works.gameObject.AddComponent<RoadworksActivity>();
            Transform focus = A.Pivot("Focus", works, new Vector3(60f, 0f, 0f));

            Transform clear = Group("S0_Clear", works);
            GameObject skip = Instance("Skip", clear, new Vector3(71.5f, 0.03f, 0f), 90f);
            ActivityZone skipZone = ClearStrip(clear, new Vector3(70f, 0f, 0f), new Vector2(8f, 26f));
            var slots = new List<Transform>();
            for (int i = 0; i < 3; i++) slots.Add(A.Pivot("Slot" + i, skip.transform, new Vector3(-1.3f + i * 1.3f, 0.4f, 0f)));
            var debris = new List<Object>
            {
                Instance("DebrisCone", clear, new Vector3(58f, 0.05f, -7f)).GetComponent<PushableDebris>(),
                Instance("DebrisRock", clear, new Vector3(62f, 0.05f, 1f), 40f).GetComponent<PushableDebris>(),
                Instance("DebrisCone", clear, new Vector3(59f, 0.05f, 8f)).GetComponent<PushableDebris>()
            };
            var clearAct = clear.gameObject.AddComponent<BulldozerClearingActivity>();
            E.SetArray(clearAct, "m_Debris", debris.ToArray());
            E.Set(clearAct, "m_Target", skipZone);
            E.Set(clearAct, "m_BoundsRadius", 18f);
            E.SetArray(clearAct, "m_ClearSlots", slots.ToArray());
            Transform cf = A.Pivot("Focus", clear, new Vector3(61f, 0f, 0f));
            SetupActivity(clearAct, "road_clear", VehicleType.Bulldozer, cf, Marker(clear, new Vector3(61f, 0f, 0f), VehicleType.Bulldozer));

            Transform fill = Group("S1_Fill", works);
            var fillAct = fill.gameObject.AddComponent<MaterialDeliveryActivity>();
            var fillZone = fill.gameObject.AddComponent<DeliveryZone>();
            ActivityZone area = Zone("DropArea", fill, new Vector3(60f, 0f, 0f), 9f);
            E.Set(fillZone, "m_Zone", area);
            E.Set(fillZone, "m_Activity", fillAct);
            E.Set(fillZone, "m_DropPoint", A.Pivot("DropPoint", fill, new Vector3(60f, 0f, 0f)));
            Transform ff = A.Pivot("Focus", fill, new Vector3(60f, 0f, 0f));
            SetupActivity(fillAct, "road_fill", VehicleType.DumpTruck, ff, Marker(fill, new Vector3(60f, 0f, 0f), VehicleType.DumpTruck));

            Transform roll = Group("S2_Roll", works);
            var patches = new List<Object>();
            foreach (float z in new[] { -10f, -3.5f, 3.5f, 10f })
            {
                Transform p = Group("Patch", roll);
                p.position = new Vector3(60f, 0f, z);
                GameObject pothole = A.Part("Pothole", p, A.Disc(2.2f, 0.03f, 0.01f, 20), A.Pothole, new Vector3(0f, 0.05f, 0f), null, new Vector3(1.3f, 1f, 1f));
                GameObject rough = A.Part("Rough", p, A.Dome(2.3f, 0.35f), A.Gravel, new Vector3(0f, 0.02f, 0f), null, new Vector3(1.3f, 1f, 1f));
                for (int i = 0; i < 5; i++)
                    A.Part("Lump", rough.transform, A.Sphere(0.3f), A.Rock, new Vector3(Mathf.Cos(i * 1.3f) * 1.3f, 0.25f, Mathf.Sin(i * 1.3f) * 1.3f));
                GameObject smooth = A.Part("Smooth", p, A.Disc(2.4f, 0.03f, 0.01f, 20), A.Tarmac, new Vector3(0f, 0.05f, 0f), null, new Vector3(1.3f, 1f, 1f));
                A.NoShadows(p.gameObject);
                var rp = p.gameObject.AddComponent<RoughPatch>();
                E.Set(rp, "m_Pothole", pothole);
                E.Set(rp, "m_Rough", rough);
                E.Set(rp, "m_Smooth", smooth);
                E.Set(rp, "m_Radius", 3.4f);
                rough.SetActive(false);
                smooth.SetActive(false);
                patches.Add(rp);
            }
            var rollAct = roll.gameObject.AddComponent<RoadRollerActivity>();
            E.SetArray(rollAct, "m_Patches", patches.ToArray());
            Transform rf = A.Pivot("Focus", roll, new Vector3(60f, 0f, 0f));
            SetupActivity(rollAct, "road_roll", VehicleType.RoadRoller, rf, Marker(roll, new Vector3(60f, 0f, 0f), VehicleType.RoadRoller));

            // Finishing touches revealed at the end: fresh lines, barriers removed.
            GameObject lines = A.Part("FreshLines", works, A.Strip("Strip_FreshLines", 28f, 10f, 10f), A.Road, new Vector3(60f, 0.07f, 0f));
            lines.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            GameObject barrierN = Instance("Barrier", works, new Vector3(60f, 0.03f, 16.5f));
            GameObject barrierS = Instance("Barrier", works, new Vector3(60f, 0.03f, -16.5f));

            E.SetArray(project, "m_Stages", new Object[] { clearAct, fillAct, rollAct });
            E.Set(project, "m_ProjectBonus", 3);
            SetupActivity(project, "roadworks", VehicleType.RoadRoller, focus, null);
            SetReveals(project, new List<(GameObject[], GameObject[])>
            {
                (new GameObject[0], new GameObject[0]),
                (new GameObject[0], new GameObject[0]),
                (new[] { lines }, new[] { barrierN, barrierS })
            });
            s_Keepout.Add((new Vector3(64f, 0f, 0f), 13f));
        }

        // ------------------------------------------------------------------ playground (visible, opens in a later update)
        private static void Playground()
        {
            Transform pg = Group("PlaygroundSite_ComingLater");
            Vector3 c = new Vector3(-36f, 0f, -84f);
            GameObject dirt = A.Part("Ground", pg, A.GroundRect("Playground_20x14", 20f, 14f, 6f), A.Dirt, c + new Vector3(0f, 0.035f, 0f));
            dirt.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (float x = -8f; x <= 8.1f; x += 4.2f)
            {
                Instance("Fence", pg, c + new Vector3(x, 0f, 7.5f));
                Instance("Fence", pg, c + new Vector3(x, 0f, -7.5f));
            }
            for (float z = -5f; z <= 5.1f; z += 4.2f)
            {
                Instance("Fence", pg, c + new Vector3(-10.5f, 0f, z), 90f);
                Instance("Fence", pg, c + new Vector3(10.5f, 0f, z), 90f);
            }
            GameObject sign = Instance("SignPost", pg, c + new Vector3(0f, 0f, 9.5f), 180f);
            SetSignIcon(sign, "icon_slide");
            Instance("DebrisCrate", pg, c + new Vector3(-3f, 0.03f, 1f)).GetComponent<PushableDebris>().enabled = false;
            Instance("BrickPallet", pg, c + new Vector3(3f, 0.03f, -1f));
            Static(pg.gameObject);
            s_Keepout.Add((c, 14f));
        }

        private static void SetSignIcon(GameObject sign, string icon)
        {
            Material m = A.UnlitCutout("Sign_" + icon, IconPainter.Sprite(icon).texture);
            foreach (string n in new[] { "Icon", "IconBack" })
            {
                Transform t = sign.transform.Find(n);
                if (t != null) t.GetComponent<Renderer>().sharedMaterial = m;
            }
        }

        // ------------------------------------------------------------------ landmarks and dressing
        private static void Landmarks()
        {
            Transform lm = Group("Landmarks");
            GameObject mill = Instance("Windmill", lm, new Vector3(-82f, 0f, 30f), 60f);
            Ambient(mill.transform.Find("Blades"), AmbientMotion.Kind.Spin, 35f, 0f);
            Static(Instance("WaterTower", lm, new Vector3(28f, 0f, 24f)));
            s_Keepout.Add((new Vector3(-82f, 0f, 30f), 7f));
            s_Keepout.Add((new Vector3(28f, 0f, 24f), 5f));
            string[] houses = { "TownHouseA", "TownHouseB", "TownHouseC", "TownHouseD" };
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = new Vector3(2f + i * 14f, 0f, -75f);
                Static(Instance(houses[i], lm, p, 180f));
                s_Keepout.Add((p, 6f));
                Vector3 q = new Vector3(-75f, 0f, -36f + i * 18f);
                Static(Instance(houses[(i + 2) % 4], lm, q, -90f));
                s_Keepout.Add((q, 6f));
            }
            GameObject toHouse = Instance("SignPost", lm, new Vector3(6f, 0f, 53f), 90f);
            SetSignIcon(toHouse, "icon_house");
            GameObject toRoad = Instance("SignPost", lm, new Vector3(53f, 0f, -6f), 0f);
            SetSignIcon(toRoad, "icon_road");
            GameObject toYard = Instance("SignPost", lm, new Vector3(-6f, 0f, -53f), 180f);
            SetSignIcon(toYard, "icon_home");
            Static(toHouse);
            Static(toRoad);
            Static(toYard);
            // Lamps along the loop.
            for (int i = -2; i <= 2; i++)
            {
                Static(Instance("Lamp", lm, new Vector3(i * 22f + 11f, 0f, 66.5f)));
                Static(Instance("Lamp", lm, new Vector3(i * 22f + 11f, 0f, -66.5f)));
            }
        }

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a, ap = p - a;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
            Vector3 q = a + ab * t;
            q.y = p.y = 0f;
            return Vector3.Distance(p, q);
        }

        private static bool Free(Vector3 p, float margin)
        {
            foreach (var r in s_Roads) if (DistanceToSegment(p, r.a, r.b) < r.w * 0.5f + margin) return false;
            foreach (var k in s_Keepout) if (Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(k.c.x, 0f, k.c.z)) < k.r + margin * 0.5f) return false;
            foreach (Vector2 c in new[] { new Vector2(-60, -60), new Vector2(60, -60), new Vector2(-60, 60), new Vector2(60, 60) })
                if (Vector2.Distance(new Vector2(p.x, p.z), c) < 9f + margin) return false;
            return true;
        }

        private static void Trees()
        {
            Transform trees = Group("Trees");
            var rng = new System.Random(2024);
            int placed = 0;
            for (int attempt = 0; attempt < 900 && placed < 85; attempt++)
            {
                var p = new Vector3((float)rng.NextDouble() * 180f - 90f, 0f, (float)rng.NextDouble() * 180f - 90f);
                if (!Free(p, 3f)) continue;
                bool pine = rng.NextDouble() < 0.4;
                float s = 0.85f + (float)rng.NextDouble() * 0.5f;
                GameObject t = Instance(pine ? "TreePine" : "TreeRound", trees, p, (float)rng.NextDouble() * 360f, s);
                Ambient(t.transform.Find("Sway"), AmbientMotion.Kind.Sway, 0.8f + (float)rng.NextDouble() * 0.4f, 1.6f);
                placed++;
            }
            // A soft green wall of trees beyond the edge so the world never looks like it ends.
            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f * Mathf.PI * 2f;
                float r = 104f + (i % 3) * 4f;
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                p.x = Mathf.Clamp(p.x, -112f, 112f);
                p.z = Mathf.Clamp(p.z, -112f, 112f);
                GameObject t = Instance(i % 2 == 0 ? "TreePine" : "TreeRound", trees, p, i * 37f, 1.4f + (i % 4) * 0.15f);
                Static(t);
            }
            for (int i = 0; i < 30; i++)
            {
                var p = new Vector3((float)rng.NextDouble() * 170f - 85f, 0.03f, (float)rng.NextDouble() * 170f - 85f);
                if (Free(p, 2f)) Static(Instance("Bush", trees, p, (float)rng.NextDouble() * 360f));
            }
            Debug.Log($"[LBW] Trees placed: {placed}");
        }

        private static void Clouds()
        {
            Transform sky = Group("Sky");
            var rng = new System.Random(5);
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3((float)rng.NextDouble() * 260f - 130f, 42f + (float)rng.NextDouble() * 12f, (float)rng.NextDouble() * 200f - 60f);
                GameObject c = Instance("Cloud", sky, p, (float)rng.NextDouble() * 60f, 1.2f + (float)rng.NextDouble());
                Ambient(c.transform, AmbientMotion.Kind.Drift, 1.2f + (float)rng.NextDouble(), 0f);
            }
        }

        private static void Boundaries()
        {
            Transform b = Group("SoftBoundary");
            foreach (var (pos, size) in new[]
                     {
                         (new Vector3(0f, 3f, 97f), new Vector3(200f, 6f, 2f)), (new Vector3(0f, 3f, -97f), new Vector3(200f, 6f, 2f)),
                         (new Vector3(97f, 3f, 0f), new Vector3(2f, 6f, 200f)), (new Vector3(-97f, 3f, 0f), new Vector3(2f, 6f, 200f))
                     })
            {
                var go = new GameObject("Wall");
                go.transform.SetParent(b, false);
                go.transform.position = pos;
                go.AddComponent<BoxCollider>().size = size;
            }
        }

        private static void RecoveryPoints()
        {
            Transform rp = Group("RecoveryPoints");
            void Point(Vector3 p, float yaw)
            {
                var go = new GameObject("Recovery");
                go.transform.SetParent(rp, false);
                go.transform.SetPositionAndRotation(p + Vector3.up * 0.05f, Quaternion.Euler(0f, yaw, 0f));
                go.AddComponent<RecoveryPoint>();
            }
            for (float x = -45f; x <= 45f; x += 22.5f) { Point(new Vector3(x, 0f, 60f), 90f); Point(new Vector3(x, 0f, -60f), -90f); }
            for (float z = -45f; z <= 45f; z += 22.5f) { Point(new Vector3(-60f, 0f, z), 0f); if (Mathf.Abs(z) > 15f) Point(new Vector3(60f, 0f, z), 180f); }
            Point(new Vector3(60f, 0f, 22f), 180f);
            Point(new Vector3(0f, 0f, 35f), 0f);
            Point(new Vector3(0f, 0f, -35f), 180f);
            Point(new Vector3(38f, 0f, 0f), 90f);
            Point(new Vector3(0f, 0f, 2f), 0f);
            Point(new Vector3(-6f, 0f, -4f), 0f);
            Point(new Vector3(34f, 0f, 70f), 0f);
        }
    }
}
