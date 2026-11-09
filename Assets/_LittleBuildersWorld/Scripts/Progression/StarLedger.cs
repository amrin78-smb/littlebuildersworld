using System;
using System.Collections.Generic;
using LittleBuildersWorld.Save;

namespace LittleBuildersWorld.Progression
{
    public enum UnlockKind
    {
        Paint,
        YardDecoration,
        /// <summary>Opens a new building site (e.g. the playground).</summary>
        Area
    }

    /// <summary>A cosmetic reward unlocked when the Builder Star total reaches a threshold. Stars are never spent.</summary>
    [Serializable]
    public sealed class UnlockDefinition
    {
        public string id;
        public UnlockKind kind;
        public int starsRequired;
        public UnityEngine.Sprite icon;
        public UnityEngine.Color colour = UnityEngine.Color.white;
    }

    /// <summary>Pure reward rules over <see cref="SaveData"/>: each reward key pays once, unlocks follow the running total.</summary>
    public static class StarLedger
    {
        public static bool HasReward(SaveData data, string rewardKey) => data.grantedRewards.Contains(rewardKey);

        /// <summary>Grants the stars once per key. Returns false (and changes nothing) for a repeat.</summary>
        public static bool TryGrant(SaveData data, string rewardKey, int stars)
        {
            if (string.IsNullOrEmpty(rewardKey) || stars <= 0) return false;
            if (data.grantedRewards.Contains(rewardKey)) return false;
            data.grantedRewards.Add(rewardKey);
            data.builderStars += stars;
            return true;
        }

        /// <summary>Adds every unlock the current total qualifies for. Earned unlocks are never removed.</summary>
        public static List<UnlockDefinition> EvaluateUnlocks(SaveData data, IList<UnlockDefinition> unlocks)
        {
            var newlyUnlocked = new List<UnlockDefinition>();
            if (unlocks == null) return newlyUnlocked;
            foreach (UnlockDefinition u in unlocks)
            {
                if (u == null || string.IsNullOrEmpty(u.id)) continue;
                if (data.builderStars >= u.starsRequired && !data.unlocked.Contains(u.id))
                {
                    data.unlocked.Add(u.id);
                    newlyUnlocked.Add(u);
                }
            }
            return newlyUnlocked;
        }
    }
}
