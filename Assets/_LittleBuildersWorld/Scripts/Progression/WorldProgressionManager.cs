using System;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Save;
using LittleBuildersWorld.Vehicles;

namespace LittleBuildersWorld.Progression
{
    /// <summary>Persistent world state: activity progress, chosen vehicle, paint choices and vehicle availability.</summary>
    public sealed class WorldProgressionManager : UnityEngine.MonoBehaviour
    {
        private SaveSystem m_Save;
        private GameConfig m_Config;

        public event Action<string> ActivityChanged;
        public event Action<string> PaintChanged;

        public void Initialise(SaveSystem save, GameConfig config)
        {
            m_Save = save;
            m_Config = config;
        }

        public ActivityRecord GetRecord(string activityId) => m_Save.Data.GetActivity(activityId, true);

        /// <summary>Stores the activity's new state and schedules an autosave (a safe point).</summary>
        public void Commit(string activityId, int stage, int progress, bool completedNow)
        {
            ActivityRecord r = GetRecord(activityId);
            r.stage = stage;
            r.progress = progress;
            if (completedNow) r.timesCompleted++;
            m_Save.RequestSave();
            ActivityChanged?.Invoke(activityId);
        }

        public bool IsVehicleAvailable(VehicleProfile profile) =>
            profile != null && m_Save.Data.builderStars >= profile.starsRequired;

        public string SelectedVehicle
        {
            get => m_Save.Data.selectedVehicle;
            set
            {
                if (m_Save.Data.selectedVehicle == value) return;
                m_Save.Data.selectedVehicle = value;
                m_Save.RequestSave();
            }
        }

        public int GetPaint(string vehicleId) => m_Save.Data.GetPaint(vehicleId);

        public bool IsPaintAvailable(int index)
        {
            if (index <= 0) return true;
            if (index >= m_Config.paints.Length) return false;
            string unlock = m_Config.paints[index].unlockId;
            return string.IsNullOrEmpty(unlock) || m_Save.Data.unlocked.Contains(unlock);
        }

        /// <summary>Next available paint after the current one (wraps to factory paint).</summary>
        public int CyclePaint(string vehicleId)
        {
            int count = Math.Max(1, m_Config.paints.Length);
            int current = GetPaint(vehicleId);
            for (int step = 1; step <= count; step++)
            {
                int candidate = (current + step) % count;
                if (IsPaintAvailable(candidate))
                {
                    m_Save.Data.SetPaint(vehicleId, candidate);
                    m_Save.RequestSave();
                    PaintChanged?.Invoke(vehicleId);
                    return candidate;
                }
            }
            return current;
        }

        public int AvailablePaintCount()
        {
            int n = 0;
            for (int i = 0; i < m_Config.paints.Length; i++) if (IsPaintAvailable(i)) n++;
            return n;
        }
    }
}
