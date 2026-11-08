using System;
using System.Collections.Generic;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Save;
using UnityEngine;

namespace LittleBuildersWorld.Progression
{
    /// <summary>Grants Builder Stars (once per reward key), unlocks cosmetics at star totals and tells the HUD to celebrate.</summary>
    public sealed class BuilderStarsManager : MonoBehaviour
    {
        private SaveSystem m_Save;
        private GameConfig m_Config;

        /// <summary>(stars just awarded, new total, world position of the achievement).</summary>
        public event Action<int, int, Vector3> StarsAwarded;
        public event Action<UnlockDefinition> Unlocked;
        public event Action TotalChanged;

        public int Total => m_Save != null ? m_Save.Data.builderStars : 0;

        public void Initialise(SaveSystem save, GameConfig config)
        {
            m_Save = save;
            m_Config = config;
            m_Save.ProgressReset += Refresh;
            Refresh();
        }

        /// <summary>Re-applies unlock thresholds (after load or reset) without celebrating.</summary>
        public void Refresh()
        {
            StarLedger.EvaluateUnlocks(m_Save.Data, m_Config.unlocks);
            TotalChanged?.Invoke();
        }

        public bool HasReward(string rewardKey) => StarLedger.HasReward(m_Save.Data, rewardKey);

        public bool IsUnlocked(string unlockId) => string.IsNullOrEmpty(unlockId) || m_Save.Data.unlocked.Contains(unlockId);

        /// <summary>Awards stars for a milestone. Returns false if this milestone already paid out.</summary>
        public bool Award(string rewardKey, int stars, Vector3 worldPosition)
        {
            if (!StarLedger.TryGrant(m_Save.Data, rewardKey, stars)) return false;
            List<UnlockDefinition> fresh = StarLedger.EvaluateUnlocks(m_Save.Data, m_Config.unlocks);
            m_Save.RequestSave();
            StarsAwarded?.Invoke(stars, Total, worldPosition);
            TotalChanged?.Invoke();
            foreach (UnlockDefinition u in fresh) Unlocked?.Invoke(u);
            return true;
        }

        public UnlockDefinition NextUnlock()
        {
            UnlockDefinition best = null;
            foreach (UnlockDefinition u in m_Config.unlocks)
            {
                if (u == null || IsUnlocked(u.id)) continue;
                if (best == null || u.starsRequired < best.starsRequired) best = u;
            }
            return best;
        }
    }
}
