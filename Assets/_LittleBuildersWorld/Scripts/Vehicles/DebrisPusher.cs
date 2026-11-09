using LittleBuildersWorld.World;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>
    /// Moves debris that the driven vehicle drives into. The bulldozer carries it along in front of its blade
    /// (a forgiving box, wider than the blade); other vehicles just nudge it aside so they never get blocked.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public sealed class DebrisPusher : MonoBehaviour
    {
        public enum Mode { CarryWithBlade, NudgeAside }

        [SerializeField] private Mode m_Mode = Mode.NudgeAside;
        [Tooltip("Half the vehicle's width, m.")] [SerializeField] private float m_HalfWidth = 1.4f;
        [Tooltip("Distance from the pivot to the front face (the blade), m.")] [SerializeField] private float m_Front = 2.6f;
        [Tooltip("Distance from the pivot to the back, m.")] [SerializeField] private float m_Back = 2.2f;
        [Tooltip("Extra forgiving reach in front of the blade, m.")] [SerializeField] private float m_Reach = 1.2f;

        private VehicleController m_Controller;

        /// <summary>Diagnostics: the last push a wall stopped (read by the automated play-through).</summary>
        public static string LastBlock { get; private set; } = "";

        /// <summary>
        /// Debris has no collider of its own, so a move is checked against walls first: fences and buildings stop it
        /// (it piles up on the inside instead of passing through). Vehicles (rigidbodies) and debris never block.
        /// </summary>
        private static void MoveUnlessBlocked(PushableDebris d, Vector3 target)
        {
            Vector3 from = d.transform.position;
            Vector3 delta = target - from;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist > 1e-4f)
            {
                Vector3 dir = delta / dist;
                var origin = new Vector3(from.x, from.y + 1f, from.z);
                if (Physics.SphereCast(origin, 0.3f, dir, out RaycastHit hit, dist + d.Radius, ~0, QueryTriggerInteraction.Ignore)
                    && hit.rigidbody == null && hit.collider.GetComponentInParent<PushableDebris>() == null)
                {
                    float allowed = Mathf.Max(0f, hit.distance - d.Radius);
                    LastBlock = $"{d.name} blocked by {hit.collider.name} at {hit.distance:0.00} (allowed {allowed:0.00})";
                    if (allowed < 0.01f) return;
                    target = from + dir * Mathf.Min(dist, allowed);
                    target.y = from.y;
                }
            }
            d.PushTo(target);
        }

        private void Awake() => m_Controller = GetComponent<VehicleController>();

        private void FixedUpdate()
        {
            if (!m_Controller.IsDriven) return;
            float speed = m_Controller.Speed;
            var all = PushableDebris.All;
            for (int i = 0; i < all.Count; i++)
            {
                PushableDebris d = all[i];
                if (!d.IsPushable) continue;
                Vector3 local = transform.InverseTransformPoint(d.transform.position);
                float r = d.Radius;
                if (m_Mode == Mode.CarryWithBlade && speed > 0.05f &&
                    Mathf.Abs(local.x) < m_HalfWidth + r * 0.6f && local.z > -0.5f && local.z < m_Front + r + m_Reach)
                {
                    float wanted = m_Front + r;
                    if (local.z < wanted)
                    {
                        local.z = Mathf.MoveTowards(local.z, wanted, 6f * Time.fixedDeltaTime + (wanted - local.z) * 0.5f);
                        local.x *= 0.985f; // drift gently toward the middle of the blade so it is not lost off the side
                        MoveUnlessBlocked(d, transform.TransformPoint(local));
                    }
                }
                else if (Mathf.Abs(local.x) < m_HalfWidth + r && local.z < m_Front + r && local.z > -m_Back - r)
                {
                    float side = local.x >= 0f ? 1f : -1f;
                    local.x = Mathf.MoveTowards(local.x, side * (m_HalfWidth + r), 5f * Time.fixedDeltaTime);
                    MoveUnlessBlocked(d, transform.TransformPoint(local));
                }
            }
        }
    }
}
