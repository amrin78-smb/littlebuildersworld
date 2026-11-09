using System;
using LittleBuildersWorld.Vehicles;
using UnityEditor;
using UnityEngine;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// One-click (or batch) generation of the whole game: settings, art, audio, data, prefabs and scenes.
    /// Re-runnable; asset GUIDs are kept. Batch: Tools/setup.sh
    /// </summary>
    public static class ProjectSetup
    {
        public const string MenuRoot = "Little Builders World/";

        [MenuItem(MenuRoot + "Setup Project (generate everything)", priority = 1)]
        public static void Run()
        {
            try
            {
                Step("Project settings", 0.02f);
                ProjectConfigurator.ConfigureLayers();
                ProjectConfigurator.ConfigurePlayerSettings();
                ProjectConfigurator.ConfigureRenderPipeline();
                Step("TextMeshPro", 0.06f);
                if (!ProjectConfigurator.EnsureTextMeshPro()) throw new Exception("TextMeshPro essentials are not imported (run Tools/setup.sh, which imports them first).");
                UIBuilder.Font();

                Step("Icons & textures", 0.1f);
                ArtLibrary.ClearCache();
                IconPainter.GenerateAll();
                Step("Audio", 0.2f);
                AudioSynth.GenerateAll();

                Step("Vehicle data", 0.3f);
                var profiles = DataBuilder.BuildProfiles();
                Step("Vehicles", 0.4f);
                VehicleBuilder.BuildAll(profiles);
                Step("Portraits", 0.5f);
                var portraits = PortraitRenderer.RenderAll();
                profiles = DataBuilder.LoadProfiles();
                DataBuilder.AssignPortraits(profiles, portraits);
                ConfigureAppIcon();
                Step("Config", 0.55f);
                var audio = DataBuilder.BuildAudio();
                var guide = DataBuilder.BuildGuide(out string thaiCharacters);
                UIBuilder.ThaiFallback(thaiCharacters);
                DataBuilder.BuildConfig(profiles, audio, guide);
                Step("Props", 0.6f);
                PropBuilder.BuildAll();
                AssetDatabase.SaveAssets();

                Step("Scenes", 0.7f);
                E.EnsureFolder(E.Root + "/Scenes");
                MenuBuilder.BuildBoot();
                MenuBuilder.BuildMenu();
                Step("Town", 0.8f);
                TownBuilder.Build(DataBuilder.LoadProfiles());
                ProjectConfigurator.ConfigureBuildScenes(MenuBuilder.BootPath, MenuBuilder.MenuPath, TownBuilder.ScenePath);
                AssetDatabase.SaveAssets();
                Step("Validate", 0.95f);
                bool ok = ProjectValidator.Validate();
                Debug.Log("[LBW] Setup finished. Validation " + (ok ? "passed" : "FAILED"));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>Batch step 1: TextMeshPro's package import only completes after this method returns.</summary>
        public static void ImportTmpBatch()
        {
            ProjectConfigurator.EnsureTextMeshPro();
        }

        /// <summary>Batch entry: exits non-zero if generation or validation fails.</summary>
        public static void RunBatch()
        {
            try
            {
                Run();
                EditorApplication.Exit(ProjectValidator.Validate() ? 0 : 2);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void Step(string label, float p)
        {
            Debug.Log("[LBW] Setup: " + label);
            if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("Little Builders World", label, p);
        }

        private static void ConfigureAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitRenderer.AppIconPath);
            if (icon == null) return;
            var imp = (TextureImporter)AssetImporter.GetAtPath(PortraitRenderer.AppIconPath);
            imp.textureType = TextureImporterType.Default;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }
    }
}
