using System;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    public enum CelebrationSize
    {
        /// <summary>A unit of work: a scoop, a delivery, a rock in the skip.</summary>
        Small,
        /// <summary>A stage finished.</summary>
        Stage,
        /// <summary>A whole project finished.</summary>
        Project
    }

    /// <summary>Scene-wide feedback hooks so activities do not need references to FX, audio or HUD.</summary>
    public static class ActivityEvents
    {
        public static event Action<Vector3, CelebrationSize> Celebrate;

        public static void RaiseCelebrate(Vector3 position, CelebrationSize size) => Celebrate?.Invoke(position, size);
    }

    /// <summary>Something the guidance arrow can point the current vehicle toward.</summary>
    public interface IGuidanceTarget
    {
        bool TryGetGuidance(Vehicles.Vehicle vehicle, out Vector3 position, out int priority);
    }

    public static class GuidanceRegistry
    {
        private static readonly System.Collections.Generic.List<IGuidanceTarget> s_Targets = new System.Collections.Generic.List<IGuidanceTarget>();

        public static void Register(IGuidanceTarget t)
        {
            if (!s_Targets.Contains(t)) s_Targets.Add(t);
        }

        public static void Unregister(IGuidanceTarget t) => s_Targets.Remove(t);

        /// <summary>Highest priority wins; ties go to the nearest.</summary>
        public static bool TryFind(Vehicles.Vehicle vehicle, out Vector3 best)
        {
            best = default;
            int bestPriority = int.MinValue;
            float bestSq = float.MaxValue;
            Vector3 from = vehicle.transform.position;
            for (int i = 0; i < s_Targets.Count; i++)
            {
                if (!s_Targets[i].TryGetGuidance(vehicle, out Vector3 p, out int priority)) continue;
                float sq = (p - from).sqrMagnitude;
                if (priority > bestPriority || (priority == bestPriority && sq < bestSq))
                {
                    bestPriority = priority;
                    bestSq = sq;
                    best = p;
                }
            }
            return bestPriority != int.MinValue;
        }
    }
}
