using System.IO;
using UnityEditor;
using UnityEngine;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>Safe, re-runnable asset helpers used by the setup pipeline.</summary>
    public static class EditorAssetUtil
    {
        public const string Root = "Assets/_LittleBuildersWorld";

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>Creates the asset, or copies the new data into the existing asset so GUID references survive.</summary>
        public static T CreateOrReplace<T>(T asset, string path) where T : Object
        {
            EnsureFolder(Path.GetDirectoryName(path));
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            if (existing is Mesh em && asset is Mesh nm)
            {
                em.Clear();
                EditorUtility.CopySerialized(nm, em);
                em.name = Path.GetFileNameWithoutExtension(path);
            }
            else
            {
                EditorUtility.CopySerialized(asset, existing);
                existing.name = Path.GetFileNameWithoutExtension(path);
            }
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>Loads a ScriptableObject or creates it (keeping the GUID on re-runs).</summary>
        public static T LoadOrCreateSO<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(Path.GetDirectoryName(path));
            T so = AssetDatabase.LoadAssetAtPath<T>(path);
            if (so != null) return so;
            so = ScriptableObject.CreateInstance<T>();
            so.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        public static GameObject SavePrefab(GameObject go, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
            if (!ok) Debug.LogError("[LBW] Failed to save prefab " + path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Sets a serialized (possibly private) field.</summary>
        public static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[LBW] Field '{field}' not found on {target.GetType().Name}");
                return;
            }
            Assign(p, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[LBW] Array '{field}' not found on {target.GetType().Name}");
                return;
            }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(SerializedProperty p, object value)
        {
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case float f: p.floatValue = f; break;
                case int i:
                    if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = i;
                    else p.intValue = i;
                    break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Vector2 v2: p.vector2Value = v2; break;
                case Vector3 v3: p.vector3Value = v3; break;
                case Color c: p.colorValue = c; break;
                case System.Enum e: p.intValue = System.Convert.ToInt32(e); break;
                case null: p.objectReferenceValue = null; break;
                default: Debug.LogError("[LBW] Unsupported value type " + value.GetType()); break;
            }
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out Color c);
            return c;
        }
    }
}
