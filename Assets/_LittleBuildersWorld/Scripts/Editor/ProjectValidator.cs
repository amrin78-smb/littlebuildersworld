using System.Collections.Generic;
using System.Linq;
using LittleBuildersWorld.Activities;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Checks the generated project for broken references and the budget-phone build settings.
    /// Every serialized object reference in our components must be set, except a short allow-list of optional ones.
    /// </summary>
    public static class ProjectValidator
    {
        private static readonly HashSet<string> OptionalFields = new HashSet<string>
        {
            "ActivityBase.m_Marker", "HouseConstructionActivity.m_Marker", "RoadworksActivity.m_Marker",
            "ExcavationActivity.m_OpenVisual", "MaterialDeliveryActivity.m_OpenVisual", "BulldozerClearingActivity.m_OpenVisual",
            "ExcavatorArm.m_Dust", "VehicleVisuals.m_Beacon", "LoadingBay.m_DropVisual", "SettingsPanel.m_Input",
            "VehicleProfile.engineLoop", "RoadRollerActivity.m_OpenVisual"
        };

        [MenuItem(ProjectSetup.MenuRoot + "Validate Project", priority = 2)]
        public static bool Validate()
        {
            var errors = new List<string>();
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(DataBuilder.ConfigPath);
            if (cfg == null) errors.Add("GameConfig missing from Resources");
            else
            {
                if (cfg.vehicles.Length != 4 || cfg.vehicles.Any(v => v == null || v.portrait == null)) errors.Add("GameConfig vehicles/portraits incomplete");
                if (cfg.audio == null || cfg.audio.sfx.Any(s => s.clip == null)) errors.Add("Audio library incomplete");
            }

            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) == 0 ||
                (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                errors.Add("Android must target both ARMv7 and ARM64 (32-bit phones such as the Redmi A3)");
            var filters = PlayerSettings.Android.androidVulkanDeviceFilterListAsset;
            if (filters == null || filters.vulkanDeviceDenyFilters == null || !filters.vulkanDeviceDenyFilters.Any(f => f.deviceName == ProjectConfigurator.PowerVrPattern))
                errors.Add("Vulkan deny filter for PowerVR is missing");

            foreach (string scene in new[] { MenuBuilder.BootPath, MenuBuilder.MenuPath, TownBuilder.ScenePath })
            {
                if (AssetDatabase.LoadMainAssetAtPath(scene) == null) { errors.Add("Missing scene " + scene); continue; }
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (mb == null) { errors.Add($"{scene}: missing script"); continue; }
                    if (!mb.GetType().Namespace?.StartsWith("LittleBuildersWorld") ?? true) continue;
                    CheckRefs(mb, scene, errors);
                }
                if (scene == TownBuilder.ScenePath)
                {
                    var ids = Object.FindObjectsByType<ActivityBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(a => a.Id).ToList();
                    foreach (string dup in ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key)) errors.Add("Duplicate activity id " + dup);
                    if (Object.FindObjectsByType<Vehicle>(FindObjectsSortMode.None).Length != 4) errors.Add("Town must contain 4 vehicles");
                }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorAssetUtil.Root + "/Prefabs" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (MonoBehaviour mb in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) errors.Add($"{prefab.name}: missing script");
                    else CheckRefs(mb, prefab.name, errors);
                }
            }

            foreach (string e in errors) Debug.LogError("[LBW Validate] " + e);
            Debug.Log($"[LBW Validate] {(errors.Count == 0 ? "OK" : errors.Count + " problem(s)")}");
            return errors.Count == 0;
        }

        private static void CheckRefs(Object target, string where, List<string> errors)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.GetIterator();
            while (p.NextVisible(true))
            {
                if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue != null) continue;
                if (p.name == "m_Script") continue;
                string key = target.GetType().Name + "." + p.name;
                string baseKey = "ActivityBase." + p.name;
                if (OptionalFields.Contains(key) || (target is ActivityBase && OptionalFields.Contains(baseKey) && !(target is ProjectActivity) && p.name == "m_Marker")) continue;
                if (target is ProjectActivity && p.name == "m_Marker") continue;
                errors.Add($"{where}: {target.GetType().Name} on '{(target as Component)?.name}' has empty {p.propertyPath}");
            }
        }
    }
}
