using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Player, Android, graphics, quality, layer and TextMeshPro configuration. Re-runnable; every build calls
    /// <see cref="ConfigurePlayerSettings"/> so the budget-phone fixes can never be lost.
    /// </summary>
    public static class ProjectConfigurator
    {
        public const string AppVersion = "0.2.0";
        public const int AppVersionCode = 2;

        public const string ApplicationId = "com.studio.littlebuildersworld";
        public const string VulkanFilterPath = E.Root + "/Settings/VulkanDeviceFilters.asset";
        public const string PowerVrPattern = ".*PowerVR.*";

        public const int VehicleLayer = 8;
        public const int BuildingLayer = 11;

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Nocvault";
            PlayerSettings.productName = "Little Builders World";
            // Bump both for every build handed to testers (phones only update to a higher version code).
            PlayerSettings.bundleVersion = AppVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ApplicationId);
            PlayerSettings.Android.bundleVersionCode = Math.Max(AppVersionCode, PlayerSettings.Android.bundleVersionCode);

            // Landscape only, both ways round.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // IL2CPP with BOTH 32-bit and 64-bit ARM: budget phones such as the Redmi A3 run a 32-bit-only Android
            // and refuse to install a 64-bit-only APK.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < 25) PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true; // desktop test players run unfocused; phones pause regardless
            PlayerSettings.SplashScreen.backgroundColor = E.Hex("#FFEFD2");
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
            EditorUserBuildSettings.buildAppBundle = false;

            ConfigureAndroidGraphics();

            // Desktop test builds: windowed landscape.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
        }

        /// <summary>
        /// Vulkan first, OpenGL ES 3 fallback, and PowerVR GPUs denied Vulkan. On the Redmi A3 (PowerVR GE8320) a
        /// Vulkan build ran once and then crashed on every later launch (suspected: the saved Vulkan pipeline cache).
        /// With PowerVR on OpenGL ES 3 it played normally.
        /// </summary>
        public static void ConfigureAndroidGraphics()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            E.EnsureFolder(Path.GetDirectoryName(VulkanFilterPath));
            var filters = AssetDatabase.LoadAssetAtPath<VulkanDeviceFilterLists>(VulkanFilterPath);
            if (filters == null)
            {
                filters = new VulkanDeviceFilterLists();
                AssetDatabase.CreateAsset(filters, VulkanFilterPath);
            }
            filters.vulkanDeviceDenyFilters = new[] { new VulkanDeviceFilterData { deviceName = PowerVrPattern } };
            EditorUtility.SetDirty(filters);
            AssetDatabase.SaveAssets();
            PlayerSettings.Android.androidVulkanDeviceFilterListAsset = filters;
            Debug.Log("[LBW] Vulkan deny list: " + PowerVrPattern + " (PowerVR GPUs use OpenGL ES 3).");
        }

        /// <summary>Mobile-friendly URP: one hard-shadow cascade at modest distance, no HDR, no post, 2x MSAA.</summary>
        public static void ConfigureRenderPipeline()
        {
            var mobile = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            var pc = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            foreach (RenderPipelineAsset a in new[] { mobile, pc })
            {
                if (a == null) continue;
                var so = new SerializedObject(a);
                void SetI(string n, int v) { SerializedProperty p = so.FindProperty(n); if (p != null) p.intValue = v; }
                void SetF(string n, float v) { SerializedProperty p = so.FindProperty(n); if (p != null) p.floatValue = v; }
                void SetB(string n, bool v) { SerializedProperty p = so.FindProperty(n); if (p != null) p.boolValue = v; }
                SetB("m_MainLightShadowsSupported", true);
                SetB("m_SoftShadowsSupported", false);
                SetF("m_ShadowDistance", 40f);
                SetI("m_ShadowCascadeCount", 1);
                SetI("m_MainLightShadowmapResolution", a == mobile ? 1024 : 2048);
                SetI("m_AdditionalLightsRenderingMode", 0); // main light only
                SetI("m_MSAA", a == mobile ? 2 : 4);
                SetB("m_SupportsHDR", a != mobile);
                if (a == mobile) SetF("m_RenderScale", 1f);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(a);
            }

            if (mobile != null) GraphicsSettings.defaultRenderPipeline = mobile;

            // Android uses the Mobile quality level.
            string[] names = QualitySettings.names;
            int mobileLevel = Array.IndexOf(names, "Mobile");
            if (mobileLevel >= 0)
            {
                var qso = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
                SerializedProperty perPlatform = qso.FindProperty("m_PerPlatformDefaultQuality");
                if (perPlatform != null)
                {
                    for (int i = 0; i < perPlatform.arraySize; i++)
                    {
                        SerializedProperty e = perPlatform.GetArrayElementAtIndex(i);
                        SerializedProperty key = e.FindPropertyRelative("first");
                        SerializedProperty val = e.FindPropertyRelative("second");
                        if (key != null && val != null && key.stringValue == "Android") val.intValue = mobileLevel;
                    }
                    qso.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        public static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            void Name(int index, string name)
            {
                SerializedProperty l = layers.GetArrayElementAtIndex(index);
                if (string.IsNullOrEmpty(l.stringValue) || l.stringValue == name) l.stringValue = name;
                else Debug.LogWarning($"[LBW] Layer {index} is already '{l.stringValue}'; not renamed to {name}.");
            }
            Name(VehicleLayer, "Vehicle");
            Name(BuildingLayer, "Building");
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            // Toy feel: stronger gravity so vehicles settle quickly and never float off bumps.
            var dynamics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")[0]);
            SerializedProperty g = dynamics.FindProperty("m_Gravity");
            if (g != null) g.vector3Value = new Vector3(0f, -20f, 0f);
            dynamics.ApplyModifiedPropertiesWithoutUndo();
        }

        public const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        /// <summary>Imports TextMeshPro's essential resources (shaders, settings) once.</summary>
        public static bool EnsureTextMeshPro()
        {
            if (AssetDatabase.LoadMainAssetAtPath("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null) return true;
            string full = Path.GetFullPath(TmpPackage);
            if (!File.Exists(full))
            {
                Debug.LogError("[LBW] TMP Essential Resources package not found at " + full);
                return false;
            }
            AssetDatabase.ImportPackage(full, false);
            AssetDatabase.Refresh();
            bool ok = AssetDatabase.LoadMainAssetAtPath("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null;
            Debug.Log("[LBW] TMP essentials imported: " + ok);
            return ok;
        }

        public static void ConfigureBuildScenes(params string[] scenePaths)
        {
            var list = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++) list[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            EditorBuildSettings.scenes = list;
        }
    }
}
