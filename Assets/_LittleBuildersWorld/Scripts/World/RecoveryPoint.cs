using System.Collections.Generic;
using UnityEngine;

namespace LittleBuildersWorld.World
{
    /// <summary>A safe spot on a road where a stuck vehicle can be put back, facing along the road.</summary>
    public sealed class RecoveryPoint : MonoBehaviour
    {
        private static readonly List<RecoveryPoint> s_All = new List<RecoveryPoint>();

        private void OnEnable() => s_All.Add(this);
        private void OnDisable() => s_All.Remove(this);

        public static RecoveryPoint Nearest(Vector3 position)
        {
            RecoveryPoint best = null;
            float bestSq = float.MaxValue;
            foreach (RecoveryPoint p in s_All)
            {
                float sq = (p.transform.position - position).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = p;
                }
            }
            return best;
        }

        /// <summary>Nearest point that passes <paramref name="isFree"/> (e.g. no parked vehicle on it).</summary>
        public static RecoveryPoint NearestWhere(Vector3 position, System.Func<RecoveryPoint, bool> isFree)
        {
            var sorted = new List<RecoveryPoint>(s_All);
            sorted.Sort((a, b) => (a.transform.position - position).sqrMagnitude.CompareTo((b.transform.position - position).sqrMagnitude));
            foreach (RecoveryPoint p in sorted) if (isFree(p)) return p;
            return sorted.Count > 0 ? sorted[0] : null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.5f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * 2f);
        }
    }
}
