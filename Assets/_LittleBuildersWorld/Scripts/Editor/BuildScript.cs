using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Player builds. Every Android build re-applies the budget-phone settings (ARMv7 + ARM64, PowerVR → GLES3).
    /// Batch: Tools/build_android.sh [release], Tools/autopilot.sh (Linux test player).
    /// </summary>
    public static class BuildScript
    {
        public const string AndroidDevPath = "Builds/Android/LittleBuildersWorld-dev.apk";
        public const string AndroidReleasePath = "Builds/Android/LittleBuildersWorld.apk";
        public const string LinuxPath = "Builds/Linux/LittleBuildersWorld.x86_64";

        private static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem(ProjectSetup.MenuRoot + "Build Android APK (development)", priority = 30)]
        public static void AndroidDevelopment() => BuildAndroid(true);

        [MenuItem(ProjectSetup.MenuRoot + "Build Android APK (release)", priority = 31)]
        public static void AndroidRelease() => BuildAndroid(false);

        public static void AndroidDevelopmentBatch() => Exit(BuildAndroid(true));
        public static void AndroidReleaseBatch() => Exit(BuildAndroid(false));

        public static BuildReport BuildAndroid(bool development)
        {
            ProjectConfigurator.ConfigurePlayerSettings();
            EditorUserBuildSettings.buildAppBundle = false;
            string path = development ? AndroidDevPath : AndroidReleasePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development ? BuildOptions.Development : BuildOptions.None
            });
            Log(report);
            return report;
        }

        [MenuItem(ProjectSetup.MenuRoot + "Build Linux Test Player", priority = 32)]
        public static void Linux() => BuildLinux();

        public static void LinuxBatch() => Exit(BuildLinux());

        public static BuildReport BuildLinux()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Directory.CreateDirectory(Path.GetDirectoryName(LinuxPath));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = LinuxPath,
                target = BuildTarget.StandaloneLinux64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });
            Log(report);
            return report;
        }

        private static void Exit(BuildReport r) => EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);

        private static void Log(BuildReport report)
        {
            BuildSummary s = report.summary;
            Debug.Log($"[LBW Build] {s.platform} result={s.result} size={s.totalSize / (1024f * 1024f):0.0} MB " +
                      $"time={s.totalTime.TotalSeconds:0}s errors={s.totalErrors} warnings={s.totalWarnings} output={s.outputPath}");
            foreach (BuildStep step in report.steps)
            foreach (BuildStepMessage m in step.messages)
                if (m.type == LogType.Error || m.type == LogType.Exception) Debug.LogError("[LBW Build] " + m.content);
        }
    }
}
