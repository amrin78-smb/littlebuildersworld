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
                        d.PushTo(transform.TransformPoint(local));
                    }
                }
                else if (Mathf.Abs(local.x) < m_HalfWidth + r && local.z < m_Front + r && local.z > -m_Back - r)
                {
                    float side = local.x >= 0f ? 1f : -1f;
                    local.x = Mathf.MoveTowards(local.x, side * (m_HalfWidth + r), 5f * Time.fixedDeltaTime);
                    d.PushTo(transform.TransformPoint(local));
                }
            }
        }
    }
}
