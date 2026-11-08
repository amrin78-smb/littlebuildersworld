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
                Save(TownHouse(wall, roof), n);
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
            Save(Resident("#FFB703", "#219EBC"), "Resident");
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
            Transform sway = A.Pivot("Sway", r.transform, Vector3.zero);
            A.Part("Trunk", sway, A.Disc(0.28f, 2.2f, 0.1f, 12), A.DarkWood, Vector3.zero);
            A.Part("Leaves", sway, A.Sphere(1.6f), A.Leaf, new Vector3(0f, 3.1f, 0f), null, new Vector3(1f, 0.95f, 1f));
            A.Part("Leaves2", sway, A.Sphere(1.05f), A.LeafLight, new Vector3(0.75f, 3.9f, 0.3f));
            A.Part("Leaves3", sway, A.Sphere(0.9f), A.LeafLight, new Vector3(-0.8f, 3.6f, -0.4f));
            var c = r.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0f, 1.2f, 0f);
            c.radius = 0.45f;
            c.height = 2.4f;
            return r;
        }

        private static GameObject TreePine()
        {
            GameObject r = Root("TreePine");
            Transform sway = A.Pivot("Sway", r.transform, Vector3.zero);
            A.Part("Trunk", sway, A.Disc(0.24f, 1.2f, 0.08f, 12), A.DarkWood, Vector3.zero);
            for (int i = 0; i < 3; i++)
                A.Part("Tier", sway, A.Cone(1.7f - i * 0.4f, 2.0f), i % 2 == 0 ? A.Leaf : A.LeafLight, new Vector3(0f, 1.0f + i * 1.1f, 0f));
            var c = r.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0f, 1.2f, 0f);
            c.radius = 0.45f;
            c.height = 2.4f;
            return r;
        }

        private static GameObject Bush()
        {
            GameObject r = Root("Bush");
            A.Part("A", r.transform, A.Sphere(0.8f), A.Leaf, new Vector3(0f, 0.5f, 0f), null, new Vector3(1.2f, 0.8f, 1f));
            A.Part("B", r.transform, A.Sphere(0.55f), A.LeafLight, new Vector3(0.6f, 0.45f, 0.2f));
            A.Part("Flower", r.transform, A.Sphere(0.14f), A.Colour("FlowerPink", "#FF8FC7"), new Vector3(-0.3f, 1.05f, 0.5f));
            A.Part("Flower2", r.transform, A.Sphere(0.12f), A.Colour("FlowerYellow", "#FFE066"), new Vector3(0.5f, 0.95f, 0.5f));
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
            for (int i = 0; i < 3; i++)
                A.Part("Post", r.transform, A.Box(0.22f, 1.2f, 0.22f, 0.08f), A.White, new Vector3(-2f + i * 2f, 0.6f, 0f));
            A.Part("Rail", r.transform, A.Box(4.2f, 0.18f, 0.1f, 0.05f), A.White, new Vector3(0f, 0.85f, 0f));
            A.Part("Rail2", r.transform, A.Box(4.2f, 0.18f, 0.1f, 0.05f), A.White, new Vector3(0f, 0.45f, 0f));
            BoxCol(r, new Vector3(0f, 0.6f, 0f), new Vector3(4.2f, 1.2f, 0.4f));
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
            A.Part("Pole", r.transform, A.Disc(0.1f, 3.6f, 0.04f, 10), A.DarkSteel, Vector3.zero);
            A.Part("Head", r.transform, A.Sphere(0.32f), A.Colour("LampGlow", "#FFF3B0", 0.8f), new Vector3(0f, 3.75f, 0f));
            return r;
        }

        private static GameObject SignPost()
        {
            GameObject r = Root("SignPost");
            A.Part("Post", r.transform, A.Box(0.18f, 2.6f, 0.18f, 0.06f), A.Wood, new Vector3(0f, 1.3f, 0f));
            A.Part("Board", r.transform, A.Box(2.0f, 1.2f, 0.14f, 0.12f), A.White, new Vector3(0f, 2.6f, 0f));
            // Icon quad is assigned per instance by the town builder (house / road / slide pictures).
            GameObject icon = A.Part("Icon", r.transform, A.UprightQuad(1.05f, 1.05f), A.White, new Vector3(-0.35f, 2.6f, -0.08f));
            GameObject icon2 = A.Part("IconBack", r.transform, A.UprightQuad(1.05f, 1.05f), A.White, new Vector3(-0.35f, 2.6f, 0.08f), new Vector3(0f, 180f, 0f));
            A.Part("Arrow", r.transform, A.Box(0.6f, 0.22f, 0.16f, 0.06f), A.Colour("SignArrow", "#1982C4"), new Vector3(0.55f, 2.6f, 0f));
            A.Part("ArrowHead", r.transform, A.Box(0.32f, 0.32f, 0.16f, 0.06f), A.Colour("SignArrow", "#1982C4"), new Vector3(0.85f, 2.6f, 0f), new Vector3(0f, 0f, 45f));
            A.NoShadows(icon);
            A.NoShadows(icon2);
            BoxCol(r, new Vector3(0f, 1.3f, 0f), new Vector3(0.3f, 2.6f, 0.3f));
            return r;
        }

        private static GameObject Barrier()
        {
            GameObject r = Root("Barrier");
            foreach (float x in new[] { -1.4f, 1.4f })
                A.Part("Leg", r.transform, A.Box(0.15f, 1.0f, 0.5f, 0.05f), A.White, new Vector3(x, 0.5f, 0f));
            for (int i = 0; i < 5; i++)
                A.Part("Stripe", r.transform, A.Box(0.6f, 0.4f, 0.12f, 0.04f), i % 2 == 0 ? A.ConeOrange : A.White, new Vector3(-1.2f + i * 0.6f, 0.9f, 0f));
            A.Part("Light", r.transform, A.Sphere(0.14f), A.Beacon, new Vector3(1.4f, 1.15f, 0f));
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
            A.Part("Walls", r.transform, A.Box(6f, 3.2f, 4.5f, 0.3f), A.Colour("ShedWall", "#FFE3B3", 0.2f), new Vector3(0f, 1.6f, 0f));
            A.Part("Roof", r.transform, A.Box(6.6f, 0.4f, 5.1f, 0.15f), A.Colour("ShedRoof", "#1982C4", 0.3f), new Vector3(0f, 3.4f, 0f));
            A.Part("Door", r.transform, A.Box(1.3f, 2.2f, 0.1f, 0.06f), A.Wood, new Vector3(-1.2f, 1.1f, -2.27f));
            A.Part("Window", r.transform, A.Box(1.4f, 1f, 0.08f, 0.06f), A.Glass, new Vector3(1.3f, 1.9f, -2.27f));
            A.Part("Sign", r.transform, A.Box(2.6f, 0.7f, 0.1f, 0.1f), A.Colour("SignYellow", "#FFC93C"), new Vector3(0f, 2.9f, -2.3f));
            BoxCol(r, new Vector3(0f, 1.7f, 0f), new Vector3(6f, 3.4f, 4.5f));
            return Building(r);
        }

        private static GameObject WaterTower()
        {
            GameObject r = Root("WaterTower");
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad;
                A.Part("Leg", r.transform, A.Box(0.3f, 7f, 0.3f, 0.08f), A.DarkSteel, new Vector3(Mathf.Cos(a) * 1.8f, 3.5f, Mathf.Sin(a) * 1.8f));
            }
            A.Part("Tank", r.transform, A.Disc(2.6f, 3.2f, 0.6f, 24), A.Colour("TowerTank", "#8AD8FF", 0.4f), new Vector3(0f, 7f, 0f));
            A.Part("Cap", r.transform, A.Dome(2.7f, 1.2f), A.Colour("TowerCap", "#FF595E", 0.3f), new Vector3(0f, 10.2f, 0f));
            A.Part("Band", r.transform, A.Disc(2.65f, 0.4f, 0.1f, 24), A.White, new Vector3(0f, 8.4f, 0f));
            BoxCol(r, new Vector3(0f, 3.5f, 0f), new Vector3(4f, 7f, 4f));
            return Building(r);
        }

        private static GameObject Windmill()
        {
            GameObject r = Root("Windmill");
            A.Part("Tower", r.transform, A.Cone(3.2f, 12f), A.Colour("MillWall", "#FFF1E6", 0.2f), Vector3.zero);
            A.Part("Cap", r.transform, A.Dome(1.6f, 1.6f), A.RoofRed, new Vector3(0f, 9.6f, 0f));
            A.Part("Door", r.transform, A.Box(1.2f, 2f, 0.2f, 0.1f), A.Wood, new Vector3(0f, 1f, -2.9f));
            Transform hub = A.Pivot("Blades", r.transform, new Vector3(0f, 9.8f, -1.7f));
            A.Part("Hub", hub, A.Sphere(0.45f), A.DarkWood, Vector3.zero);
            for (int i = 0; i < 4; i++)
                A.Part("Blade", hub, A.Box(0.9f, 5f, 0.12f, 0.06f), A.White, Quaternion.Euler(0f, 0f, i * 90f) * new Vector3(0f, 2.8f, 0f), new Vector3(0f, 0f, i * 90f));
            BoxCol(r, new Vector3(0f, 5f, 0f), new Vector3(4.5f, 10f, 4.5f));
            return Building(r);
        }

        private static GameObject TownHouse(string wall, string roof)
        {
            GameObject r = Root("TownHouse");
            A.Part("Walls", r.transform, A.Box(7f, 4f, 6f, 0.35f), A.Colour("HouseWall" + wall.Substring(1), wall, 0.2f), new Vector3(0f, 2f, 0f));
            Material roofMat = A.Colour("HouseRoof" + roof.Substring(1), roof, 0.3f);
            A.Part("RoofL", r.transform, A.Box(7.6f, 0.4f, 4.4f, 0.15f), roofMat, new Vector3(0f, 5.1f, -1.4f), new Vector3(-35f, 0f, 0f));
            A.Part("RoofR", r.transform, A.Box(7.6f, 0.4f, 4.4f, 0.15f), roofMat, new Vector3(0f, 5.1f, 1.4f), new Vector3(35f, 0f, 0f));
            A.Part("Door", r.transform, A.Box(1.3f, 2.2f, 0.12f, 0.08f), A.Wood, new Vector3(0f, 1.1f, -3.02f));
            foreach (float x in new[] { -2.3f, 2.3f })
                A.Part("Window", r.transform, A.Box(1.2f, 1.1f, 0.1f, 0.08f), A.Glass, new Vector3(x, 2.4f, -3.02f));
            A.Part("Chimney", r.transform, A.Box(0.7f, 1.6f, 0.7f, 0.1f), A.Brick, new Vector3(2.2f, 5.8f, 1.2f));
            BoxCol(r, new Vector3(0f, 2.5f, 0f), new Vector3(7f, 5f, 6f));
            return Building(r);
        }

        private static GameObject Fountain()
        {
            GameObject r = Root("Fountain");
            A.Part("Basin", r.transform, A.Disc(2.2f, 0.7f, 0.25f, 28), A.Concrete, Vector3.zero);
            A.Part("Water", r.transform, A.Disc(1.9f, 0.1f, 0.05f, 28), A.Colour("Water", "#5FC9F8", 0.8f), new Vector3(0f, 0.62f, 0f));
            A.Part("Column", r.transform, A.Disc(0.3f, 1.6f, 0.1f, 14), A.Concrete, Vector3.zero);
            A.Part("Top", r.transform, A.Dome(0.9f, 0.5f), A.Colour("Water", "#5FC9F8", 0.8f), new Vector3(0f, 1.6f, 0f));
            BoxCol(r, new Vector3(0f, 0.6f, 0f), new Vector3(4.4f, 1.2f, 4.4f));
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
            float radius = 0.8f;
            switch (kind)
            {
                case "Rock":
                    A.Part("Rock", r.transform, A.Sphere(0.75f), A.Rock, new Vector3(0f, 0.45f, 0f), new Vector3(10f, 30f, 5f), new Vector3(1.2f, 0.8f, 1f));
                    A.Part("Pebble", r.transform, A.Sphere(0.35f), A.Rock, new Vector3(0.6f, 0.25f, 0.3f));
                    break;
                case "Log":
                    A.Part("Log", r.transform, A.Wheel(0.4f, 2.0f, 0.1f), A.DarkWood, new Vector3(0f, 0.4f, 0f));
                    A.Part("End", r.transform, A.Wheel(0.32f, 2.04f, 0.04f), A.Wood, new Vector3(0f, 0.4f, 0f));
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

        private static GameObject Resident(string body, string shirt)
        {
            GameObject r = Root("Resident");
            A.Part("Legs", r.transform, A.Box(0.5f, 0.6f, 0.32f, 0.12f), A.Colour("ResidentTrousers", "#3D5A80"), new Vector3(0f, 0.3f, 0f));
            A.Part("Body", r.transform, A.Box(0.7f, 0.75f, 0.42f, 0.2f), A.Colour("ResidentShirt", shirt), new Vector3(0f, 0.95f, 0f));
            A.Part("Head", r.transform, A.Sphere(0.36f), A.Colour("ResidentSkin", "#F1C27D"), new Vector3(0f, 1.62f, 0f));
            A.Part("Hat", r.transform, A.Dome(0.4f, 0.28f), A.Colour("HardHat", body, 0.5f), new Vector3(0f, 1.78f, 0f));
            A.Part("Smile", r.transform, A.Box(0.18f, 0.05f, 0.04f, 0.02f), A.Ink, new Vector3(0f, 1.52f, 0.34f));
            foreach (float x in new[] { -0.12f, 0.12f })
                A.Part("Eye", r.transform, A.Sphere(0.05f), A.Ink, new Vector3(x, 1.66f, 0.32f));
            Transform arm = A.Pivot("WaveArm", r.transform, new Vector3(0.42f, 1.25f, 0f));
            A.Part("Arm", arm, A.Box(0.18f, 0.6f, 0.18f, 0.08f), A.Colour("ResidentShirt", shirt), new Vector3(0f, 0.28f, 0f));
            A.Part("Hand", arm, A.Sphere(0.12f), A.Colour("ResidentSkin", "#F1C27D"), new Vector3(0f, 0.62f, 0f));
            A.Part("ArmL", r.transform, A.Box(0.18f, 0.6f, 0.18f, 0.08f), A.Colour("ResidentShirt", shirt), new Vector3(-0.45f, 0.95f, 0f));
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
