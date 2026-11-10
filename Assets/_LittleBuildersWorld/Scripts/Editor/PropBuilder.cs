using LittleBuildersWorld.World;
using UnityEditor;
using UnityEngine;
using A = LittleBuildersWorld.EditorTools.ArtLibrary;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>Environment prefabs: rounded, toy-like props from shared palette materials, with simple colliders.</summary>
    public static class PropBuilder
    {
        public const string Folder = E.Root + "/Prefabs/Environment";
        public const string ActivityFolder = E.Root + "/Prefabs/Activities";
        public const string FxFolder = E.Root + "/Prefabs/VFX";

        public static GameObject Get(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{name}.prefab") ??
            AssetDatabase.LoadAssetAtPath<GameObject>($"{ActivityFolder}/{name}.prefab") ??
            AssetDatabase.LoadAssetAtPath<GameObject>($"{FxFolder}/{name}.prefab");

        public static void BuildAll()
        {
            E.EnsureFolder(Folder);
            E.EnsureFolder(ActivityFolder);
            E.EnsureFolder(FxFolder);
            Save(TreeRound(), "TreeRound");
            Save(TreePine(), "TreePine");
            Save(Bush(), "Bush");
            Save(Fence(), "Fence");
            Save(FlagPole(), "FlagPole");
            Save(Lamp(), "Lamp");
            Save(Cloud(), "Cloud");
            Save(Shed(), "Shed");
            Save(WaterTower(), "WaterTower");
            Save(Windmill(), "Windmill");
            foreach (var (n, wall, roof) in new[] { ("TownHouseA", "#FFE3B3", "#E63946"), ("TownHouseB", "#BDE0FE", "#457B9D"), ("TownHouseC", "#FFC8DD", "#8E7DBE"), ("TownHouseD", "#CAFFBF", "#F4A261") })
                Save(TownHouse(n, wall, roof), n);
            Save(Hopper(), "Hopper");
            Save(BrickPallet(), "BrickPallet");
            Save(Skip(), "Skip");
            Save(Barrier(), "Barrier");
            Save(SignPost(), "SignPost");
            Save(SoilMound(1.0f), "SoilMound");
            Save(SoilMound(0.45f), "SoilPile");
            Save(Debris("Rock"), "DebrisRock", ActivityFolder);
            Save(Debris("Log"), "DebrisLog", ActivityFolder);
            Save(Debris("Crate"), "DebrisCrate", ActivityFolder);
            Save(Debris("Cone"), "DebrisCone", ActivityFolder);
            Save(Resident(), "Resident");
            Save(Pond(), "Pond");
            Save(Person("Foreman", "#FFC93C", "#FF9F1C", 1f, true), "Foreman");
            Save(Person("KidA", "#3A2E2A", "#FF595E", 0.72f, false), "KidA");
            Save(Person("KidB", "#8A5A3A", "#4EA8DE", 0.68f, false), "KidB");
            Save(Person("KidC", "#F4D35E", "#8AC926", 0.7f, false), "KidC");
            Save(Sandpit(), "Sandpit");
            Save(Slide(), "Slide");
            Save(Swings(), "Swings");
            Save(ClimbingFrame(), "ClimbingFrame");
            Save(Roundabout(), "Roundabout");
            Save(Bench(), "Bench");
            Save(Rainbow(), "Rainbow");
            Save(PartsPallet(), "PartsPallet");
            Save(Bunting(), "Bunting");
            Save(BalloonArch(), "BalloonArch");
            Save(Fountain(), "Fountain");
            Save(Particles("Sparkle", true, 0.6f, 0.45f, 3f, 0.2f, 0.9f, new[] { "#FFFFFF", "#FFE066" }, 0f), "FX_Sparkle", FxFolder);
            Save(Particles("Confetti", false, 1.6f, 0.28f, 9f, 0.5f, 2.4f, new[] { "#FF595E", "#FFCA3A", "#8AC926", "#1982C4", "#6A4C93", "#FF8FC7" }, 0.7f), "FX_Confetti", FxFolder);
            Save(Particles("Poof", false, 0.9f, 1.2f, 2.5f, 0.6f, 1.6f, new[] { "#FFFFFF", "#F1F1F1" }, -0.1f), "FX_Poof", FxFolder);
            Save(Smoke(), "FX_Smoke", FxFolder);
        }

        private static GameObject Save(GameObject go, string name, string folder = Folder) => E.SavePrefab(go, $"{folder}/{name}.prefab");

        private static GameObject Root(string name) => new GameObject(name);

        /// <summary>Smooth textured Blender model (Tools/blender/build_env.sh) if built; false = use the procedural parts.</summary>
        private static bool Model(GameObject r, string prop, string group)
        {
            if (!BlenderModels.HasProp(prop, group)) return false;
            BlenderModels.BuildProp(prop, group, r.transform);
            return true;
        }

        private static void BoxCol(GameObject go, Vector3 centre, Vector3 size)
        {
            var c = go.AddComponent<BoxCollider>();
            c.center = centre;
            c.size = size;
        }

        // ------------------------------------------------------------------ nature
        private static GameObject TreeRound()
        {
            GameObject r = Root("TreeRound");
            if (!Model(r, "TreeRound", "Nature"))
            {
                Transform sway = A.Pivot("Sway", r.transform, Vector3.zero);
                A.Part("Trunk", sway, A.Disc(0.28f, 2.2f, 0.1f, 12), A.DarkWood, Vector3.zero);
                A.Part("Leaves", sway, A.Sphere(1.6f), A.Leaf, new Vector3(0f, 3.1f, 0f), null, new Vector3(1f, 0.95f, 1f));
                A.Part("Leaves2", sway, A.Sphere(1.05f), A.LeafLight, new Vector3(0.75f, 3.9f, 0.3f));
                A.Part("Leaves3", sway, A.Sphere(0.9f), A.LeafLight, new Vector3(-0.8f, 3.6f, -0.4f));
                A.Part("Leaves4", sway, A.Sphere(0.85f), A.Leaf, new Vector3(0.1f, 3.3f, 1.0f));
                A.Part("Apple", sway, A.Sphere(0.14f), A.Colour("Apple", "#FF595E", 0.5f), new Vector3(0.9f, 2.9f, 1.05f));
                A.Part("Apple", sway, A.Sphere(0.14f), A.Colour("Apple", "#FF595E", 0.5f), new Vector3(-1.1f, 3.2f, 0.7f));
            }
            var c = r.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0f, 1.2f, 0f);
            c.radius = 0.45f;
            c.height = 2.4f;
            return r;
        }

        private static GameObject TreePine()
        {
            GameObject r = Root("TreePine");
            if (!Model(r, "TreePine", "Nature"))
            {
                Transform sway = A.Pivot("Sway", r.transform, Vector3.zero);
                A.Part("Trunk", sway, A.Disc(0.24f, 1.2f, 0.08f, 12), A.DarkWood, Vector3.zero);
                for (int i = 0; i < 3; i++)
                    A.Part("Tier", sway, A.Cone(1.7f - i * 0.4f, 2.0f), i % 2 == 0 ? A.Leaf : A.LeafLight, new Vector3(0f, 1.0f + i * 1.1f, 0f));
            }
            var c = r.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0f, 1.2f, 0f);
            c.radius = 0.45f;
            c.height = 2.4f;
            return r;
        }

        private static GameObject Bush()
        {
            GameObject r = Root("Bush");
            if (!Model(r, "Bush", "Nature"))
            {
                A.Part("A", r.transform, A.Sphere(0.8f), A.Leaf, new Vector3(0f, 0.5f, 0f), null, new Vector3(1.2f, 0.8f, 1f));
                A.Part("B", r.transform, A.Sphere(0.55f), A.LeafLight, new Vector3(0.6f, 0.45f, 0.2f));
                A.Part("Flower", r.transform, A.Sphere(0.14f), A.Colour("FlowerPink", "#FF8FC7"), new Vector3(-0.3f, 1.05f, 0.5f));
                A.Part("Flower2", r.transform, A.Sphere(0.12f), A.Colour("FlowerYellow", "#FFE066"), new Vector3(0.5f, 0.95f, 0.5f));
            }
            return r;
        }

        private static GameObject Cloud()
        {
            GameObject r = Root("Cloud");
            A.Part("A", r.transform, A.Sphere(3f), A.Cloud, Vector3.zero, null, new Vector3(1.4f, 0.8f, 1f));
            A.Part("B", r.transform, A.Sphere(2.2f), A.Cloud, new Vector3(3f, -0.4f, 0.5f));
            A.Part("C", r.transform, A.Sphere(2.4f), A.Cloud, new Vector3(-3.2f, -0.3f, -0.4f));
            A.NoShadows(r);
            return r;
        }

        // ------------------------------------------------------------------ street furniture
        private static GameObject Fence()
        {
            GameObject r = Root("Fence");
            if (!Model(r, "Fence", "Street"))
            {
                for (int i = 0; i < 3; i++)
                    A.Part("Post", r.transform, A.Box(0.22f, 1.2f, 0.22f, 0.08f), A.White, new Vector3(-2f + i * 2f, 0.6f, 0f));
                A.Part("Rail", r.transform, A.Box(4.2f, 0.18f, 0.1f, 0.05f), A.White, new Vector3(0f, 0.85f, 0f));
                A.Part("Rail2", r.transform, A.Box(4.2f, 0.18f, 0.1f, 0.05f), A.White, new Vector3(0f, 0.45f, 0f));
            }
            // Taller than it looks: an invisible wall vehicles slide along and can never ride up onto.
            BoxCol(r, new Vector3(0f, 1.5f, 0f), new Vector3(4.2f, 3f, 0.5f));
            return r;
        }

        private static GameObject FlagPole()
        {
            GameObject r = Root("FlagPole");
            A.Part("Pole", r.transform, A.Disc(0.07f, 4.5f, 0.03f, 10), A.White, Vector3.zero);
            A.Part("Ball", r.transform, A.Sphere(0.14f), A.Colour("Gold", "#FFC93C", 0.6f), new Vector3(0f, 4.55f, 0f));
            Transform wave = A.Pivot("Flag", r.transform, new Vector3(0f, 4.3f, 0f));
            A.Part("Cloth", wave, A.Box(1.3f, 0.8f, 0.05f, 0.02f), A.Colour("FlagRed", "#FF595E"), new Vector3(0.7f, -0.4f, 0f));
            return r;
        }

        private static GameObject Lamp()
        {
            GameObject r = Root("Lamp");
            if (!Model(r, "Lamp", "Street"))
            {
                A.Part("Base", r.transform, A.Disc(0.22f, 0.45f, 0.06f, 12), A.DarkSteel, Vector3.zero);
                A.Part("Pole", r.transform, A.Disc(0.1f, 3.6f, 0.04f, 10), A.DarkSteel, Vector3.zero);
                A.Part("Collar", r.transform, A.Disc(0.17f, 0.12f, 0.04f, 12), A.DarkSteel, new Vector3(0f, 3.45f, 0f));
                A.Part("Head", r.transform, A.Sphere(0.32f), A.Lit("LampGlow", "#FFF3B0", 0.8f, null, null, 0.5f), new Vector3(0f, 3.75f, 0f));
                A.Part("Cap", r.transform, A.Dome(0.3f, 0.18f), A.DarkSteel, new Vector3(0f, 3.98f, 0f));
            }
            return r;
        }

        private static GameObject SignPost()
        {
            GameObject r = Root("SignPost");
            if (!Model(r, "SignPost", "Street"))
            {
                A.Part("Post", r.transform, A.Box(0.18f, 2.6f, 0.18f, 0.06f), A.Wood, new Vector3(0f, 1.3f, 0f));
                A.Part("Board", r.transform, A.Box(2.0f, 1.2f, 0.14f, 0.12f), A.White, new Vector3(0f, 2.6f, 0f));
                A.Part("Arrow", r.transform, A.Box(0.6f, 0.22f, 0.16f, 0.06f), A.Colour("SignArrow", "#1982C4"), new Vector3(0.55f, 2.6f, 0f));
                A.Part("ArrowHead", r.transform, A.Box(0.32f, 0.32f, 0.16f, 0.06f), A.Colour("SignArrow", "#1982C4"), new Vector3(0.85f, 2.6f, 0f), new Vector3(0f, 0f, 45f));
            }
            // Icon quad is assigned per instance by the town builder (house / road / slide pictures).
            GameObject icon = A.Part("Icon", r.transform, A.UprightQuad(1.05f, 1.05f), A.White, new Vector3(-0.35f, 2.6f, -0.08f));
            GameObject icon2 = A.Part("IconBack", r.transform, A.UprightQuad(1.05f, 1.05f), A.White, new Vector3(-0.35f, 2.6f, 0.08f), new Vector3(0f, 180f, 0f));
            A.NoShadows(icon);
            A.NoShadows(icon2);
            BoxCol(r, new Vector3(0f, 1.3f, 0f), new Vector3(0.3f, 2.6f, 0.3f));
            return r;
        }

        private static GameObject Barrier()
        {
            GameObject r = Root("Barrier");
            if (!Model(r, "Barrier", "Street"))
            {
                foreach (float x in new[] { -1.4f, 1.4f })
                    A.Part("Leg", r.transform, A.Box(0.15f, 1.0f, 0.5f, 0.05f), A.White, new Vector3(x, 0.5f, 0f));
                for (int i = 0; i < 5; i++)
                    A.Part("Stripe", r.transform, A.Box(0.6f, 0.4f, 0.12f, 0.04f), i % 2 == 0 ? A.ConeOrange : A.White, new Vector3(-1.2f + i * 0.6f, 0.9f, 0f));
                A.Part("Light", r.transform, A.Sphere(0.14f), A.Beacon, new Vector3(1.4f, 1.15f, 0f));
            }
            return r;
        }

        // ------------------------------------------------------------------ buildings (Building layer: the camera avoids them)
        private static GameObject Building(GameObject r)
        {
            A.SetLayer(r, ProjectConfigurator.BuildingLayer);
            return r;
        }

        private static GameObject Shed()
        {
            GameObject r = Root("Shed");
            if (!Model(r, "Shed", "Buildings"))
            {
                A.Part("Walls", r.transform, A.Box(6f, 3.2f, 4.5f, 0.3f), A.Colour("ShedWall", "#FFE3B3", 0.2f), new Vector3(0f, 1.6f, 0f));
                A.Part("Roof", r.transform, A.Box(6.6f, 0.4f, 5.1f, 0.15f), A.Colour("ShedRoof", "#1982C4", 0.3f), new Vector3(0f, 3.4f, 0f));
                A.Part("Door", r.transform, A.Box(1.3f, 2.2f, 0.1f, 0.06f), A.Wood, new Vector3(-1.2f, 1.1f, -2.27f));
                A.Part("Window", r.transform, A.Box(1.4f, 1f, 0.08f, 0.06f), A.Glass, new Vector3(1.3f, 1.9f, -2.27f));
                A.Part("Sign", r.transform, A.Box(2.6f, 0.7f, 0.1f, 0.1f), A.Colour("SignYellow", "#FFC93C"), new Vector3(0f, 2.9f, -2.3f));
                A.Part("SignHat", r.transform, A.Dome(0.42f, 0.3f), A.Colour("HardHat_FF9F1C", "#FF9F1C", 0.5f), new Vector3(0f, 2.72f, -2.4f), new Vector3(-90f, 0f, 0f), new Vector3(1f, 0.35f, 1f));
                A.Part("DoorFrame", r.transform, A.Box(1.5f, 2.35f, 0.08f, 0.05f), A.White, new Vector3(-1.2f, 1.15f, -2.25f));
                A.Part("Knob", r.transform, A.Sphere(0.08f), A.Colour("Gold", "#FFC93C", 0.6f), new Vector3(-0.75f, 1.1f, -2.35f));
                A.Part("WindowFrame", r.transform, A.Box(1.6f, 1.2f, 0.06f, 0.05f), A.White, new Vector3(1.3f, 1.9f, -2.25f));
                foreach (float x in new[] { -3.0f, 3.0f })
                    A.Part("Gutter", r.transform, A.Box(0.12f, 3.2f, 0.12f, 0.04f), A.Colour("ShedRoof", "#1982C4", 0.3f), new Vector3(x, 1.6f, -2.2f));
            }
            BoxCol(r, new Vector3(0f, 1.7f, 0f), new Vector3(6f, 3.4f, 4.5f));
            return Building(r);
        }

        private static GameObject WaterTower()
        {
            GameObject r = Root("WaterTower");
            if (!Model(r, "WaterTower", "Buildings"))
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * 90f * Mathf.Deg2Rad;
                    A.Part("Leg", r.transform, A.Box(0.3f, 7f, 0.3f, 0.08f), A.DarkSteel, new Vector3(Mathf.Cos(a) * 1.8f, 3.5f, Mathf.Sin(a) * 1.8f));
                }
                for (int i = 0; i < 4; i++)
                {
                    float a0 = i * 90f * Mathf.Deg2Rad, a1 = (i + 1) * 90f * Mathf.Deg2Rad;
                    Vector3 p0 = new Vector3(Mathf.Cos(a0) * 1.8f, 0f, Mathf.Sin(a0) * 1.8f), p1 = new Vector3(Mathf.Cos(a1) * 1.8f, 0f, Mathf.Sin(a1) * 1.8f);
                    Vector3 d = p1 - p0;
                    float yaw = -Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg;
                    foreach (float y in new[] { 2.4f, 4.8f })
                        A.Part("Brace", r.transform, A.Box(d.magnitude, 0.14f, 0.14f, 0.04f), A.Steel, (p0 + p1) * 0.5f + Vector3.up * y, new Vector3(0f, yaw, 0f));
                }
                A.Part("Tank", r.transform, A.Disc(2.6f, 3.2f, 0.6f, 24), A.Colour("TowerTank", "#8AD8FF", 0.4f), new Vector3(0f, 7f, 0f));
                A.Part("Drop", r.transform, A.Sphere(0.55f), A.White, new Vector3(0f, 8.6f, -2.45f), null, new Vector3(0.8f, 1f, 0.3f));
                A.Part("Cap", r.transform, A.Dome(2.7f, 1.2f), A.Colour("TowerCap", "#FF595E", 0.3f), new Vector3(0f, 10.2f, 0f));
                A.Part("Band", r.transform, A.Disc(2.65f, 0.4f, 0.1f, 24), A.White, new Vector3(0f, 8.4f, 0f));
            }
            BoxCol(r, new Vector3(0f, 3.5f, 0f), new Vector3(4f, 7f, 4f));
            return Building(r);
        }

        private static GameObject Windmill()
        {
            GameObject r = Root("Windmill");
            if (!Model(r, "Windmill", "Buildings"))
            {
                A.Part("Tower", r.transform, A.Cone(3.2f, 12f), A.Colour("MillWall", "#FFF1E6", 0.2f), Vector3.zero);
                A.Part("Cap", r.transform, A.Dome(1.6f, 1.6f), A.RoofRed, new Vector3(0f, 9.6f, 0f));
                A.Part("Door", r.transform, A.Box(1.2f, 2f, 0.2f, 0.1f), A.Wood, new Vector3(0f, 1f, -2.9f));
                Transform hub = A.Pivot("Blades", r.transform, new Vector3(0f, 9.8f, -1.7f));
                A.Part("Hub", hub, A.Sphere(0.45f), A.DarkWood, Vector3.zero);
                Material sail = A.Colour("MillSail", "#FF595E", 0.3f);
                for (int i = 0; i < 4; i++)
                {
                    Quaternion q = Quaternion.Euler(0f, 0f, i * 90f);
                    A.Part("Spar", hub, A.Box(0.22f, 5.4f, 0.16f, 0.06f), A.DarkWood, q * new Vector3(0f, 2.9f, 0f), new Vector3(0f, 0f, i * 90f));
                    A.Part("Sail", hub, A.Box(1.0f, 3.8f, 0.08f, 0.05f), i % 2 == 0 ? sail : A.White, q * new Vector3(0.62f, 3.4f, 0.06f), new Vector3(0f, 0f, i * 90f));
                }
                foreach (float y in new[] { 4.2f, 7.0f })
                {
                    float rad = 3.2f * (1f - y / 12f);
                    A.Part("Window", r.transform, A.Box(0.7f, 0.9f, 0.2f, 0.12f), A.Glass, new Vector3(0f, y, -rad + 0.02f), new Vector3(-15f, 0f, 0f));
                }
                A.Part("Balcony", r.transform, A.Disc(2.1f, 0.15f, 0.05f, 24), A.DarkWood, new Vector3(0f, 8.2f, 0f));
            }
            BoxCol(r, new Vector3(0f, 5f, 0f), new Vector3(4.5f, 10f, 4.5f));
            return Building(r);
        }

        private static GameObject TownHouse(string prop, string wall, string roof)
        {
            GameObject r = Root("TownHouse");
            if (!Model(r, prop, "Buildings"))
            {
                A.Part("Walls", r.transform, A.Box(7f, 4f, 6f, 0.35f), A.Colour("HouseWall" + wall.Substring(1), wall, 0.2f), new Vector3(0f, 2f, 0f));
                Material roofMat = A.Colour("HouseRoof" + roof.Substring(1), roof, 0.3f);
                A.Part("RoofL", r.transform, A.Box(7.6f, 0.4f, 4.4f, 0.15f), roofMat, new Vector3(0f, 5.1f, -1.4f), new Vector3(-35f, 0f, 0f));
                A.Part("RoofR", r.transform, A.Box(7.6f, 0.4f, 4.4f, 0.15f), roofMat, new Vector3(0f, 5.1f, 1.4f), new Vector3(35f, 0f, 0f));
                A.Part("Door", r.transform, A.Box(1.3f, 2.2f, 0.12f, 0.08f), A.Wood, new Vector3(0f, 1.1f, -3.02f));
                A.Part("DoorFrame", r.transform, A.Box(1.55f, 2.4f, 0.08f, 0.06f), A.White, new Vector3(0f, 1.2f, -2.99f));
                A.Part("Knob", r.transform, A.Sphere(0.08f), A.Colour("Gold", "#FFC93C", 0.6f), new Vector3(0.4f, 1.1f, -3.1f));
                A.Part("Step", r.transform, A.Box(1.9f, 0.2f, 0.7f, 0.06f), A.Concrete, new Vector3(0f, 0.1f, -3.3f));
                foreach (float x in new[] { -2.3f, 2.3f })
                    Window(r.transform, new Vector3(x, 2.4f, -3.0f), 0f, true);
                foreach (float side in new[] { -1f, 1f })
                    Window(r.transform, new Vector3(side * 3.5f, 2.4f, 0f), -side * 90f, false);
                A.Part("Chimney", r.transform, A.Box(0.7f, 1.6f, 0.7f, 0.1f), A.Brick, new Vector3(2.2f, 5.8f, 1.2f));
            }
            BoxCol(r, new Vector3(0f, 2.5f, 0f), new Vector3(7f, 5f, 6f));
            return Building(r);
        }

        /// <summary>A white-framed window with a cross bar (facing -Z before <paramref name="yaw"/>), optional flower box.</summary>
        private static void Window(Transform parent, Vector3 pos, float yaw, bool flowers)
        {
            Transform w = A.Pivot("Window", parent, pos, new Vector3(0f, yaw, 0f));
            A.Part("Frame", w, A.Box(1.4f, 1.3f, 0.08f, 0.05f), A.White, Vector3.zero);
            A.Part("Glass", w, A.Box(1.18f, 1.08f, 0.1f, 0.04f), A.Glass, new Vector3(0f, 0f, -0.02f));
            A.Part("BarV", w, A.Box(0.08f, 1.1f, 0.12f, 0.02f), A.White, new Vector3(0f, 0f, -0.04f));
            A.Part("BarH", w, A.Box(1.2f, 0.08f, 0.12f, 0.02f), A.White, new Vector3(0f, 0f, -0.04f));
            if (!flowers) return;
            A.Part("FlowerBox", w, A.Box(1.4f, 0.26f, 0.32f, 0.06f), A.Wood, new Vector3(0f, -0.75f, -0.16f));
            string[] cols = { "#FF8FC7", "#FFE066", "#FF595E" };
            for (int i = 0; i < 3; i++)
                A.Part("Flower", w, A.Sphere(0.13f), A.Colour("Flower" + i, cols[i], 0.2f), new Vector3(-0.42f + i * 0.42f, -0.55f, -0.16f));
        }

        /// <summary>A small duck pond (decoration). "Duck0"/"Duck1" bob on the water (AmbientMotion).</summary>
        private static GameObject Pond()
        {
            GameObject r = Root("Pond");
            A.Part("Bed", r.transform, A.Disc(5.2f, 0.06f, 0.02f, 32), A.Colour("PondBed", "#4FA3C7", 0.5f), new Vector3(0f, 0.01f, 0f), null, new Vector3(1f, 1f, 0.75f));
            A.Part("Water", r.transform, A.Disc(4.9f, 0.08f, 0.02f, 32), A.Colour("Water", "#5FC9F8", 0.8f), new Vector3(0f, 0.03f, 0f), null, new Vector3(1f, 1f, 0.75f));
            var rng = new System.Random(11);
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Cos(a) * 5.1f, 0.1f, Mathf.Sin(a) * 5.1f * 0.75f);
                float s = 0.35f + (float)rng.NextDouble() * 0.2f;
                A.Part("Stone", r.transform, A.Sphere(0.6f), A.Rock, p, new Vector3(0f, i * 40f, 0f), new Vector3(s * 1.4f, s * 0.7f, s));
            }
            Material pad = A.Colour("LilyPad", "#4CB050", 0.2f);
            foreach (Vector3 p in new[] { new Vector3(2.2f, 0.09f, 1.1f), new Vector3(-2.6f, 0.09f, -0.8f), new Vector3(0.8f, 0.09f, -2.0f) })
            {
                A.Part("LilyPad", r.transform, A.Disc(0.55f, 0.04f, 0.01f, 12), pad, p);
                A.Part("Lily", r.transform, A.Sphere(0.12f), A.Colour("FlowerPink", "#FF8FC7"), p + new Vector3(0.15f, 0.08f, 0.1f));
            }
            Material yellow = A.Colour("Duck", "#FFD447", 0.3f);
            Material beak = A.Colour("Beak", "#FF9F1C", 0.3f);
            for (int i = 0; i < 2; i++)
            {
                Transform d = A.Pivot("Duck" + i, r.transform, new Vector3(-1f + i * 2.2f, 0.12f, 0.6f - i * 1.4f), new Vector3(0f, 40f + i * 150f, 0f));
                float s = i == 0 ? 1f : 0.7f;
                A.Part("Body", d, A.Sphere(0.4f * s), yellow, new Vector3(0f, 0.2f * s, 0f), null, new Vector3(1f, 0.75f, 1.3f));
                A.Part("Tail", d, A.Sphere(0.15f * s), yellow, new Vector3(0f, 0.38f * s, -0.48f * s));
                A.Part("Head", d, A.Sphere(0.24f * s), yellow, new Vector3(0f, 0.58f * s, 0.32f * s));
                A.Part("Beak", d, A.Box(0.2f * s, 0.08f * s, 0.2f * s, 0.03f), beak, new Vector3(0f, 0.54f * s, 0.56f * s));
                foreach (float x in new[] { -0.11f, 0.11f })
                    A.Part("Eye", d, A.Sphere(0.04f * s), A.Ink, new Vector3(x * s, 0.64f * s, 0.5f * s));
            }
            var col = r.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 4.6f;
            col.height = 2f;
            return r;
        }

        private static GameObject Fountain()
        {
            GameObject r = Root("Fountain");
            A.Part("Basin", r.transform, A.Disc(2.2f, 0.7f, 0.25f, 28), A.Concrete, Vector3.zero);
            A.Part("Water", r.transform, A.Disc(1.9f, 0.1f, 0.05f, 28), A.Colour("Water", "#5FC9F8", 0.8f), new Vector3(0f, 0.62f, 0f));
            A.Part("Column", r.transform, A.Disc(0.3f, 1.6f, 0.1f, 14), A.Concrete, Vector3.zero);
            A.Part("Top", r.transform, A.Dome(0.9f, 0.5f), A.Colour("Water", "#5FC9F8", 0.8f), new Vector3(0f, 1.6f, 0f));
            BoxCol(r, new Vector3(0f, 1.5f, 0f), new Vector3(4.4f, 3f, 4.4f));
            return r;
        }

        // ------------------------------------------------------------------ construction props
        private static GameObject Hopper()
        {
            GameObject r = Root("Hopper");
            foreach (float x in new[] { -2.2f, 2.2f })
            foreach (float z in new[] { -1.4f, 1.4f })
                A.Part("Leg", r.transform, A.Box(0.3f, 5.4f, 0.3f, 0.08f), A.DarkSteel, new Vector3(x, 2.7f, z));
            A.Part("Bin", r.transform, A.Box(4.8f, 2.2f, 3.4f, 0.3f), A.Colour("HopperYellow", "#FFC93C", 0.4f), new Vector3(0f, 6.2f, 0f));
            A.Part("Gravel", r.transform, A.Dome(1.9f, 0.9f), A.Gravel, new Vector3(0f, 7.25f, 0f), null, new Vector3(1.15f, 1f, 0.8f));
            A.Part("Chute", r.transform, A.Box(1.2f, 1.0f, 1.2f, 0.2f), A.DarkSteel, new Vector3(0f, 4.7f, 0f));
            A.Part("Stripe", r.transform, A.Box(4.85f, 0.3f, 3.45f, 0.08f), A.Ink, new Vector3(0f, 5.3f, 0f));
            // Legs only: trucks drive in under the bin.
            foreach (float x in new[] { -2.2f, 2.2f })
            foreach (float z in new[] { -1.4f, 1.4f })
                BoxCol(r, new Vector3(x, 2.7f, z), new Vector3(0.35f, 5.4f, 0.35f));
            return r;
        }

        private static GameObject BrickPallet()
        {
            GameObject r = Root("BrickPallet");
            A.Part("Pallet", r.transform, A.Box(1.6f, 0.2f, 1.6f, 0.04f), A.Wood, new Vector3(0f, 0.1f, 0f));
            Material[] mats = { A.Brick, A.Colour("BrickYellow", "#FFD447", 0.2f), A.Colour("BrickBlue", "#4EA8DE", 0.2f) };
            int n = 0;
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
            for (int z = 0; z < 2; z++)
                A.Part("Brick", r.transform, A.Box(0.7f, 0.42f, 0.7f, 0.08f), mats[n++ % 3], new Vector3(-0.38f + x * 0.76f, 0.42f + y * 0.44f, -0.38f + z * 0.76f));
            return r;
        }

        private static GameObject Skip()
        {
            GameObject r = Root("Skip");
            Material green = A.Colour("SkipGreen", "#2A9D8F", 0.3f);
            A.Part("Floor", r.transform, A.Box(4.6f, 0.25f, 3.6f, 0.08f), green, new Vector3(0f, 0.12f, 0f));
            A.Part("Back", r.transform, A.Box(4.6f, 1.2f, 0.25f, 0.08f), green, new Vector3(0f, 0.6f, 1.7f));
            foreach (float x in new[] { -2.2f, 2.2f })
                A.Part("Side", r.transform, A.Box(0.25f, 1.0f, 3.6f, 0.08f), green, new Vector3(x, 0.5f, 0f));
            A.Part("Stripe", r.transform, A.Box(4.65f, 0.2f, 0.27f, 0.05f), A.Colour("SignYellow", "#FFC93C"), new Vector3(0f, 1.0f, 1.7f));
            // Open at the front so the bulldozer can push straight in.
            return r;
        }

        private static GameObject SoilMound(float scale)
        {
            GameObject r = Root("SoilMound");
            A.Part("Mound", r.transform, A.Dome(3f * scale, 1.8f * scale), A.Soil, Vector3.zero);
            A.Part("Bump", r.transform, A.Dome(1.6f * scale, 1.2f * scale), A.SoilLight, new Vector3(1.2f * scale, 0.6f * scale, 0.6f * scale));
            A.Part("Bump2", r.transform, A.Dome(1.3f * scale, 1.0f * scale), A.SoilLight, new Vector3(-1.3f * scale, 0.4f * scale, -0.5f * scale));
            return r;
        }

        private static GameObject Debris(string kind)
        {
            GameObject r = Root("Debris" + kind);
            float radius = kind == "Log" ? 1f : kind == "Crate" ? 0.75f : kind == "Cone" ? 0.55f : 0.8f;
            if (!Model(r, "Debris" + kind, kind == "Rock" || kind == "Log" ? "Nature" : "Street"))
            switch (kind)
            {
                case "Rock":
                    A.Part("Rock", r.transform, A.Sphere(0.75f), A.Rock, new Vector3(0f, 0.45f, 0f), new Vector3(10f, 30f, 5f), new Vector3(1.2f, 0.8f, 1f));
                    A.Part("Pebble", r.transform, A.Sphere(0.35f), A.Rock, new Vector3(0.6f, 0.25f, 0.3f));
                    break;
                case "Log":
                    A.Part("Log", r.transform, A.Wheel(0.4f, 2.0f, 0.1f), A.DarkWood, new Vector3(0f, 0.4f, 0f));
                    A.Part("End", r.transform, A.Wheel(0.32f, 2.04f, 0.04f), A.Wood, new Vector3(0f, 0.4f, 0f));
                    A.Part("Ring", r.transform, A.Wheel(0.17f, 2.06f, 0.03f), A.DarkWood, new Vector3(0f, 0.4f, 0f));
                    A.Part("Leaf", r.transform, A.Sphere(0.18f), A.LeafLight, new Vector3(0.3f, 0.82f, 0.05f), null, new Vector3(1.2f, 0.5f, 0.8f));
                    radius = 1f;
                    break;
                case "Crate":
                    A.Part("Crate", r.transform, A.Box(1.1f, 1.0f, 1.1f, 0.1f), A.Wood, new Vector3(0f, 0.5f, 0f));
                    A.Part("Band", r.transform, A.Box(1.14f, 0.15f, 1.14f, 0.04f), A.DarkWood, new Vector3(0f, 0.5f, 0f));
                    radius = 0.75f;
                    break;
                default:
                    A.Part("Cone", r.transform, A.Cone(0.42f, 1.0f), A.ConeOrange, Vector3.zero);
                    A.Part("Stripe", r.transform, A.Disc(0.3f, 0.16f, 0.02f, 16), A.White, new Vector3(0f, 0.45f, 0f));
                    A.Part("Base", r.transform, A.Box(0.9f, 0.1f, 0.9f, 0.04f), A.ConeOrange, new Vector3(0f, 0.05f, 0f));
                    radius = 0.55f;
                    break;
            }
            var d = r.AddComponent<PushableDebris>();
            E.Set(d, "m_Radius", radius);
            return r;
        }

        private static GameObject Resident() => Person("Resident", "#6B4226", "#219EBC", 1f, false, "#2A9D8F");

        /// <summary>
        /// A friendly figure: the foreman (hard hat, hi-vis stripes) or a child (hair). Big head, smile, rosy cheeks.
        /// Materials are named per colour, so every figure keeps its own colours. "WaveArm" is animated by AmbientMotion.
        /// </summary>
        private static GameObject Person(string name, string hatOrHair, string shirt, float scale, bool hardHat, string trousers = "#3D5A80")
        {
            GameObject r = Root(name);
            Transform b = A.Pivot("Body", r.transform, Vector3.zero);
            b.localScale = Vector3.one * scale;
            Material shirtMat = A.Colour("Shirt_" + shirt.TrimStart('#'), shirt);
            Material legMat = A.Colour("Trousers_" + trousers.TrimStart('#'), trousers);
            Material skin = A.Skin;
            Material shoe = A.Colour("Shoe", "#5A4636", 0.3f);
            foreach (float x in new[] { -0.14f, 0.14f })
            {
                A.Part("Leg", b, A.Box(0.24f, 0.5f, 0.28f, 0.11f), legMat, new Vector3(x, 0.36f, 0f));
                A.Part("Shoe", b, A.Box(0.26f, 0.15f, 0.36f, 0.07f), shoe, new Vector3(x, 0.08f, 0.04f));
            }
            A.Part("Torso", b, A.Box(0.68f, 0.72f, 0.46f, 0.25f), shirtMat, new Vector3(0f, 0.95f, 0f));
            if (hardHat)
                foreach (float y in new[] { 0.86f, 1.06f })
                    A.Part("Stripe", b, A.Box(0.7f, 0.07f, 0.48f, 0.03f), A.Colour("HiVisStripe", "#FFF2B0"), new Vector3(0f, y, 0f));
            A.Part("Head", b, A.Sphere(0.4f), skin, new Vector3(0f, 1.68f, 0f));
            foreach (float x in new[] { -1f, 1f })
            {
                A.Part("Ear", b, A.Sphere(0.09f), skin, new Vector3(x * 0.39f, 1.66f, 0f), null, new Vector3(0.6f, 1f, 1f));
                A.Part("Eye", b, A.Sphere(0.075f), A.Ink, new Vector3(x * 0.14f, 1.73f, 0.35f), null, new Vector3(1f, 1.25f, 0.6f));
                A.Part("Shine", b, A.Sphere(0.026f), A.White, new Vector3(x * 0.14f + 0.025f, 1.76f, 0.4f));
                A.Part("Cheek", b, A.Sphere(0.075f), A.Cheek, new Vector3(x * 0.24f, 1.58f, 0.32f), null, new Vector3(1f, 0.6f, 0.4f));
            }
            A.Part("Nose", b, A.Sphere(0.06f), A.Colour("Nose", "#E8A87C", 0.3f), new Vector3(0f, 1.63f, 0.39f));
            // Open smile: an upside-down dome pressed into the face.
            A.Part("Smile", b, A.Dome(0.12f, 0.08f), A.Colour("Mouth", "#B5423C", 0.3f), new Vector3(0f, 1.54f, 0.35f), new Vector3(200f, 0f, 0f), new Vector3(1f, 1f, 0.45f));
            Material top = A.Colour((hardHat ? "HardHat_" : "Hair_") + hatOrHair.TrimStart('#'), hatOrHair, hardHat ? 0.5f : 0.15f);
            if (hardHat)
            {
                A.Part("Hat", b, A.Dome(0.44f, 0.32f), top, new Vector3(0f, 1.82f, 0f));
                A.Part("Brim", b, A.Disc(0.52f, 0.05f, 0.02f, 20), top, new Vector3(0f, 1.82f, 0.05f));
                A.Part("HatBadge", b, A.Sphere(0.07f), A.White, new Vector3(0f, 1.98f, 0.38f), null, new Vector3(1f, 1f, 0.4f));
            }
            else
            {
                A.Part("Hair", b, A.Dome(0.43f, 0.3f), top, new Vector3(0f, 1.75f, -0.03f));
                A.Part("Fringe", b, A.Sphere(0.16f), top, new Vector3(0.12f, 1.98f, 0.25f), null, new Vector3(1.4f, 0.7f, 0.8f));
                A.Part("Tuft", b, A.Sphere(0.12f), top, new Vector3(-0.05f, 2.1f, -0.05f));
            }
            A.Part("ArmL", b, A.Box(0.19f, 0.6f, 0.21f, 0.09f), shirtMat, new Vector3(-0.44f, 0.98f, 0f), new Vector3(0f, 0f, -10f));
            A.Part("HandL", b, A.Sphere(0.11f), skin, new Vector3(-0.5f, 0.64f, 0f));
            Transform arm = A.Pivot("WaveArm", r.transform, new Vector3(0.42f, 1.25f, 0f) * scale);
            arm.localScale = Vector3.one * scale;
            A.Part("Arm", arm, A.Box(0.19f, 0.6f, 0.21f, 0.09f), shirtMat, new Vector3(0f, 0.28f, 0f));
            A.Part("Hand", arm, A.Sphere(0.11f), skin, new Vector3(0f, 0.62f, 0f));
            return r;
        }

        // ------------------------------------------------------------------ playground (no colliders, like the house parts)
        private static GameObject Sandpit()
        {
            GameObject r = Root("Sandpit");
            foreach (var (pos, size) in new[] { (new Vector3(0f, 0.2f, 2f), new Vector3(5.2f, 0.4f, 0.3f)), (new Vector3(0f, 0.2f, -2f), new Vector3(5.2f, 0.4f, 0.3f)),
                         (new Vector3(2.5f, 0.2f, 0f), new Vector3(0.3f, 0.4f, 4.3f)), (new Vector3(-2.5f, 0.2f, 0f), new Vector3(0.3f, 0.4f, 4.3f)) })
                A.Part("Board", r.transform, A.Box(size.x, size.y, size.z, 0.06f), A.Wood, pos);
            A.Part("Sand", r.transform, A.Box(4.8f, 0.24f, 3.8f, 0.1f), A.Colour("PlaySand", "#F6D98B", 0.05f), new Vector3(0f, 0.13f, 0f));
            A.Part("Castle", r.transform, A.Box(0.6f, 0.5f, 0.6f, 0.08f), A.Colour("PlaySandDark", "#E9C46A", 0.05f), new Vector3(-1f, 0.45f, 0.4f));
            Transform bucket = A.Pivot("Bucket", r.transform, new Vector3(1.1f, 0.25f, -0.6f));
            A.Part("Pail", bucket, A.Disc(0.28f, 0.42f, 0.04f, 16), A.Colour("ToyRed", "#FF595E", 0.4f), Vector3.zero);
            A.Part("Spade", r.transform, A.Box(0.12f, 0.06f, 0.7f, 0.03f), A.Colour("ToyBlue", "#1982C4", 0.4f), new Vector3(0.4f, 0.3f, -1f), new Vector3(0f, 30f, 0f));
            return r;
        }

        private static GameObject Slide()
        {
            GameObject r = Root("Slide");
            Material post = A.Colour("PlayBlue", "#1982C4", 0.4f);
            Material yellow = A.Colour("PlayYellow", "#FFC93C", 0.4f);
            foreach (float x in new[] { -0.7f, 0.7f })
            foreach (float z in new[] { -0.7f, 0.7f })
                A.Part("Post", r.transform, A.Box(0.16f, 2.6f, 0.16f, 0.05f), post, new Vector3(x, 1.3f, z));
            A.Part("Deck", r.transform, A.Box(1.6f, 0.16f, 1.6f, 0.05f), A.Wood, new Vector3(0f, 2f, 0f));
            A.Part("Roof", r.transform, A.Cone(1.25f, 0.9f), A.Colour("PlayRed", "#FF595E", 0.4f), new Vector3(0f, 2.6f, 0f));
            // Chute down to the east (+x).
            Transform chute = A.Pivot("Chute", r.transform, new Vector3(2.55f, 1.05f, 0f), new Vector3(0f, 0f, -27f));
            A.Part("Bed", chute, A.Box(4.1f, 0.12f, 0.9f, 0.05f), A.Colour("SlideRed", "#FF595E", 0.6f), Vector3.zero);
            A.Part("SideL", chute, A.Box(4.1f, 0.3f, 0.1f, 0.04f), yellow, new Vector3(0f, 0.15f, 0.45f));
            A.Part("SideR", chute, A.Box(4.1f, 0.3f, 0.1f, 0.04f), yellow, new Vector3(0f, 0.15f, -0.45f));
            // Ladder up the west side.
            Transform ladder = A.Pivot("Ladder", r.transform, new Vector3(-1.25f, 1f, 0f), new Vector3(0f, 0f, -18f));
            foreach (float z in new[] { -0.4f, 0.4f })
                A.Part("Rail", ladder, A.Box(0.1f, 2.2f, 0.1f, 0.03f), post, new Vector3(0f, 0f, z));
            for (int i = 0; i < 4; i++)
                A.Part("Rung", ladder, A.Box(0.08f, 0.08f, 0.8f, 0.02f), yellow, new Vector3(0f, -0.8f + i * 0.5f, 0f));
            return r;
        }

        private static GameObject Swings()
        {
            GameObject r = Root("Swings");
            Material frame = A.Colour("PlayGreen", "#6BCB77", 0.4f);
            A.Part("TopBar", r.transform, A.Box(5.2f, 0.16f, 0.16f, 0.06f), frame, new Vector3(0f, 2.7f, 0f));
            foreach (float x in new[] { -2.4f, 2.4f })
            foreach (float side in new[] { -1f, 1f })
                A.Part("Leg", r.transform, A.Box(0.16f, 2.85f, 0.16f, 0.05f), frame, new Vector3(x, 1.35f, side * 0.45f), new Vector3(side * 19f, 0f, 0f));
            string[] seats = { "#FF595E", "#FFC93C" };
            for (int i = 0; i < 2; i++)
            {
                Transform pivot = A.Pivot("SwingPivot" + i, r.transform, new Vector3(-1.1f + i * 2.2f, 2.62f, 0f));
                foreach (float x in new[] { -0.35f, 0.35f })
                    A.Part("Chain", pivot, A.Box(0.04f, 1.9f, 0.04f, 0.01f), A.Steel, new Vector3(x, -0.95f, 0f));
                A.Part("Seat", pivot, A.Box(0.85f, 0.1f, 0.4f, 0.04f), A.Colour("SwingSeat" + i, seats[i], 0.4f), new Vector3(0f, -1.92f, 0f));
            }
            return r;
        }

        private static GameObject ClimbingFrame()
        {
            GameObject r = Root("ClimbingFrame");
            string[] cols = { "#FF595E", "#FFC93C", "#1982C4" };
            foreach (float x in new[] { -1.2f, 1.2f })
            foreach (float z in new[] { -1.2f, 1.2f })
                A.Part("Post", r.transform, A.Box(0.16f, 2.4f, 0.16f, 0.05f), A.Colour("PlayPurple", "#8E7DBE", 0.4f), new Vector3(x, 1.2f, z));
            for (int level = 0; level < 3; level++)
            {
                Material m = A.Colour("Bar" + level, cols[level], 0.4f);
                float y = 0.6f + level * 0.75f;
                foreach (float s in new[] { -1.2f, 1.2f })
                {
                    A.Part("BarX", r.transform, A.Box(2.4f, 0.1f, 0.1f, 0.03f), m, new Vector3(0f, y, s));
                    A.Part("BarZ", r.transform, A.Box(0.1f, 0.1f, 2.4f, 0.03f), m, new Vector3(s, y, 0f));
                }
            }
            A.Part("Deck", r.transform, A.Box(2.4f, 0.14f, 2.4f, 0.05f), A.Wood, new Vector3(0f, 2.4f, 0f));
            A.Part("FlagPole", r.transform, A.Box(0.06f, 1.2f, 0.06f, 0.02f), A.White, new Vector3(1.1f, 3f, 1.1f));
            Transform flag = A.Pivot("Flag", r.transform, new Vector3(1.1f, 3.45f, 1.1f));
            A.Part("Cloth", flag, A.Pennant(0.7f, 0.5f), A.Colour("PlayRed", "#FF595E", 0.4f), new Vector3(0.35f, 0f, 0f), new Vector3(0f, 0f, 90f));
            return r;
        }

        private static GameObject Roundabout()
        {
            GameObject r = Root("Roundabout");
            A.Part("Base", r.transform, A.Disc(0.4f, 0.3f, 0.05f, 16), A.DarkSteel, Vector3.zero);
            Transform spin = A.Pivot("Spin", r.transform, new Vector3(0f, 0.3f, 0f));
            A.Part("Plate", spin, A.Disc(1.6f, 0.18f, 0.05f, 28), A.Colour("PlayYellow", "#FFC93C", 0.4f), Vector3.zero);
            A.Part("Centre", spin, A.Disc(0.5f, 0.22f, 0.05f, 16), A.Colour("PlayRed", "#FF595E", 0.4f), new Vector3(0f, 0.05f, 0f));
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 1.1f, 0.6f, Mathf.Sin(a) * 1.1f);
                A.Part("Handle", spin, A.Box(0.08f, 0.8f, 0.08f, 0.03f), A.Colour("PlayBlue", "#1982C4", 0.4f), p);
                A.Part("Knob", spin, A.Sphere(0.1f), A.Colour("PlayRed", "#FF595E", 0.4f), p + Vector3.up * 0.42f);
            }
            return r;
        }

        private static GameObject Bench()
        {
            GameObject r = Root("Bench");
            A.Part("Seat", r.transform, A.Box(2f, 0.1f, 0.5f, 0.03f), A.Wood, new Vector3(0f, 0.5f, 0f));
            A.Part("Back", r.transform, A.Box(2f, 0.45f, 0.08f, 0.03f), A.Wood, new Vector3(0f, 0.85f, -0.25f));
            foreach (float x in new[] { -0.85f, 0.85f })
                A.Part("Leg", r.transform, A.Box(0.1f, 0.5f, 0.45f, 0.03f), A.DarkSteel, new Vector3(x, 0.25f, 0f));
            return r;
        }

        /// <summary>A rainbow arch with cloud feet (yard decoration unlock). One combined mesh per colour band.</summary>
        private static GameObject Rainbow()
        {
            GameObject r = Root("Rainbow");
            string[] cols = { "#FF595E", "#FF9F1C", "#FFC93C", "#6BCB77", "#1982C4", "#8E7DBE" };
            Mesh seg = A.Box(0.42f, 0.4f, 0.4f, 0.05f);
            const int n = 22;
            for (int b = 0; b < cols.Length; b++)
            {
                float radius = 7.2f - b * 0.38f;
                var parts = new (Mesh, Matrix4x4)[n];
                for (int i = 0; i < n; i++)
                {
                    float a = (i + 0.5f) / n * Mathf.PI;
                    var pos = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                    float arc = Mathf.PI * radius / n;
                    parts[i] = (seg, Matrix4x4.TRS(pos, Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f), new Vector3(arc / 0.42f * 1.08f, 0.95f, 1f)));
                }
                Mesh band = A.SaveMesh("RainbowBand" + b, LittleBuildersWorld.Core.MeshFactory.Combine("RainbowBand" + b, parts));
                A.Part("Band" + b, r.transform, band, A.Colour("Rainbow" + b, cols[b], 0.35f), Vector3.zero);
            }
            foreach (float x in new[] { -6.3f, 6.3f })
            {
                Transform puff = A.Pivot("Cloud", r.transform, new Vector3(x, 0.4f, 0f));
                A.Part("Puff", puff, A.Sphere(1.1f), A.Cloud, Vector3.zero);
                A.Part("Puff", puff, A.Sphere(0.8f), A.Cloud, new Vector3(0.9f, -0.15f, 0.2f));
                A.Part("Puff", puff, A.Sphere(0.8f), A.Cloud, new Vector3(-0.9f, -0.15f, -0.2f));
            }
            return r;
        }

        /// <summary>Planks and bright playground panels on a pallet (the play-parts depot).</summary>
        private static GameObject PartsPallet()
        {
            GameObject r = Root("PartsPallet");
            A.Part("Pallet", r.transform, A.Box(1.6f, 0.2f, 1.6f, 0.04f), A.Wood, new Vector3(0f, 0.1f, 0f));
            for (int i = 0; i < 3; i++)
                A.Part("Plank", r.transform, A.Box(1.5f, 0.14f, 0.4f, 0.03f), A.Wood, new Vector3(0f, 0.28f + i * 0.15f, -0.45f + i * 0.45f));
            string[] cols = { "#FF595E", "#FFC93C", "#1982C4" };
            for (int i = 0; i < 3; i++)
                A.Part("Panel", r.transform, A.Box(1.3f, 0.14f, 1.3f, 0.05f), A.Colour("PlayPanel" + i, cols[i], 0.4f), new Vector3(0f, 0.75f + i * 0.16f, 0f), new Vector3(0f, i * 25f, 0f));
            return r;
        }

        private static GameObject Bunting()
        {
            GameObject r = Root("Bunting");
            foreach (float x in new[] { -6f, 6f })
                A.Part("Pole", r.transform, A.Disc(0.08f, 4.2f, 0.03f, 10), A.White, new Vector3(x, 0f, 0f));
            A.Part("Line", r.transform, A.Box(12f, 0.05f, 0.05f, 0.02f), A.Ink, new Vector3(0f, 4.0f, 0f));
            string[] cols = { "#FF595E", "#FFCA3A", "#8AC926", "#1982C4", "#6A4C93" };
            for (int i = 0; i < 11; i++)
                A.Part("Pennant", r.transform, A.Pennant(0.8f, 0.9f), A.Colour("Pennant" + i % 5, cols[i % 5]), new Vector3(-5f + i, 3.98f - Mathf.Sin(i / 10f * Mathf.PI) * 0.4f, 0f));
            return r;
        }

        private static GameObject BalloonArch()
        {
            GameObject r = Root("BalloonArch");
            string[] cols = { "#FF595E", "#FFCA3A", "#1982C4", "#8AC926", "#FF8FC7" };
            for (int i = 0; i <= 16; i++)
            {
                float a = i / 16f * Mathf.PI;
                Vector3 p = new Vector3(Mathf.Cos(a) * 5f, Mathf.Sin(a) * 5f + 0.5f, 0f);
                A.Part("Balloon", r.transform, A.Sphere(0.6f), A.Colour("Balloon" + i % 5, cols[i % 5], 0.7f), p);
            }
            return r;
        }

        // ------------------------------------------------------------------ particles
        private static GameObject Particles(string name, bool additive, float lifetime, float size, float speed, float sizeMin, float sizeMax, string[] colours, float gravity)
        {
            var go = new GameObject("FX_" + name, typeof(ParticleSystem));
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * sizeMin, size * sizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var grad = new Gradient();
            var keys = new GradientColorKey[Mathf.Min(8, colours.Length)];
            for (int i = 0; i < keys.Length; i++) keys[i] = new GradientColorKey(E.Hex(colours[i]), keys.Length == 1 ? 0f : i / (float)(keys.Length - 1));
            grad.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            grad.mode = GradientMode.Fixed;
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = fade;
            var sol = ps.sizeOverLifetime;
            sol.enabled = name != "Confetti";
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.2f));
            var rot = ps.rotationOverLifetime;
            rot.enabled = name == "Confetti";
            rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = A.Particle(name, IconPainter.Sprite(name == "Confetti" ? "fx_confetti" : "fx_spark").texture, additive);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static GameObject Smoke()
        {
            GameObject go = Particles("Smoke", false, 3f, 0.9f, 0.8f, 0.6f, 1.2f, new[] { "#FFFFFF", "#EEEEEE" }, -0.05f);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 2.5f;
            var shape = ps.shape;
            shape.radius = 0.15f;
            return go;
        }
    }
}
