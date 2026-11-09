using System.Collections.Generic;
using LittleBuildersWorld.Vehicles;
using UnityEditor;
using UnityEngine;
using A = LittleBuildersWorld.EditorTools.ArtLibrary;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Builds the four vehicle prefabs from rounded procedural parts (custom, not placeholders of external art).
    /// Each has a distinct silhouette and colour, a friendly face on the windscreen, animated wheels/tracks and
    /// machinery, a beacon, and the gameplay components wired up. Forward is +Z, pivot on the ground.
    /// </summary>
    public static class VehicleBuilder
    {
        public const string Folder = E.Root + "/Prefabs/Vehicles";

        private sealed class Ctx
        {
            public GameObject Root;
            public Transform Body;
            public readonly List<Renderer> Primary = new List<Renderer>();
            public readonly List<Renderer> Secondary = new List<Renderer>();
            public readonly List<Transform> Wheels = new List<Transform>();
            public readonly List<Transform> SteerWheels = new List<Transform>();
            public readonly List<Renderer> Tracks = new List<Renderer>();
            public Transform Beacon;
            public Material PrimaryMat;
            public Material SecondaryMat;

            public GameObject P(string n, Transform parent, Mesh m, Vector3 pos, Vector3? euler = null, Vector3? scale = null)
            {
                GameObject g = A.Part(n, parent, m, PrimaryMat, pos, euler, scale);
                Primary.Add(g.GetComponent<Renderer>());
                return g;
            }

            public GameObject S(string n, Transform parent, Mesh m, Vector3 pos, Vector3? euler = null, Vector3? scale = null)
            {
                GameObject g = A.Part(n, parent, m, SecondaryMat, pos, euler, scale);
                Secondary.Add(g.GetComponent<Renderer>());
                return g;
            }
        }

        public static Dictionary<VehicleType, GameObject> BuildAll(IDictionary<VehicleType, VehicleProfile> profiles)
        {
            E.EnsureFolder(Folder);
            var result = new Dictionary<VehicleType, GameObject>
            {
                [VehicleType.Excavator] = Excavator(profiles[VehicleType.Excavator]),
                [VehicleType.DumpTruck] = DumpTruck(profiles[VehicleType.DumpTruck]),
                [VehicleType.Bulldozer] = Bulldozer(profiles[VehicleType.Bulldozer]),
                [VehicleType.RoadRoller] = RoadRoller(profiles[VehicleType.RoadRoller])
            };
            return result;
        }

        public static GameObject Load(VehicleType type) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{type}.prefab");

        // ------------------------------------------------------------------ shared
        private static PhysicsMaterial s_Frictionless;

        private static PhysicsMaterial Frictionless()
        {
            if (s_Frictionless != null) return s_Frictionless;
            string path = E.Root + "/Settings/PM_VehicleFrictionless.physicMaterial";
            E.EnsureFolder(E.Root + "/Settings");
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (m == null)
            {
                m = new PhysicsMaterial("VehicleFrictionless");
                AssetDatabase.CreateAsset(m, path);
            }
            m.dynamicFriction = 0f;
            m.staticFriction = 0f;
            m.bounciness = 0f;
            m.frictionCombine = PhysicsMaterialCombine.Minimum;
            m.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(m);
            s_Frictionless = m;
            return m;
        }

        private static Ctx Begin(string name, string primaryHex, string secondaryHex)
        {
            var c = new Ctx
            {
                Root = new GameObject(name),
                PrimaryMat = A.Colour("Vehicle_" + name + "_Primary", primaryHex, 0.45f),
                SecondaryMat = A.Colour("Vehicle_" + name + "_Secondary", secondaryHex, 0.4f)
            };
            c.Body = A.Pivot("Body", c.Root.transform, Vector3.zero);
            return c;
        }

        private static void Colliders(Ctx c, Vector3 boxCentre, Vector3 boxSize, float sphereZ, float sphereRadius)
        {
            PhysicsMaterial pm = Frictionless();
            var box = c.Root.AddComponent<BoxCollider>();
            box.center = boxCentre;
            box.size = boxSize;
            box.sharedMaterial = pm;
            foreach (float z in new[] { sphereZ, -sphereZ })
            {
                var s = c.Root.AddComponent<SphereCollider>();
                s.center = new Vector3(0f, sphereRadius, z);
                s.radius = sphereRadius;
                s.sharedMaterial = pm;
            }
        }

        private static void Eyes(Transform parent, Vector3 centre, float spacing, float size)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 p = centre + new Vector3(side * spacing, 0f, 0f);
                A.Part("Eye", parent, A.Sphere(size), A.White, p, null, new Vector3(1f, 1.15f, 0.45f));
                A.Part("Pupil", parent, A.Sphere(size * 0.5f), A.Ink, p + new Vector3(side * -0.02f, -size * 0.12f, size * 0.32f), null, new Vector3(1f, 1.1f, 0.5f));
                A.Part("Shine", parent, A.Sphere(size * 0.16f), A.White, p + new Vector3(side * -0.02f + size * 0.15f, size * 0.12f, size * 0.47f));
            }
        }

        private static Transform Beacon(Ctx c, Transform parent, Vector3 pos)
        {
            Transform pivot = A.Pivot("Beacon", parent, pos);
            A.Part("Base", parent, A.Disc(0.17f, 0.08f, 0.03f), A.DarkSteel, pos - new Vector3(0f, 0.05f, 0f));
            A.Part("Light", pivot, A.Dome(0.15f, 0.22f), A.Beacon, Vector3.zero);
            A.Part("Glint", pivot, A.Box(0.06f, 0.14f, 0.06f, 0.02f), A.White, new Vector3(0.12f, 0.1f, 0f));
            c.Beacon = pivot;
            return pivot;
        }

        private static Transform SpinWheel(Ctx c, Transform parent, Vector3 pos, float radius, float width, bool steer, Material tyre)
        {
            Transform mount = parent;
            if (steer)
            {
                mount = A.Pivot("SteerPivot", parent, pos);
                c.SteerWheels.Add(mount);
                pos = Vector3.zero;
            }
            Transform spin = A.Pivot("Wheel", mount, pos);
            bool rubber = tyre == A.Rubber;
            A.Part("Tyre", spin, rubber ? A.Tyre(radius, width) : A.Wheel(radius, width, Mathf.Min(0.15f, width * 0.3f)), tyre, Vector3.zero);
            if (rubber)
            {
                A.Part("Hub", spin, A.Wheel(radius * 0.55f, width + 0.04f, 0.04f), A.Steel, Vector3.zero);
                c.S("HubCap", spin, A.Wheel(radius * 0.3f, width + 0.1f, 0.05f), Vector3.zero);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    A.Part("Bolt", spin, A.Box(width + 0.12f, 0.07f, 0.07f, 0.02f), A.DarkSteel, new Vector3(0f, Mathf.Cos(a) * radius * 0.42f, Mathf.Sin(a) * radius * 0.42f));
                }
            }
            else
            {
                // Roller drum: bright end rings and a stripe so the roll is easy to see.
                foreach (float side in new[] { -1f, 1f })
                    c.S("DrumRing", spin, A.Wheel(radius * 0.72f, 0.08f, 0.03f), new Vector3(side * (width * 0.5f + 0.01f), 0f, 0f));
                A.Part("Bolt", spin, A.Box(width + 0.06f, radius * 0.2f, radius * 0.2f, 0.03f), A.DarkSteel, new Vector3(0f, radius * 0.3f, 0f));
            }
            c.Wheels.Add(spin);
            return spin;
        }

        private static void Track(Ctx c, float x, float length, float height, float width)
        {
            GameObject t = A.Part("Track", c.Body, A.TrackLoop(length, height, width), A.Track, new Vector3(x, 0f, 0f));
            t.GetComponent<MeshRenderer>().sharedMaterials = new[] { A.Track, A.DarkSteel };
            c.Tracks.Add(t.GetComponent<Renderer>());
            float r = height * 0.5f;
            foreach (float z in new[] { length * 0.5f - r, -(length * 0.5f - r) })
            {
                float side = Mathf.Sign(x);
                Transform spin = A.Pivot("Sprocket", c.Body, new Vector3(x + side * (width * 0.5f + 0.03f), r, z));
                A.Part("Disc", spin, A.Wheel(r * 0.7f, 0.06f, 0.02f), A.Steel, Vector3.zero);
                A.Part("Spoke", spin, A.Box(0.08f, r * 1.3f, 0.12f, 0.03f), A.DarkSteel, new Vector3(side * 0.03f, 0f, 0f));
                c.Wheels.Add(spin);
            }
            // Little road wheels between the sprockets.
            for (int i = 0; i < 3; i++)
            {
                float z = (i - 1) * (length - height) / 3f;
                float side = Mathf.Sign(x);
                Transform spin = A.Pivot("Roller", c.Body, new Vector3(x + side * (width * 0.5f + 0.03f), r * 0.62f, z));
                A.Part("Disc", spin, A.Wheel(r * 0.4f, 0.06f, 0.02f), A.Steel, Vector3.zero);
                A.Part("Cap", spin, A.Wheel(r * 0.16f, 0.1f, 0.02f), A.DarkSteel, Vector3.zero);
                c.Wheels.Add(spin);
            }
        }

        // ------------------------------------------------------------------ detail helpers (art pass)
        /// <summary>Rosy cheeks under the eyes: friendlier faces.</summary>
        private static void Cheeks(Transform parent, Vector3 centre, float spacing, float size)
        {
            foreach (float side in new[] { -1f, 1f })
                A.Part("Cheek", parent, A.Sphere(size), A.Cheek, centre + new Vector3(side * spacing, 0f, 0f), null, new Vector3(1f, 0.62f, 0.3f));
        }

        /// <summary>Rear lights facing -Z (or +Z when <paramref name="front"/>): red tail light + orange indicator each side.</summary>
        private static void RearLights(Transform parent, Vector3 centre, float spacing, float scale = 1f)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                A.Part("TailLight", parent, A.Box(0.3f * scale, 0.17f * scale, 0.07f, 0.04f), A.TailLight, centre + new Vector3(side * spacing, 0f, 0f));
                A.Part("Indicator", parent, A.Box(0.14f * scale, 0.17f * scale, 0.07f, 0.04f), A.Indicator, centre + new Vector3(side * (spacing - 0.25f * scale), 0f, 0f));
            }
        }

        /// <summary>Round headlights facing +Z.</summary>
        private static void HeadLights(Transform parent, Vector3 centre, float spacing, float radius)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 p = centre + new Vector3(side * spacing, 0f, 0f);
                A.Part("LightRim", parent, A.Wheel(radius * 1.25f, 0.08f, 0.03f), A.DarkSteel, p, new Vector3(0f, 90f, 0f));
                A.Part("HeadLight", parent, A.Wheel(radius, 0.12f, 0.05f), A.HeadLight, p + new Vector3(0f, 0f, 0.03f), new Vector3(0f, 90f, 0f));
            }
        }

        /// <summary>A little number plate facing -Z (a smiley "builder" plate, no letters).</summary>
        private static void Plate(Transform parent, Vector3 pos)
        {
            A.Part("Plate", parent, A.Box(0.56f, 0.22f, 0.04f, 0.04f), A.Colour("PlateYellow", "#FFE27A", 0.4f), pos);
            for (int i = 0; i < 3; i++)
                A.Part("PlateDot", parent, A.Box(0.09f, 0.09f, 0.03f, 0.02f), A.Ink, pos + new Vector3(-0.16f + i * 0.16f, 0f, -0.025f));
        }

        /// <summary>Grille slats across a face. Horizontal unless <paramref name="vertical"/>; the face normal is ±Z or ±X.</summary>
        private static void Slats(Transform parent, Vector3 centre, float width, float height, int count, Vector3 normal, bool vertical = false, Material mat = null)
        {
            mat = mat != null ? mat : A.Ink;
            bool alongX = Mathf.Abs(normal.z) > 0.5f; // face spans X (normal ±Z) or Z (normal ±X)
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : (i / (float)(count - 1) - 0.5f);
                Vector3 off;
                Vector3 size;
                if (vertical)
                {
                    off = alongX ? new Vector3(t * width, 0f, 0f) : new Vector3(0f, 0f, t * width);
                    size = alongX ? new Vector3(0.07f, height, 0.04f) : new Vector3(0.04f, height, 0.07f);
                }
                else
                {
                    off = new Vector3(0f, t * height, 0f);
                    size = alongX ? new Vector3(width, 0.07f, 0.04f) : new Vector3(0.04f, 0.07f, width);
                }
                A.Part("Slat", parent, A.Box(size.x, size.y, size.z, 0.02f), mat, centre + off + normal * 0.02f);
            }
        }

        /// <summary>Yellow/black hazard chevrons on a face whose normal is -Z.</summary>
        private static void HazardStripes(Transform parent, Vector3 centre, float width, float height, int count)
        {
            float step = width / count;
            for (int i = 0; i < count; i++)
            {
                float x = -width * 0.5f + step * (i + 0.5f);
                A.Part("Hazard", parent, A.Box(step, height, 0.04f, 0.02f), i % 2 == 0 ? A.Hazard : A.Ink, centre + new Vector3(x, 0f, 0f));
            }
        }

        private static void Pipe(Transform parent, Vector3 basePos, float height, float radius = 0.08f)
        {
            A.Part("Exhaust", parent, A.Disc(radius, height, 0.02f, 12), A.DarkSteel, basePos);
            A.Part("ExhaustCap", parent, A.Disc(radius * 1.35f, 0.12f, 0.03f, 12), A.Ink, basePos + new Vector3(0f, height - 0.06f, 0f));
        }

        /// <summary>
        /// Merges static parts that share a parent and a material into one mesh (one draw call each), so the extra
        /// detail costs little on the budget phone. Painted parts stay paintable (the merged renderer takes their place
        /// in the paint lists); tracks, inactive parts and anything with children are left alone.
        /// </summary>
        private static void MergeParts(Ctx c)
        {
            var tracks = new HashSet<Renderer>(c.Tracks);
            var groups = new Dictionary<(Transform, Material), List<MeshRenderer>>();
            var order = new List<(Transform, Material)>();
            foreach (MeshRenderer r in c.Root.GetComponentsInChildren<MeshRenderer>(true))
            {
                Transform t = r.transform;
                if (tracks.Contains(r) || !r.gameObject.activeSelf || t.childCount > 0 || r.sharedMaterials.Length != 1) continue;
                if (r.GetComponents<Component>().Length != 3) continue; // Transform + MeshFilter + MeshRenderer only
                var key = (t.parent, r.sharedMaterial);
                if (!groups.TryGetValue(key, out var list)) { groups[key] = list = new List<MeshRenderer>(); order.Add(key); }
                list.Add(r);
            }
            int n = 0;
            foreach (var key in order)
            {
                List<MeshRenderer> list = groups[key];
                if (list.Count < 2) continue;
                Transform parent = key.Item1;
                var parts = new (Mesh, Matrix4x4)[list.Count];
                for (int i = 0; i < list.Count; i++)
                    parts[i] = (list[i].GetComponent<MeshFilter>().sharedMesh, parent.worldToLocalMatrix * list[i].transform.localToWorldMatrix);
                string meshName = $"VM_{c.Root.name}_{n++:00}";
                Mesh mesh = A.SaveMesh(meshName, LittleBuildersWorld.Core.MeshFactory.Combine(meshName, parts));
                GameObject merged = A.Part("Merged_" + key.Item2.name, parent, mesh, key.Item2, Vector3.zero);
                var mr = merged.GetComponent<MeshRenderer>();
                bool primary = false, secondary = false;
                foreach (MeshRenderer r in list)
                {
                    primary |= c.Primary.Remove(r);
                    secondary |= c.Secondary.Remove(r);
                    Object.DestroyImmediate(r.gameObject);
                }
                if (primary) c.Primary.Add(mr);
                if (secondary) c.Secondary.Add(mr);
            }
        }

        private static GameObject Finish(Ctx c, VehicleProfile profile, float bubbleHeight, DebrisPusher.Mode pushMode, float halfWidth, float front, float back)
        {
            MergeParts(c);
            GameObject root = c.Root;
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = profile.mass;
            rb.isKinematic = true;
            var controller = root.AddComponent<VehicleController>();
            E.Set(controller, "m_Profile", profile);
            E.Set(controller, "m_GroundMask", (int)1); // Default layer = ground
            var vehicle = root.AddComponent<Vehicle>();
            Transform bubble = A.Pivot("BubbleAnchor", root.transform, new Vector3(0f, bubbleHeight, 0f));
            E.Set(vehicle, "m_Profile", profile);
            E.Set(vehicle, "m_Controller", controller);
            E.Set(vehicle, "m_BubbleAnchor", bubble);

            var visuals = root.AddComponent<VehicleVisuals>();
            E.Set(visuals, "m_Controller", controller);
            E.Set(visuals, "m_Profile", profile);
            E.Set(visuals, "m_Body", c.Body);
            E.Set(visuals, "m_Beacon", c.Beacon);
            E.SetArray(visuals, "m_Wheels", c.Wheels.ToArray());
            E.SetArray(visuals, "m_SteerWheels", c.SteerWheels.ToArray());
            E.SetArray(visuals, "m_TrackRenderers", c.Tracks.ToArray());

            root.AddComponent<VehicleRecovery>();
            root.AddComponent<AudioSource>();
            var audio = root.AddComponent<VehicleAudio>();
            E.Set(audio, "m_Controller", controller);
            E.Set(audio, "m_Profile", profile);

            var paint = root.AddComponent<VehiclePaint>();
            E.Set(paint, "m_Vehicle", vehicle);
            E.SetArray(paint, "m_Primary", c.Primary.ToArray());
            E.SetArray(paint, "m_Secondary", c.Secondary.ToArray());

            var pusher = root.AddComponent<DebrisPusher>();
            E.Set(pusher, "m_Mode", (int)pushMode);
            E.Set(pusher, "m_HalfWidth", halfWidth);
            E.Set(pusher, "m_Front", front);
            E.Set(pusher, "m_Back", back);

            A.SetLayer(root, ProjectConfigurator.VehicleLayer);
            return root;
        }

        private static GameObject Save(GameObject root, VehicleType type) => E.SavePrefab(root, $"{Folder}/{type}.prefab");

        // ------------------------------------------------------------------ Excavator
        private static GameObject Excavator(VehicleProfile profile)
        {
            Ctx c = Begin("Excavator", "#FFC93C", "#4B4E6D");
            Transform b = c.Body;
            Track(c, -1.0f, 3.6f, 0.85f, 0.7f);
            Track(c, 1.0f, 3.6f, 0.85f, 0.7f);
            A.Part("Undercarriage", b, A.Box(1.4f, 0.45f, 2.4f, 0.15f), A.DarkSteel, new Vector3(0f, 0.75f, 0f));
            A.Part("Turntable", b, A.Disc(0.85f, 0.18f, 0.05f), A.DarkSteel, new Vector3(0f, 0.95f, 0f));

            Transform cab = A.Pivot("Cab", b, new Vector3(0f, 1.0f, 0f));
            c.P("Deck", cab, A.Box(2.5f, 0.85f, 2.7f, 0.25f), new Vector3(0f, 0.45f, -0.1f));
            c.S("Counterweight", cab, A.Box(2.4f, 0.8f, 0.75f, 0.3f), new Vector3(0f, 0.55f, -1.35f));
            c.P("Cabin", cab, A.Box(1.15f, 1.4f, 1.3f, 0.28f), new Vector3(-0.6f, 1.55f, 0.45f));
            A.Part("Windscreen", cab, A.Box(0.95f, 0.75f, 0.06f, 0.03f), A.Glass, new Vector3(-0.6f, 1.8f, 1.1f));
            A.Part("SideWindow", cab, A.Box(0.06f, 0.7f, 0.9f, 0.03f), A.Glass, new Vector3(-1.17f, 1.8f, 0.45f));
            A.Part("SideWindowR", cab, A.Box(0.06f, 0.7f, 0.9f, 0.03f), A.Glass, new Vector3(-0.03f, 1.8f, 0.45f));
            Eyes(cab, new Vector3(-0.6f, 1.85f, 1.15f), 0.22f, 0.16f);
            Cheeks(cab, new Vector3(-0.6f, 1.3f, 1.1f), 0.36f, 0.11f);
            Beacon(c, cab, new Vector3(-0.6f, 2.32f, 0.45f));
            A.Part("Rail", cab, A.Box(0.08f, 0.5f, 1.4f, 0.03f), A.Steel, new Vector3(1.1f, 1.1f, -0.1f));
            // Cabin: rear window, door handle, roof work lights.
            A.Part("RearWindow", cab, A.Box(0.8f, 0.55f, 0.05f, 0.03f), A.Glass, new Vector3(-0.6f, 1.85f, -0.21f));
            A.Part("Handle", cab, A.Box(0.05f, 0.06f, 0.22f, 0.02f), A.DarkSteel, new Vector3(-1.19f, 1.35f, 0.2f));
            foreach (float x in new[] { -0.92f, -0.28f })
                A.Part("WorkLight", cab, A.Box(0.2f, 0.14f, 0.12f, 0.04f), A.HeadLight, new Vector3(x, 2.3f, 1.0f));
            // Engine cover with vents and an exhaust behind the cabin.
            A.Part("EngineCover", cab, A.Box(1.0f, 0.12f, 0.95f, 0.05f), A.DarkSteel, new Vector3(0.55f, 0.92f, -0.55f));
            for (int i = 0; i < 4; i++)
                A.Part("Vent", cab, A.Box(0.85f, 0.05f, 0.08f, 0.02f), A.Ink, new Vector3(0.55f, 0.99f, -0.88f + i * 0.22f));
            Pipe(cab, new Vector3(0.95f, 0.9f, -0.95f), 0.75f);
            // Counterweight (what the child sees from behind): hazard stripes and tail lights.
            HazardStripes(cab, new Vector3(0f, 0.56f, -1.74f), 0.9f, 0.18f, 6);
            RearLights(cab, new Vector3(0f, 0.56f, -1.74f), 0.8f, 0.8f);

            Transform boom = A.Pivot("Boom", cab, new Vector3(0.45f, 1.15f, 0.95f));
            c.P("BoomArm", boom, A.Box(0.42f, 0.5f, 3.3f, 0.18f), new Vector3(0f, 0f, 1.55f));
            A.Part("BoomRam", boom, A.Box(0.18f, 0.18f, 1.7f, 0.08f), A.Steel, new Vector3(0f, -0.36f, 0.95f));
            A.Part("BoomRamBody", boom, A.Box(0.26f, 0.26f, 0.9f, 0.1f), A.DarkSteel, new Vector3(0f, -0.36f, 0.45f));
            A.Part("StickRam", boom, A.Box(0.16f, 0.16f, 1.3f, 0.07f), A.Steel, new Vector3(0f, 0.38f, 2.35f));
            A.Part("StickRamBody", boom, A.Box(0.24f, 0.24f, 0.8f, 0.09f), A.DarkSteel, new Vector3(0f, 0.38f, 1.75f));
            Transform stick = A.Pivot("Stick", boom, new Vector3(0f, 0f, 3.1f));
            c.S("StickArm", stick, A.Box(0.34f, 0.42f, 2.3f, 0.14f), new Vector3(0f, 0f, 1.05f));
            A.Part("BucketRam", stick, A.Box(0.14f, 0.14f, 1.1f, 0.06f), A.Steel, new Vector3(0f, 0.3f, 1.25f));
            A.Part("Joint", stick, A.Wheel(0.26f, 0.5f, 0.06f), A.DarkSteel, Vector3.zero);
            Transform bucket = A.Pivot("Bucket", stick, new Vector3(0f, 0f, 2.15f));
            A.Part("Joint", bucket, A.Wheel(0.2f, 0.46f, 0.05f), A.DarkSteel, Vector3.zero);
            A.Part("Shell", bucket, A.Box(1.0f, 0.62f, 0.8f, 0.18f), A.DarkSteel, new Vector3(0f, -0.15f, 0.38f));
            for (int i = 0; i < 4; i++)
                A.Part("Tooth", bucket, A.Box(0.13f, 0.12f, 0.24f, 0.04f), A.Steel, new Vector3(-0.36f + i * 0.24f, -0.42f, 0.78f));
            GameObject soil = A.Part("Soil", bucket, A.Dome(0.42f, 0.38f), A.Soil, new Vector3(0f, 0.12f, 0.38f), null, new Vector3(1f, 1f, 0.85f));
            Transform tip = A.Pivot("Tip", bucket, new Vector3(0f, -0.4f, 0.8f));

            // Bake the folded rest pose (same as ExcavatorArm.Rest) so the prefab looks right everywhere.
            boom.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            stick.localRotation = Quaternion.Euler(100f, 0f, 0f);
            bucket.localRotation = Quaternion.Euler(60f, 0f, 0f);
            soil.SetActive(false);

            Colliders(c, new Vector3(0f, 1.35f, 0f), new Vector3(2.6f, 1.5f, 3.6f), 1.25f, 0.55f);
            GameObject root = Finish(c, profile, 5f, DebrisPusher.Mode.NudgeAside, 1.35f, 1.9f, 1.9f);
            var arm = root.AddComponent<ExcavatorArm>();
            E.Set(arm, "m_Vehicle", root.GetComponent<Vehicle>());
            E.Set(arm, "m_Cab", cab);
            E.Set(arm, "m_Boom", boom);
            E.Set(arm, "m_Stick", stick);
            E.Set(arm, "m_Bucket", bucket);
            E.Set(arm, "m_BucketSoil", soil);
            E.Set(arm, "m_BucketTip", tip);
            return Save(root, VehicleType.Excavator);
        }

        // ------------------------------------------------------------------ Dump truck
        private static GameObject DumpTruck(VehicleProfile profile)
        {
            Ctx c = Begin("DumpTruck", "#FF595E", "#1982C4");
            Transform b = c.Body;
            A.Part("Chassis", b, A.Box(1.5f, 0.5f, 5.0f, 0.15f), A.DarkSteel, new Vector3(0f, 0.95f, -0.2f));
            foreach (float x in new[] { -1.05f, 1.05f })
            {
                SpinWheel(c, b, new Vector3(x, 0.62f, 1.75f), 0.62f, 0.5f, true, A.Rubber);
                SpinWheel(c, b, new Vector3(x, 0.62f, -1.0f), 0.62f, 0.5f, false, A.Rubber);
                SpinWheel(c, b, new Vector3(x, 0.62f, -2.15f), 0.62f, 0.5f, false, A.Rubber);
                A.Part("Mudguard", b, A.Box(0.6f, 0.12f, 1.4f, 0.05f), A.DarkSteel, new Vector3(x, 1.32f, 1.75f));
            }
            c.P("Cab", b, A.Box(2.4f, 1.7f, 1.7f, 0.38f), new Vector3(0f, 2.05f, 1.85f));
            A.Part("Bumper", b, A.Box(2.4f, 0.42f, 0.32f, 0.14f), A.Steel, new Vector3(0f, 1.0f, 2.78f));
            A.Part("Grille", b, A.Box(1.3f, 0.45f, 0.06f, 0.05f), A.DarkSteel, new Vector3(0f, 1.5f, 2.72f));
            foreach (float x in new[] { -0.85f, 0.85f })
                A.Part("Headlight", b, A.Sphere(0.16f), A.White, new Vector3(x, 1.45f, 2.7f), null, new Vector3(1f, 1f, 0.5f));
            A.Part("Windscreen", b, A.Box(2.0f, 0.75f, 0.06f, 0.04f), A.Glass, new Vector3(0f, 2.4f, 2.71f));
            foreach (float x in new[] { -1.21f, 1.21f })
                A.Part("SideWindow", b, A.Box(0.06f, 0.6f, 0.9f, 0.03f), A.Glass, new Vector3(x, 2.4f, 1.95f));
            Eyes(b, new Vector3(0f, 2.42f, 2.75f), 0.45f, 0.2f);
            Cheeks(b, new Vector3(0f, 1.9f, 2.72f), 0.8f, 0.14f);
            Beacon(c, b, new Vector3(0f, 2.98f, 1.85f));
            foreach (float y in new[] { 1.38f, 1.5f, 1.62f })
                A.Part("GrilleSlat", b, A.Box(1.15f, 0.06f, 0.05f, 0.02f), A.Steel, new Vector3(0f, y, 2.76f));
            A.Part("Visor", b, A.Box(2.1f, 0.08f, 0.32f, 0.03f), A.DarkSteel, new Vector3(0f, 2.86f, 2.78f));
            foreach (float side in new[] { -1f, 1f })
            {
                A.Part("MirrorArm", b, A.Box(0.28f, 0.06f, 0.06f, 0.02f), A.DarkSteel, new Vector3(side * 1.32f, 2.3f, 2.45f));
                c.P("Mirror", b, A.Box(0.09f, 0.42f, 0.26f, 0.04f), new Vector3(side * 1.47f, 2.3f, 2.45f));
                A.Part("DoorLine", b, A.Box(0.03f, 1.2f, 0.04f, 0.01f), A.Ink, new Vector3(side * 1.215f, 1.95f, 1.15f));
                A.Part("DoorHandle", b, A.Box(0.05f, 0.06f, 0.2f, 0.02f), A.DarkSteel, new Vector3(side * 1.22f, 1.75f, 1.35f));
            }
            Pipe(b, new Vector3(1.36f, 1.25f, 0.9f), 2.0f, 0.09f);
            // Fuel tank and toolbox between the wheels, rear bumper with lights, plate and mud flaps.
            A.Part("FuelTank", b, A.Wheel(0.3f, 0.9f, 0.08f), A.Steel, new Vector3(-1.0f, 0.95f, 0.35f), new Vector3(0f, 90f, 0f));
            A.Part("ToolBox", b, A.Box(0.5f, 0.45f, 0.8f, 0.08f), A.DarkSteel, new Vector3(1.0f, 0.95f, 0.35f));
            A.Part("RearBumper", b, A.Box(2.3f, 0.24f, 0.2f, 0.08f), A.DarkSteel, new Vector3(0f, 0.95f, -2.78f));
            RearLights(b, new Vector3(0f, 0.97f, -2.9f), 0.85f);
            Plate(b, new Vector3(0f, 0.97f, -2.9f));
            foreach (float x in new[] { -1.05f, 1.05f })
                A.Part("MudFlap", b, A.Box(0.5f, 0.5f, 0.05f, 0.02f), A.Rubber, new Vector3(x, 0.55f, -2.84f));

            Transform bed = A.Pivot("BedPivot", b, new Vector3(0f, 1.25f, -2.55f));
            c.S("Floor", bed, A.Box(2.4f, 0.18f, 3.5f, 0.06f), new Vector3(0f, 0.09f, 1.75f));
            c.S("WallL", bed, A.Box(0.16f, 0.9f, 3.5f, 0.06f), new Vector3(-1.12f, 0.55f, 1.75f));
            c.S("WallR", bed, A.Box(0.16f, 0.9f, 3.5f, 0.06f), new Vector3(1.12f, 0.55f, 1.75f));
            c.S("WallFront", bed, A.Box(2.4f, 1.25f, 0.18f, 0.06f), new Vector3(0f, 0.72f, 3.42f));
            c.S("Tailgate", bed, A.Box(2.4f, 0.7f, 0.15f, 0.06f), new Vector3(0f, 0.45f, 0.08f));
            foreach (float x in new[] { -1.21f, 1.21f })
            {
                A.Part("Stripe", bed, A.Box(0.04f, 0.14f, 3.3f, 0.02f), A.White, new Vector3(x, 0.75f, 1.75f));
                foreach (float z in new[] { 0.55f, 1.75f, 2.95f })
                    c.S("Rib", bed, A.Box(0.07f, 0.85f, 0.12f, 0.03f), new Vector3(x * 1.02f, 0.5f, z));
            }
            // Tailgate: chevrons and hinge pins (seen all the time while driving).
            HazardStripes(bed, new Vector3(0f, 0.5f, -0.005f), 2.0f, 0.22f, 10);
            foreach (float x in new[] { -1.05f, 1.05f })
                A.Part("Hinge", bed, A.Wheel(0.08f, 0.22f, 0.03f), A.DarkSteel, new Vector3(x, 0.82f, 0.02f));

            GameObject soilLoad = A.Part("Load_Soil", bed, A.Dome(1.0f, 0.85f), A.Soil, new Vector3(0f, 0.18f, 1.75f), null, new Vector3(1.05f, 1f, 1.55f));
            GameObject gravelLoad = A.Part("Load_Gravel", bed, A.Dome(1.0f, 0.75f), A.Gravel, new Vector3(0f, 0.18f, 1.75f), null, new Vector3(1.05f, 1f, 1.55f));
            Transform bricks = A.Pivot("Load_Bricks", bed, new Vector3(0f, 0.18f, 1.75f));
            Material[] brickMats = { A.Brick, A.Colour("BrickYellow", "#FFD447", 0.2f), A.Colour("BrickBlue", "#4EA8DE", 0.2f) };
            int n = 0;
            for (int layer = 0; layer < 2; layer++)
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                if (layer == 1 && (x + z) % 2 != 0) continue;
                A.Part("Brick", bricks, A.Box(0.6f, 0.4f, 0.85f, 0.08f), brickMats[n++ % brickMats.Length], new Vector3(x * 0.68f, 0.22f + layer * 0.42f, z * 0.95f));
            }
            // Playground parts: planks with bright red / yellow / blue panels on top.
            Transform parts = A.Pivot("Load_PlayParts", bed, new Vector3(0f, 0.18f, 1.75f));
            for (int i = 0; i < 3; i++)
                A.Part("Plank", parts, A.Box(1.9f, 0.16f, 0.5f, 0.04f), A.Wood, new Vector3(0f, 0.12f, -1f + i));
            string[] panelCols = { "#FF595E", "#FFC93C", "#1982C4" };
            for (int i = 0; i < 3; i++)
                A.Part("Panel", parts, A.Box(1.4f, 0.16f, 1.1f, 0.05f), A.Colour("PlayPanel" + i, panelCols[i], 0.4f), new Vector3(0f, 0.32f + i * 0.17f, -0.9f + i * 0.9f), new Vector3(0f, (i - 1) * 12f, 0f));
            Transform dump = A.Pivot("DumpPoint", c.Root.transform, new Vector3(0f, 0.2f, -4.4f));

            Colliders(c, new Vector3(0f, 1.5f, -0.1f), new Vector3(2.5f, 1.6f, 5.6f), 2.0f, 0.6f);
            GameObject root = Finish(c, profile, 4.8f, DebrisPusher.Mode.NudgeAside, 1.3f, 2.85f, 2.85f);
            var tool = root.AddComponent<DumpTruckBed>();
            E.Set(tool, "m_Vehicle", root.GetComponent<Vehicle>());
            E.Set(tool, "m_BedPivot", bed);
            E.SetArray(tool, "m_LoadVisuals", new Object[] { soilLoad.transform, gravelLoad.transform, bricks, parts });
            E.Set(tool, "m_DumpPoint", dump);
            soilLoad.SetActive(false);
            gravelLoad.SetActive(false);
            bricks.gameObject.SetActive(false);
            parts.gameObject.SetActive(false);
            return Save(root, VehicleType.DumpTruck);
        }

        // ------------------------------------------------------------------ Bulldozer
        private static GameObject Bulldozer(VehicleProfile profile)
        {
            Ctx c = Begin("Bulldozer", "#FF9F1C", "#5B5F73");
            Transform b = c.Body;
            Track(c, -1.05f, 3.6f, 0.9f, 0.75f);
            Track(c, 1.05f, 3.6f, 0.9f, 0.75f);
            c.P("Body", b, A.Box(1.7f, 1.1f, 3.2f, 0.32f), new Vector3(0f, 1.35f, -0.1f));
            c.P("Hood", b, A.Box(1.55f, 0.75f, 1.3f, 0.28f), new Vector3(0f, 2.0f, 0.75f));
            c.P("Cab", b, A.Box(1.6f, 1.3f, 1.4f, 0.3f), new Vector3(0f, 2.55f, -0.75f));
            A.Part("Windscreen", b, A.Box(1.3f, 0.7f, 0.06f, 0.04f), A.Glass, new Vector3(0f, 2.7f, -0.03f));
            foreach (float x in new[] { -0.81f, 0.81f })
                A.Part("SideWindow", b, A.Box(0.06f, 0.65f, 0.95f, 0.03f), A.Glass, new Vector3(x, 2.7f, -0.75f));
            c.S("Roof", b, A.Box(1.85f, 0.16f, 1.65f, 0.06f), new Vector3(0f, 3.28f, -0.75f));
            Eyes(b, new Vector3(0f, 2.75f, 0.01f), 0.3f, 0.17f);
            Cheeks(b, new Vector3(0f, 2.5f, 0.0f), 0.55f, 0.12f);
            Pipe(b, new Vector3(0.55f, 2.3f, 0.95f), 0.9f, 0.1f);
            Slats(b, new Vector3(0f, 1.95f, 1.4f), 0.6f, 0.36f, 4, Vector3.forward);
            HeadLights(b, new Vector3(0f, 2.2f, 1.42f), 0.5f, 0.1f);
            A.Part("RearWindow", b, A.Box(1.2f, 0.6f, 0.05f, 0.03f), A.Glass, new Vector3(0f, 2.75f, -1.46f));
            foreach (float side in new[] { -1f, 1f })
            {
                Slats(b, new Vector3(side * 0.85f, 1.38f, -0.75f), 0.9f, 0.4f, 5, new Vector3(side, 0f, 0f), true);
            }
            // Back end: lights, a grille and a ripper claw (the driving view).
            RearLights(b, new Vector3(0f, 1.5f, -1.72f), 0.55f, 0.85f);
            Slats(b, new Vector3(0f, 1.22f, -1.7f), 0.9f, 0.2f, 3, Vector3.back);
            A.Part("RipperBar", b, A.Box(1.5f, 0.24f, 0.3f, 0.08f), A.DarkSteel, new Vector3(0f, 1.0f, -2.0f));
            foreach (float x in new[] { -0.6f, 0.6f })
                A.Part("RipperArm", b, A.Box(0.16f, 0.2f, 0.55f, 0.05f), A.DarkSteel, new Vector3(x, 1.05f, -1.8f));
            c.S("RipperShank", b, A.Box(0.3f, 0.95f, 0.34f, 0.09f), new Vector3(0f, 0.62f, -2.12f), new Vector3(-12f, 0f, 0f));
            A.Part("RipperTooth", b, A.Box(0.26f, 0.26f, 0.4f, 0.06f), A.Steel, new Vector3(0f, 0.24f, -2.22f), new Vector3(20f, 0f, 0f));
            Beacon(c, b, new Vector3(0.55f, 3.45f, -0.75f));

            c.S("Blade", b, A.Box(3.3f, 1.15f, 0.34f, 0.14f), new Vector3(0f, 0.75f, 2.5f), new Vector3(-8f, 0f, 0f));
            A.Part("BladeEdge", b, A.Box(3.35f, 0.14f, 0.38f, 0.05f), A.Steel, new Vector3(0f, 0.2f, 2.56f));
            foreach (float x in new[] { -1.62f, 1.62f })
                A.Part("BladeCorner", b, A.Box(0.12f, 1.1f, 0.42f, 0.04f), A.DarkSteel, new Vector3(x, 0.75f, 2.52f), new Vector3(-8f, 0f, 0f));

            foreach (float x in new[] { -1.1f, 1.1f })
            {
                A.Part("BladeArm", b, A.Box(0.2f, 0.26f, 1.5f, 0.06f), A.DarkSteel, new Vector3(x, 0.85f, 1.65f));
                A.Part("BladeRam", b, A.Box(0.14f, 0.14f, 1.2f, 0.05f), A.Steel, new Vector3(x * 0.5f, 1.45f, 1.85f), new Vector3(20f, 0f, 0f));
            }

            Colliders(c, new Vector3(0f, 1.35f, 0.3f), new Vector3(2.9f, 1.4f, 4.6f), 1.3f, 0.55f);
            GameObject root = Finish(c, profile, 5f, DebrisPusher.Mode.CarryWithBlade, 1.65f, 2.7f, 1.9f);
            return Save(root, VehicleType.Bulldozer);
        }

        // ------------------------------------------------------------------ Road roller
        private static GameObject RoadRoller(VehicleProfile profile)
        {
            Ctx c = Begin("RoadRoller", "#6BCB77", "#FFD447");
            Transform b = c.Body;
            SpinWheel(c, b, new Vector3(0f, 0.8f, 1.5f), 0.8f, 2.1f, true, A.Steel);
            Transform steer = c.SteerWheels[0];
            c.P("Yoke", steer, A.Box(2.45f, 0.32f, 0.6f, 0.12f), new Vector3(0f, 0.86f, 0f));
            foreach (float x in new[] { -1.16f, 1.16f })
                c.S("YokeSide", steer, A.Box(0.12f, 1.05f, 1.0f, 0.05f), new Vector3(x, 0.4f, 0f));
            Eyes(steer, new Vector3(0f, 0.92f, 0.31f), 0.42f, 0.17f);
            Cheeks(steer, new Vector3(0f, 0.8f, 0.3f), 0.75f, 0.11f);
            A.Part("Scraper", steer, A.Box(2.1f, 0.08f, 0.12f, 0.03f), A.DarkSteel, new Vector3(0f, 0.25f, -0.85f));
            foreach (float x in new[] { -0.95f, 0.95f })
                SpinWheel(c, b, new Vector3(x, 0.65f, -1.4f), 0.65f, 0.55f, false, A.Rubber);
            c.P("Body", b, A.Box(1.7f, 1.0f, 2.3f, 0.3f), new Vector3(0f, 1.4f, -0.9f));
            c.S("Neck", b, A.Box(0.8f, 0.6f, 1.1f, 0.2f), new Vector3(0f, 1.55f, 0.55f));
            foreach (float x in new[] { -0.78f, 0.78f })
            foreach (float z in new[] { -0.25f, -1.6f })
                A.Part("Post", b, A.Box(0.1f, 1.35f, 0.1f, 0.03f), A.DarkSteel, new Vector3(x, 2.55f, z));
            c.S("Canopy", b, A.Box(1.85f, 0.16f, 1.75f, 0.06f), new Vector3(0f, 3.28f, -0.92f));
            A.Part("Seat", b, A.Box(0.7f, 0.25f, 0.6f, 0.1f), A.Rubber, new Vector3(0f, 2.05f, -1.0f));
            A.Part("SeatBack", b, A.Box(0.7f, 0.55f, 0.14f, 0.08f), A.Rubber, new Vector3(0f, 2.4f, -1.32f), new Vector3(-8f, 0f, 0f));
            Pipe(b, new Vector3(0.45f, 1.85f, -1.85f), 1.0f);
            foreach (float x in new[] { -0.78f, 0.78f })
                A.Part("CanopyLight", b, A.Box(0.18f, 0.14f, 0.12f, 0.04f), A.HeadLight, new Vector3(x, 3.12f, -0.12f));
            RearLights(b, new Vector3(0f, 1.55f, -2.07f), 0.58f, 0.85f);
            Slats(b, new Vector3(0f, 1.28f, -2.05f), 0.9f, 0.22f, 3, Vector3.back);
            foreach (float side in new[] { -1f, 1f })
                Slats(b, new Vector3(side * 0.85f, 1.4f, -1.0f), 0.8f, 0.4f, 4, new Vector3(side, 0f, 0f), true);
            A.Part("Wheel", b, A.Wheel(0.22f, 0.06f, 0.02f), A.Ink, new Vector3(0f, 2.35f, -0.45f), new Vector3(0f, 90f, 60f));
            Beacon(c, b, new Vector3(0f, 3.45f, -0.92f));

            Colliders(c, new Vector3(0f, 1.4f, 0f), new Vector3(2.45f, 1.4f, 4.6f), 1.6f, 0.6f);
            GameObject root = Finish(c, profile, 5f, DebrisPusher.Mode.NudgeAside, 1.25f, 2.3f, 2.1f);
            return Save(root, VehicleType.RoadRoller);
        }
    }
}
