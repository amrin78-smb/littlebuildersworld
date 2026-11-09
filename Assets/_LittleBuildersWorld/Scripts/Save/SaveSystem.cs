using System;
using System.IO;
using UnityEngine;

namespace LittleBuildersWorld.Save
{
    /// <summary>
    /// Local JSON save in Application.persistentDataPath. Writes go to a temp file first and the previous good file
    /// is kept as a backup, so a crash or power loss mid-write never loses progress. Saves on request (debounced),
    /// when the app is paused or loses focus, and on quit.
    /// </summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        public const string FileName = "littlebuilders_save.json";
        private const float DebounceSeconds = 1.5f;

        public SaveData Data { get; private set; } = new SaveData();
        public LoadOutcome LastLoadOutcome { get; private set; }

        /// <summary>Raised after progress has been wiped by the adult reset.</summary>
        public event Action ProgressReset;

        private string m_Directory;
        private float m_SaveDueAt = -1f;

        public string SavePath => Path.Combine(m_Directory, FileName);
        private string BackupPath => SavePath + ".bak";
        private string TempPath => SavePath + ".tmp";

        /// <summary>Lets tests point the save somewhere disposable. Call before <see cref="Load"/>.</summary>
        public void Initialise(string directory)
        {
            m_Directory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
        }

        public void Load()
        {
            if (m_Directory == null) Initialise(null);
            Data = ReadFile(SavePath, out LoadOutcome outcome);
            if (outcome == LoadOutcome.Corrupt || outcome == LoadOutcome.Missing)
            {
                SaveData backup = ReadFile(BackupPath, out LoadOutcome backupOutcome);
                if (backupOutcome != LoadOutcome.Corrupt && backupOutcome != LoadOutcome.Missing)
                {
                    Debug.LogWarning($"[LBW Save] Main save was {outcome}; restored the backup.");
                    Data = backup;
                    outcome = backupOutcome;
                }
                else if (outcome == LoadOutcome.Corrupt)
                {
                    Debug.LogWarning("[LBW Save] Save file was unreadable; starting with fresh progress.");
                }
            }
            LastLoadOutcome = outcome;
        }

        private static SaveData ReadFile(string path, out LoadOutcome outcome)
        {
            try
            {
                if (!File.Exists(path))
                {
                    outcome = LoadOutcome.Missing;
                    return new SaveData();
                }
                return SaveSerializer.Deserialize(File.ReadAllText(path), out outcome);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LBW Save] Could not read {path}: {e.Message}");
                outcome = LoadOutcome.Corrupt;
                return new SaveData();
            }
        }

        /// <summary>Schedule a save shortly (several changes in a row produce one write).</summary>
        public void RequestSave()
        {
            if (m_SaveDueAt < 0f) m_SaveDueAt = Time.unscaledTime + DebounceSeconds;
        }

        public bool SaveNow()
        {
            m_SaveDueAt = -1f;
            if (m_Directory == null) Initialise(null);
            try
            {
                Directory.CreateDirectory(m_Directory);
                File.WriteAllText(TempPath, SaveSerializer.Serialize(Data));
                if (File.Exists(SavePath))
                {
                    File.Copy(SavePath, BackupPath, true);
                    File.Delete(SavePath);
                }
                File.Move(TempPath, SavePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LBW Save] Save failed: {e.Message}");
                return false;
            }
        }

        /// <summary>Adult-confirmed reset. Keeps audio, control and language settings.</summary>
        public void ResetProgress()
        {
            var fresh = new SaveData { audio = Data.audio, controls = Data.controls, language = Data.language };
            Data = fresh;
            SaveNow();
            ProgressReset?.Invoke();
        }

        private void Update()
        {
            if (m_SaveDueAt >= 0f && Time.unscaledTime >= m_SaveDueAt) SaveNow();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && m_SaveDueAt >= 0f) SaveNow();
        }

        private void OnApplicationQuit() => SaveNow();
    }
}
