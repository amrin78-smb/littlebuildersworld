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
    /// and the Playground (third project, opened by an Area unlock) to the south-west. See Documentation/ART_DIRECTION.md for scale.
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
            Pond();
            Trees();
            Meadow();
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
            E.SetArray(pool, "m_KindMaterials", new Object[] { A.Soil, A.Gravel, A.Brick, A.Wood });

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
            UIBuilder.TownHud hud = UIBuilder.BuildTownHud(interaction, actions, input, cam, ordered);
            UIBuilder.EventSystem();

            // The helper: picks what to say (speech bubble + voice) from what the child is doing.
            var guide = systems.AddComponent<Guide.GuideDirector>();
            E.Set(guide, "m_Interaction", interaction);
            E.Set(guide, "m_Actions", actions);
            E.Set(guide, "m_Bubble", hud.Guide);

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

        private static void SetupActivity(ActivityBase a, string id, VehicleType vehicle, Transform focus, ActivityMarker marker, int[] goals = null, bool repeatable = false, int bonus = 0, string hint = null)
        {
            E.Set(a, "m_Id", id);
            E.Set(a, "m_HintKey", hint ?? "");
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
            // Beyond the play area: more grass and soft rolling hills fading into the fog, so the world has a horizon.
            GameObject far = A.Part("FarGround", s_Env, A.Ground("Ground_520", 520f, 8f), A.Grass, new Vector3(0f, -0.08f, 0f));
            far.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Static(far);
            var hrng = new System.Random(77);
            Material[] hillMats = { A.Colour("HillGreen", "#7CC860", 0.05f), A.Colour("HillGreenDark", "#5FB356", 0.05f) };
            for (int i = 0; i < 22; i++)
            {
                float a = (i + (float)hrng.NextDouble() * 0.5f) / 22f * Mathf.PI * 2f;
                float dist = 165f + (float)hrng.NextDouble() * 40f;
                float rad = 32f + (float)hrng.NextDouble() * 22f;
                float h = 10f + (float)hrng.NextDouble() * 14f;
                GameObject hl = A.Part("FarHill", s_Env, A.Hill($"FarHill_{rad:0}_{h:0}", rad, h), hillMats[i % 2], new Vector3(Mathf.Cos(a) * dist, -0.1f, Mathf.Sin(a) * dist));
                hl.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                Static(hl);
            }
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
            GameObject rainbow = Instance("Rainbow", yard, new Vector3(0f, 0.03f, -16.8f));
            decorations = new[]
            {
                new TownSession.Decoration { unlockId = "decor_flags", root = bunting },
                new TownSession.Decoration { unlockId = "decor_balloons", root = balloons },
                new TownSession.Decoration { unlockId = "decor_rainbow", root = rainbow }
            };

            // The foreman (the helper in the corner of the screen) waves from between the parking bays.
            GameObject foreman = Instance("Foreman", yard, new Vector3(0f, 0.03f, 11.5f), 180f, 1.5f);
            Ambient(foreman.transform.Find("WaveArm"), AmbientMotion.Kind.Wave, 4f, 35f);

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
            // Clear of the dig mound (34,84; hidden until clearing is done anyway) and 3 m from the north fence.
            Vector3[] spots = { new Vector3(33f, 0.03f, 76f), new Vector3(34f, 0.03f, 80f), new Vector3(29f, 0.03f, 84f), new Vector3(34f, 0.03f, 88f), new Vector3(28f, 0.03f, 90f) };
            var debris = new List<Object>();
            for (int i = 0; i < kinds.Length; i++) debris.Add(Instance(kinds[i], clear, spots[i], i * 47f).GetComponent<PushableDebris>());
            var clearAct = clear.gameObject.AddComponent<BulldozerClearingActivity>();
            E.SetArray(clearAct, "m_Debris", debris.ToArray());
            E.Set(clearAct, "m_Target", skipZone);
            E.Set(clearAct, "m_BoundsRadius", 14f);
            // Inside of the plot fence: anything that ends up beyond it (unreachable) pops back home.
            E.Set(clearAct, "m_AreaCentre", new Vector3(34f, 0f, 82f));
            E.Set(clearAct, "m_AreaHalfSize", new Vector2(12.5f, 11f));
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
            // White frames, cross bars and flower boxes (children, so they appear with the windows).
            string[] flowerCols = { "#FF8FC7", "#FFE066", "#FF595E" };
            for (int w = 2; w < 6; w++)
            {
                Transform win = doors[w].transform;
                bool side = w >= 4;
                float out_ = side ? (w == 4 ? -1f : 1f) : -1f; // outward direction along the facing axis
                Vector3 Fs(float a, float b, float depth) => side ? new Vector3(depth, b, a) : new Vector3(a, b, depth);
                A.Part("Frame", win, A.Box(Fs(1.62f, 1.42f, 0.1f).x, Fs(1.62f, 1.42f, 0.1f).y, Fs(1.62f, 1.42f, 0.1f).z, 0.05f), A.White, Fs(0f, 0f, -out_ * 0.03f));
                A.Part("BarV", win, A.Box(Fs(0.08f, 1.2f, 0.2f).x, Fs(0.08f, 1.2f, 0.2f).y, Fs(0.08f, 1.2f, 0.2f).z, 0.02f), A.White, Vector3.zero);
                A.Part("BarH", win, A.Box(Fs(1.4f, 0.08f, 0.2f).x, Fs(1.4f, 0.08f, 0.2f).y, Fs(1.4f, 0.08f, 0.2f).z, 0.02f), A.White, Vector3.zero);
                A.Part("FlowerBox", win, A.Box(Fs(1.5f, 0.28f, 0.34f).x, Fs(1.5f, 0.28f, 0.34f).y, Fs(1.5f, 0.28f, 0.34f).z, 0.06f), A.Wood, Fs(0f, -0.78f, out_ * 0.22f));
                for (int f = 0; f < 3; f++)
                    A.Part("Flower", win, A.Sphere(0.14f), A.Colour("Flower" + f, flowerCols[f], 0.2f), Fs(-0.45f + f * 0.45f, -0.57f, out_ * 0.22f));
            }
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
                SetupActivity(act, ids[i], VehicleType.DumpTruck, focus, Marker(g, new Vector3(34f, 0f, 76f), VehicleType.DumpTruck, 5.5f), hint: "house_deliver");
                deliveries.Add(act);
            }

            var stages = new List<ActivityBase> { clearAct, digAct };
            stages.AddRange(deliveries);
            E.SetArray(project, "m_Stages", stages.ToArray());
            E.Set(project, "m_ProjectBonus", 3);
            SetupActivity(project, "house", VehicleType.DumpTruck, projectFocus, null);
            SetReveals(project, new List<(GameObject[], GameObject[])>
            {
                (new[] { mound }, new GameObject[0]), // the dirt to dig appears once the plot is clear
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
                // A real-looking hole: darker middle, broken bits of road around the edge, a few pebbles.
                A.Part("Deep", pothole.transform, A.Disc(1.3f, 0.03f, 0.01f, 16), A.Colour("PotholeDeep", "#28231F", 0.05f), new Vector3(0.2f, 0.01f, -0.1f));
                Material chunk = A.Colour("BrokenRoad", "#6E7380", 0.15f);
                for (int i = 0; i < 9; i++)
                {
                    float a = i / 9f * Mathf.PI * 2f + z;
                    A.Part("Chunk", pothole.transform, A.Box(0.55f, 0.1f, 0.35f, 0.04f), chunk, new Vector3(Mathf.Cos(a) * 2.05f, 0.03f, Mathf.Sin(a) * 2.05f), new Vector3(i * 7f, a * Mathf.Rad2Deg * -1f + 90f, i % 2 == 0 ? 6f : -5f));
                }
                for (int i = 0; i < 4; i++)
                    A.Part("Pebble", pothole.transform, A.Sphere(0.12f), A.Gravel, new Vector3(Mathf.Cos(i * 2.1f + z) * 1.0f, 0.04f, Mathf.Sin(i * 2.1f + z) * 0.8f));
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

        // ------------------------------------------------------------------ playground (third project, opens with an Area unlock)
        private static void Playground()
        {
            Transform pg = Group("Playground");
            Vector3 c = new Vector3(-34f, 0f, -82f);
            GameObject dirt = A.Part("PlotGround", pg, A.GroundRect("Playground_28x22", 28f, 22f, 6f), A.Dirt, c + new Vector3(0f, 0.035f, 0f));
            dirt.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Static(dirt);
            Transform fence = Group("Fence", pg);
            for (float x = -46f; x <= -21.9f; x += 4f)
            {
                Static(Instance("Fence", fence, new Vector3(x, 0f, -93f)));
                if (x < -39.5f || x > -28.5f) Static(Instance("Fence", fence, new Vector3(x, 0f, -71f))); // gate in the north side
            }
            for (float z = -91f; z <= -72.9f; z += 4.5f)
            {
                Static(Instance("Fence", fence, new Vector3(-48f, 0f, z), 90f));
                Static(Instance("Fence", fence, new Vector3(-20f, 0f, z), 90f));
            }
            Static(Instance("Fence", fence, new Vector3(-40.4f, 0f, -71f)));
            Static(Instance("Fence", fence, new Vector3(-27.6f, 0f, -71f)));
            foreach (Vector3 p in new[] { new Vector3(-40f, 0, -70.2f), new Vector3(-28f, 0, -70.2f) })
                Ambient(Instance("FlagPole", pg, p).transform.Find("Flag"), AmbientMotion.Kind.Wave, 2.2f, 9f);
            GameObject sign = Instance("SignPost", pg, new Vector3(-43f, 0f, -68f), 180f);
            SetSignIcon(sign, "icon_slide");
            Static(sign);

            var project = pg.gameObject.AddComponent<PlaygroundActivity>();
            Transform projectFocus = A.Pivot("Focus", pg, c);
            // Closed until the Area unlock: a barrier across the gate.
            GameObject gateBarrier = Instance("Barrier", pg, new Vector3(-34f, 0.03f, -70.6f), 0f, 1.8f);
            GameObject gateBarrier2 = Instance("Barrier", pg, new Vector3(-30f, 0.03f, -70.6f), 0f, 1.8f);
            GameObject gateBarrier3 = Instance("Barrier", pg, new Vector3(-38f, 0.03f, -70.6f), 0f, 1.8f);
            foreach (GameObject g in new[] { gateBarrier, gateBarrier2, gateBarrier3 })
            {
                // Solid while locked (removed with the barrier on unlock).
                var bc = g.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.85f, 0f);
                bc.size = new Vector3(3.2f, 1.7f, 0.5f);
            }

            // Stage 0: bulldozer pushes old boxes and logs onto the striped strip along the east fence.
            Transform clear = Group("S0_Clear", pg);
            GameObject skip = Instance("Skip", clear, new Vector3(-22.5f, 0.03f, -82f), 90f);
            ActivityZone strip = ClearStrip(clear, new Vector3(-23.5f, 0f, -82f), new Vector2(7f, 18f));
            var slots = new List<Transform>();
            for (int i = 0; i < 6; i++)
                slots.Add(A.Pivot("Slot" + i, skip.transform, new Vector3(-1.4f + (i % 3) * 1.4f, 0.4f + (i / 3) * 0.5f, -0.4f + (i % 2) * 0.6f)));
            string[] kinds = { "DebrisCrate", "DebrisLog", "DebrisCrate", "DebrisRock" };
            Vector3[] spots = { new Vector3(-35f, 0.03f, -88f), new Vector3(-34f, 0.03f, -84f), new Vector3(-35f, 0.03f, -80f), new Vector3(-34f, 0.03f, -76f) };
            var debris = new List<Object>();
            for (int i = 0; i < kinds.Length; i++) debris.Add(Instance(kinds[i], clear, spots[i], i * 61f).GetComponent<PushableDebris>());
            var clearAct = clear.gameObject.AddComponent<BulldozerClearingActivity>();
            E.SetArray(clearAct, "m_Debris", debris.ToArray());
            E.Set(clearAct, "m_Target", strip);
            E.Set(clearAct, "m_BoundsRadius", 14f);
            E.Set(clearAct, "m_AreaCentre", new Vector3(-34f, 0f, -82f));
            E.Set(clearAct, "m_AreaHalfSize", new Vector2(13.5f, 10.5f));
            E.SetArray(clearAct, "m_ClearSlots", slots.ToArray());
            Transform clearFocus = A.Pivot("Focus", clear, new Vector3(-31f, 0f, -82f));
            SetupActivity(clearAct, "pg_clear", VehicleType.Bulldozer, clearFocus, Marker(clear, new Vector3(-31f, 0f, -82f), VehicleType.Bulldozer));

            // Stage 1: excavator digs the sandpit hole; the sandpit appears.
            Transform dig = Group("S1_Dig", pg);
            Vector3 pit = new Vector3(-43f, 0f, -87f);
            GameObject mound = Instance("SoilMound", dig, pit + new Vector3(0f, 0.03f, 0f), 20f, 0.7f);
            GameObject stock = Instance("SoilPile", dig, new Vector3(-45.5f, 0.03f, -77f), 0f, 1.3f);
            Transform outline = Group("SandpitOutline", dig);
            foreach (var (pos, size) in new[] { (pit + new Vector3(0f, 0.06f, 2.2f), new Vector3(5.4f, 0.08f, 0.25f)), (pit + new Vector3(0f, 0.06f, -2.2f), new Vector3(5.4f, 0.08f, 0.25f)), (pit + new Vector3(-2.7f, 0.06f, 0f), new Vector3(0.25f, 0.08f, 4.4f)), (pit + new Vector3(2.7f, 0.06f, 0f), new Vector3(0.25f, 0.08f, 4.4f)) })
                A.Part("Line", outline, A.Box(size.x, size.y, size.z, 0.03f), A.White, pos);
            ActivityZone digArea = Zone("DigArea", dig, pit + new Vector3(1f, 0f, 3f), 8.5f);
            var digAct = dig.gameObject.AddComponent<ExcavationActivity>();
            var digZone = dig.gameObject.AddComponent<DigZone>();
            E.Set(digZone, "m_Zone", digArea);
            E.Set(digZone, "m_Activity", digAct);
            E.Set(digZone, "m_Pile", mound.transform);
            E.Set(digZone, "m_Stockpile", stock.transform);
            E.Set(digAct, "m_DigZone", digZone);
            E.Set(digAct, "m_OpenVisual", outline.gameObject);
            Transform digFocus = A.Pivot("Focus", dig, pit);
            SetupActivity(digAct, "pg_dig", VehicleType.Excavator, digFocus, Marker(dig, pit, VehicleType.Excavator), new[] { 3 });
            GameObject sandpit = Instance("Sandpit", pg, pit + new Vector3(0f, 0.03f, 0f));

            // Stages 2–3: dump truck brings play parts (two loads each) for the slide, then the swings.
            var deliveries = new List<ActivityBase>();
            string[] ids = { "pg_slide", "pg_swings" };
            for (int i = 0; i < ids.Length; i++)
            {
                Transform g = Group("S" + (i + 2) + "_" + ids[i], pg);
                var act = g.gameObject.AddComponent<MaterialDeliveryActivity>();
                var zone = g.gameObject.AddComponent<DeliveryZone>();
                ActivityZone area = Zone("DropArea", g, new Vector3(-34f, 0f, -76f), 7f);
                E.Set(zone, "m_Zone", area);
                E.Set(zone, "m_Activity", act);
                E.Set(zone, "m_DropPoint", A.Pivot("DropPoint", g, new Vector3(-34f, 0f, -78f)));
                Transform focus = A.Pivot("Focus", g, new Vector3(-34f, 0f, -77f));
                SetupActivity(act, ids[i], VehicleType.DumpTruck, focus, Marker(g, new Vector3(-34f, 0f, -77f), VehicleType.DumpTruck, 5.5f), new[] { 2 }, hint: "pg_deliver");
                deliveries.Add(act);
            }
            GameObject slide = Instance("Slide", pg, new Vector3(-29.5f, 0.03f, -89f));
            GameObject swings = Instance("Swings", pg, new Vector3(-38f, 0.03f, -90.5f));
            for (int i = 0; i < 2; i++) Ambient(swings.transform.Find("SwingPivot" + i), AmbientMotion.Kind.Sway, 1.6f + i * 0.3f, 14f);

            // Stage 4: road roller flattens four bumpy patches into soft play mats.
            Transform roll = Group("S4_Roll", pg);
            var patches = new List<Object>();
            Vector3[] mats = { new Vector3(-43f, 0f, -77f), new Vector3(-36.5f, 0f, -78.5f), new Vector3(-30f, 0f, -77f), new Vector3(-34f, 0f, -84.5f) };
            for (int k = 0; k < mats.Length; k++)
            {
                Transform p = Group("Patch", roll);
                p.position = mats[k];
                GameObject rough = A.Part("Rough", p, A.Dome(2.3f, 0.3f), A.SoilLight, new Vector3(0f, 0.02f, 0f), null, new Vector3(1.25f, 1f, 1.25f));
                for (int i = 0; i < 5; i++)
                    A.Part("Lump", rough.transform, A.Sphere(0.32f), A.Soil, new Vector3(Mathf.Cos(i * 1.3f) * 1.3f, 0.22f, Mathf.Sin(i * 1.3f) * 1.3f));
                GameObject smooth = A.Part("Mat", p, A.Disc(2.4f, 0.04f, 0.015f, 24), A.Colour(k % 2 == 0 ? "SoftMatBlue" : "SoftMatGreen", k % 2 == 0 ? "#4EA8DE" : "#6BCB77", 0.15f), new Vector3(0f, 0.05f, 0f), null, new Vector3(1.25f, 1f, 1.25f));
                // Play-mat pattern: a ring of bright dots.
                Material dot = A.Colour(k % 2 == 0 ? "SoftMatDotYellow" : "SoftMatDotWhite", k % 2 == 0 ? "#FFE066" : "#FFFFFF", 0.15f);
                A.Part("Dot", smooth.transform, A.Disc(0.45f, 0.03f, 0.01f, 12), dot, new Vector3(0f, 0.02f, 0f));
                for (int i = 0; i < 6; i++)
                    A.Part("Dot", smooth.transform, A.Disc(0.3f, 0.03f, 0.01f, 12), dot, new Vector3(Mathf.Cos(i * Mathf.PI / 3f) * 1.4f, 0.02f, Mathf.Sin(i * Mathf.PI / 3f) * 1.4f));
                A.NoShadows(p.gameObject);
                var rp = p.gameObject.AddComponent<RoughPatch>();
                E.Set(rp, "m_Rough", rough);
                E.Set(rp, "m_Smooth", smooth);
                E.Set(rp, "m_Radius", 3.4f);
                smooth.SetActive(false);
                patches.Add(rp);
            }
            var rollAct = roll.gameObject.AddComponent<RoadRollerActivity>();
            E.SetArray(rollAct, "m_Patches", patches.ToArray());
            E.Set(rollAct, "m_PotholesUntilOpen", false);
            Transform rf = A.Pivot("Focus", roll, new Vector3(-36f, 0f, -80f));
            SetupActivity(rollAct, "pg_roll", VehicleType.RoadRoller, rf, Marker(roll, new Vector3(-36f, 0f, -80f), VehicleType.RoadRoller));

            // Finished playground: climbing frame and roundabout on the new mats, children playing, a bench, bushes.
            var finale = new List<GameObject>
            {
                Instance("ClimbingFrame", pg, new Vector3(-43f, 0.06f, -77f), 15f),
                Instance("Roundabout", pg, new Vector3(-30f, 0.06f, -77f)),
                Instance("Bench", pg, new Vector3(-46.5f, 0.03f, -83f), 90f)
            };
            Ambient(finale[1].transform.Find("Spin"), AmbientMotion.Kind.Turn, 40f, 0f);
            Ambient(finale[0].transform.Find("Flag"), AmbientMotion.Kind.Wave, 2.5f, 12f);
            (string kid, Vector3 pos, float yaw)[] kids = { ("KidA", new Vector3(-36.5f, 0.06f, -78.5f), 20f), ("KidB", new Vector3(-31.5f, 0.06f, -79f), -30f), ("KidC", new Vector3(-27.5f, 0.03f, -87f), -60f) };
            foreach (var k in kids)
            {
                GameObject kidGo = Instance(k.kid, pg, k.pos, k.yaw, 1.5f);
                Ambient(kidGo.transform.Find("WaveArm"), AmbientMotion.Kind.Wave, 5f + k.yaw * 0.01f, 40f);
                finale.Add(kidGo);
            }
            foreach (Vector3 p in new[] { new Vector3(-46.5f, 0.03f, -88f), new Vector3(-46.5f, 0.03f, -74f), new Vector3(-22f, 0.03f, -73.5f) })
                finale.Add(Instance("Bush", pg, p));

            E.SetArray(project, "m_Stages", new Object[] { clearAct, digAct, deliveries[0], deliveries[1], rollAct });
            E.Set(project, "m_ProjectBonus", 3);
            E.Set(project, "m_RequiredUnlock", "area_playground");
            E.SetArray(project, "m_LockedVisuals", new Object[] { gateBarrier, gateBarrier2, gateBarrier3 });
            SetupActivity(project, "playground", VehicleType.RoadRoller, projectFocus, null);
            SetReveals(project, new List<(GameObject[], GameObject[])>
            {
                (new[] { mound }, new GameObject[0]),
                (new[] { sandpit }, new[] { mound, outline.gameObject }),
                (new[] { slide }, new GameObject[0]),
                (new[] { swings }, new GameObject[0]),
                (finale.ToArray(), new GameObject[0])
            });
            s_Keepout.Add((c, 18f));

            // Play-parts depot between the yard and the playground: short, happy truck trips.
            Transform depot = Group("PlayPartsDepot");
            Vector3 dc = new Vector3(-22f, 0f, -45f);
            GameObject pad = A.Part("Pad", depot, A.GroundRect("Depot_12x9", 12f, 9f, 6f), A.Sand, dc + new Vector3(0f, 0.035f, 0f));
            pad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Static(pad);
            foreach (Vector3 p in new[] { dc + new Vector3(-5f, 0.03f, 2.5f), dc + new Vector3(-5f, 0.03f, -1.5f) })
                Static(Instance("PartsPallet", depot, p, 10f));
            GameObject stackVis = Instance("PartsPallet", depot, dc + new Vector3(-5f, 0.9f, 0.5f));
            ActivityZone loadArea = Zone("LoadArea", depot, dc + new Vector3(0.5f, 0f, 0f), 5.5f);
            var bay = depot.gameObject.AddComponent<LoadingBay>();
            E.Set(bay, "m_Zone", loadArea);
            E.Set(bay, "m_Kind", (int)LoadKind.PlayParts);
            E.Set(bay, "m_DropVisual", stackVis.transform);
            GameObject ring = A.Part("BayRing", depot, A.Ring(4.6f, 5.2f), A.UnlitCutout("LoadRing", null, "#1982C4"), dc + new Vector3(0.5f, 0.06f, 0f));
            A.NoShadows(ring);
            s_Keepout.Add((dc, 9f));
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
            GameObject toPlay = Instance("SignPost", lm, new Vector3(-20f, 0f, -66.5f), 180f);
            SetSignIcon(toPlay, "icon_slide");
            Static(toPlay);
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

        /// <summary>A duck pond in the quiet north-west corner inside the loop.</summary>
        private static void Pond()
        {
            Vector3 c = new Vector3(-36f, 0f, 36f);
            GameObject pond = Instance("Pond", Group("Landmarks"), c, 20f);
            Static(pond);
            for (int i = 0; i < 2; i++)
            {
                Transform duck = pond.transform.Find("Duck" + i);
                duck.gameObject.isStatic = false;
                foreach (Transform t in duck.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                Ambient(duck, AmbientMotion.Kind.Bob, 1.6f + i * 0.5f, 0.06f);
            }
            s_Keepout.Add((c, 7f));
        }

        /// <summary>
        /// Flowers and grass tufts scattered over the grass, built as a few combined meshes (one per colour) so
        /// hundreds of little details cost only a handful of draw calls.
        /// </summary>
        private static void Meadow()
        {
            Transform meadow = Group("Meadow");
            var rng = new System.Random(99);
            string[] petalCols = { "#FF8FC7", "#FFE066", "#FFFFFF", "#B39CFF", "#FF7F6E" };
            var petals = new List<(Mesh, Matrix4x4)>[petalCols.Length];
            for (int i = 0; i < petals.Length; i++) petals[i] = new List<(Mesh, Matrix4x4)>();
            var centres = new List<(Mesh, Matrix4x4)>();
            var tufts = new List<(Mesh, Matrix4x4)>();
            // Very low-poly pieces: the meadow is hundreds of them.
            Mesh LowLathe(string n, params Vector2[] profile) => A.SaveMesh(n, LittleBuildersWorld.Core.MeshFactory.Lathe(profile, 6, n));
            Mesh petal = LowLathe("MeadowPetal", new Vector2(0f, 0f), new Vector2(0.24f, 0f), new Vector2(0.24f, 0.05f), new Vector2(0f, 0.07f));
            Mesh centre = LowLathe("MeadowCentre", new Vector2(0f, 0f), new Vector2(0.15f, 0f), new Vector2(0.13f, 0.08f), new Vector2(0f, 0.1f));
            Mesh blade = LowLathe("MeadowBlade", new Vector2(0f, 0f), new Vector2(0.09f, 0f), new Vector2(0.02f, 0.5f), new Vector2(0f, 0.55f));
            int flowers = 0, grass = 0;
            for (int attempt = 0; attempt < 4000 && (flowers < 150 || grass < 200); attempt++)
            {
                var p = new Vector3((float)rng.NextDouble() * 190f - 95f, 0f, (float)rng.NextDouble() * 190f - 95f);
                if (!Free(p, 1.5f)) continue;
                if (flowers < 150 && rng.NextDouble() < 0.45)
                {
                    // A little flower: five round petals around a yellow centre, lying on the grass.
                    int col = rng.Next(petalCols.Length);
                    float s = 0.8f + (float)rng.NextDouble() * 0.6f;
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k / 4f * Mathf.PI * 2f + 0.4f;
                        petals[col].Add((petal, Matrix4x4.TRS(p + new Vector3(Mathf.Cos(a) * 0.24f * s, 0.06f, Mathf.Sin(a) * 0.24f * s), Quaternion.identity, Vector3.one * s)));
                    }
                    centres.Add((centre, Matrix4x4.TRS(p + new Vector3(0f, 0.07f, 0f), Quaternion.identity, Vector3.one * s)));
                    flowers++;
                }
                else if (grass < 200)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k / 3f * Mathf.PI * 2f + (float)rng.NextDouble();
                        var tilt = Quaternion.Euler(Mathf.Cos(a) * 18f, 0f, Mathf.Sin(a) * 18f);
                        tufts.Add((blade, Matrix4x4.TRS(p + new Vector3(Mathf.Cos(a) * 0.1f, 0f, Mathf.Sin(a) * 0.1f), tilt, new Vector3(1f, 0.7f + (float)rng.NextDouble() * 0.6f, 1f))));
                    }
                    grass++;
                }
            }
            void Emit(string name, List<(Mesh, Matrix4x4)> parts, Material mat)
            {
                if (parts.Count == 0) return;
                Mesh m = A.SaveMesh("Meadow_" + name, LittleBuildersWorld.Core.MeshFactory.Combine("Meadow_" + name, parts.ToArray()));
                GameObject go = A.Part(name, meadow, m, mat, Vector3.zero);
                go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                Static(go);
            }
            for (int i = 0; i < petalCols.Length; i++) Emit("Petals" + i, petals[i], A.Colour("Petal" + i, petalCols[i], 0.2f));
            Emit("Centres", centres, A.Colour("FlowerYellow", "#FFE066"));
            Emit("Grass", tufts, A.Colour("GrassTuft", "#5DBB4C", 0.1f));
            Debug.Log($"[LBW] Meadow: {flowers} flowers, {grass} grass tufts.");
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
            Point(new Vector3(-34f, 0f, -67f), 180f);
        }
    }
}
