using System;
using UnityEngine;

namespace LittleBuildersWorld.Save
{
    public enum LoadOutcome
    {
        Loaded,
        Migrated,
        Missing,
        Corrupt,
        /// <summary>Written by a newer build. Loaded best-effort; unknown fields are ignored.</summary>
        NewerVersion
    }

    /// <summary>Pure JSON (de)serialisation and migration for <see cref="SaveData"/>. No file IO, so it is unit tested.</summary>
    public static class SaveSerializer
    {
        [Serializable]
        private sealed class VersionProbe
        {
            public int version; // 0 when the field is missing (pre-versioned file)
        }

        public static string Serialize(SaveData data)
        {
            data.version = SaveData.CurrentVersion;
            return JsonUtility.ToJson(data, true);
        }

        public static SaveData Deserialize(string json, out LoadOutcome outcome)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                outcome = LoadOutcome.Missing;
                return new SaveData();
            }

            VersionProbe probe;
            try
            {
                probe = JsonUtility.FromJson<VersionProbe>(json);
            }
            catch (Exception)
            {
                outcome = LoadOutcome.Corrupt;
                return new SaveData();
            }
            if (probe == null || probe.version < 0 || !json.TrimStart().StartsWith("{"))
            {
                outcome = LoadOutcome.Corrupt;
                return new SaveData();
            }

            SaveData data;
            try
            {
                data = Migrate(json, probe.version);
            }
            catch (Exception)
            {
                outcome = LoadOutcome.Corrupt;
                return new SaveData();
            }
            if (data == null)
            {
                outcome = LoadOutcome.Corrupt;
                return new SaveData();
            }

            data.Sanitise();
            if (probe.version > SaveData.CurrentVersion) outcome = LoadOutcome.NewerVersion;
            else if (probe.version < SaveData.CurrentVersion) outcome = LoadOutcome.Migrated;
            else outcome = LoadOutcome.Loaded;
            data.version = SaveData.CurrentVersion;
            return data;
        }

        /// <summary>
        /// Upgrades older JSON one version at a time. Version 1 is the first released shape, so there are no steps yet.
        /// Add future steps as: if (version == 1) { json = V1ToV2(json); version = 2; } ...
        /// Version 0 means a file without a version field: same shape as 1.
        /// </summary>
        public static SaveData Migrate(string json, int version)
        {
            return JsonUtility.FromJson<SaveData>(json);
        }
    }
}
