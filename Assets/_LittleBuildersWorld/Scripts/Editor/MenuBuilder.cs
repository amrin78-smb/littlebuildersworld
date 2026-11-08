using LittleBuildersWorld.Core;
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
    /// <summary>Boot scene (services, then menu) and the Main Menu diorama: the four machines in a sunny yard.</summary>
    public static class MenuBuilder
    {
        public const string BootPath = E.Root + "/Scenes/Boot.unity";
        public const string MenuPath = E.Root + "/Scenes/MainMenu.unity";

        public static void BuildBoot()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = E.Hex("#FFEFD2");
            new GameObject("Bootstrap").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), BootPath);
        }

        public static void BuildMenu()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = E.Hex("#FFF2DC");
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Hard;
            sun.shadowStrength = 0.5f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = E.Hex("#C9E8FF");
            RenderSettings.ambientEquatorColor = E.Hex("#E9E2D0");
            RenderSettings.ambientGroundColor = E.Hex("#9DB98A");
            RenderSettings.fog = false;

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = E.Hex("#A8DEFF");
            cam.fieldOfView = 45f;
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            var orbit = camGo.AddComponent<MenuCameraOrbit>();
            E.Set(orbit, "m_Centre", new Vector3(0f, 0f, 2f));
            E.Set(orbit, "m_Radius", 19f);
            E.Set(orbit, "m_Height", 6.5f);

            Transform env = new GameObject("Diorama").transform;
            A.Part("Grass", env, A.Disc(45f, 0.02f, 0.01f, 48), A.Grass, new Vector3(0f, -0.02f, 0f));
            A.Part("Yard", env, A.Disc(13f, 0.03f, 0.01f, 40), A.Sand, Vector3.zero);
            (VehicleType t, Vector3 p, float yaw)[] line =
            {
                (VehicleType.Excavator, new Vector3(-6.5f, 0f, 3f), 160f), (VehicleType.DumpTruck, new Vector3(-2f, 0f, -1f), 175f),
                (VehicleType.Bulldozer, new Vector3(3f, 0f, -0.5f), 190f), (VehicleType.RoadRoller, new Vector3(7.5f, 0f, 3.5f), 205f)
            };
            foreach (var v in line)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(VehicleBuilder.Load(v.t), env);
                go.transform.SetPositionAndRotation(v.p, Quaternion.Euler(0f, v.yaw, 0f));
            }
            Place("SoilMound", env, new Vector3(-2f, 0f, 8f), 0f, 1.2f);
            Place("Hopper", env, new Vector3(8f, 0f, 11f), 20f, 1f);
            foreach (Vector3 p in new[] { new Vector3(-10f, 0, -3f), new Vector3(10.5f, 0, -2f), new Vector3(0f, 0, -6f), new Vector3(-4f, 0, -5f) })
                Place("DebrisCone", env, p, 0f, 1f).GetComponent<PushableDebris>().enabled = false;
            foreach (Vector3 p in new[] { new Vector3(-12f, 0, 6f), new Vector3(12f, 0, 7f) }) Place("FlagPole", env, p, 0f, 1f);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                Place(i % 2 == 0 ? "TreeRound" : "TreePine", env, new Vector3(Mathf.Cos(a) * 24f, 0f, Mathf.Sin(a) * 24f), i * 40f, 1.2f + (i % 3) * 0.2f);
            }
            Place("Cloud", env, new Vector3(-12f, 22f, 30f), 0f, 1.5f);
            Place("Cloud", env, new Vector3(15f, 26f, 28f), 40f, 1.3f);
            Place("Windmill", env, new Vector3(-20f, 0f, 22f), 30f, 1f);
            Place("Bunting", env, new Vector3(0f, 0f, 10f), 0f, 1f);

            UIBuilder.BuildMainMenu();
            UIBuilder.EventSystem();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), MenuPath);
        }

        private static GameObject Place(string prefab, Transform parent, Vector3 p, float yaw, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(PropBuilder.Get(prefab), parent);
            go.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }
    }
}
