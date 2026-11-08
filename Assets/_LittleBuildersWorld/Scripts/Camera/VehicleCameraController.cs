using LittleBuildersWorld.Vehicles;
using UnityEngine;

namespace LittleBuildersWorld.CameraSystem
{
    /// <summary>
    /// Lightweight third-person camera (no Cinemachine needed). Follows the driven vehicle from behind and above with
    /// damped position and yaw, looks slightly ahead, pulls in instead of clipping through buildings, ignores quick
    /// reversing so it never spins, and drifts to a 3/4 showcase angle when the vehicle rests. When nobody is driving
    /// it frames an overview of the chosen spot. Every change of target is damped, so it never snaps.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class VehicleCameraController : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private float m_Distance = 9.5f;
        [SerializeField] private float m_Height = 5.2f;
        [SerializeField] private float m_LookHeight = 1.4f;
        [Tooltip("How far ahead of the vehicle to look at top speed, m.")] [SerializeField] private float m_LookAhead = 3f;
        [Tooltip("Position smoothing time, s.")] [SerializeField] private float m_PositionDamping = 0.3f;
        [Tooltip("How quickly the camera swings behind the vehicle, 1/s.")] [SerializeField] private float m_YawFollow = 2f;
        [Tooltip("Yaw follow while reversing, 1/s. Low, so backing up never whips the camera round.")] [SerializeField] private float m_ReverseYawFollow = 0.35f;

        [Header("Showcase when resting")]
        [SerializeField] private float m_ShowcaseDelay = 4f;
        [SerializeField] private float m_ShowcaseYaw = 28f;
        [SerializeField] private float m_ShowcaseZoom = 0.12f;

        [Header("Overview (not driving)")]
        [SerializeField] private float m_OverviewDistance = 17f;
        [SerializeField] private float m_OverviewHeight = 12f;

        [Header("Collision")]
        [SerializeField] private LayerMask m_Obstacles = 1;
        [SerializeField] private float m_CollisionRadius = 0.45f;
        [SerializeField] private float m_MinDistance = 3f;

        private Vehicle m_Target;
        private Vector3 m_Focus;
        private float m_Yaw;
        private float m_OverviewYaw;
        private Vector3 m_Velocity;
        private Vector3 m_Look;
        private Vector3 m_LookVelocity;
        private float m_StillTime;
        private float m_Showcase;
        private float m_CollisionDistance = float.MaxValue;
        private bool m_HasPose;

        public Vehicle Target => m_Target;
        public bool IsFollowing => m_Target != null;

        public void Follow(Vehicle vehicle)
        {
            if (vehicle == null) return;
            if (m_Target == null) m_Yaw = YawFromCamera(vehicle.transform.position);
            m_Target = vehicle;
            m_StillTime = 0f;
        }

        public void Overview(Vector3 focus, float yaw)
        {
            m_Target = null;
            m_Focus = focus;
            m_OverviewYaw = yaw;
        }

        /// <summary>Jump straight to the current goal (scene start only).</summary>
        public void Snap()
        {
            ComputeGoal(out Vector3 pos, out Vector3 look, 0f);
            transform.position = pos;
            m_Look = look;
            transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
            m_Velocity = m_LookVelocity = Vector3.zero;
            m_HasPose = true;
        }

        private float YawFromCamera(Vector3 target)
        {
            Vector3 d = target - transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg : 0f;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (!m_HasPose) Snap();
            ComputeGoal(out Vector3 goal, out Vector3 look, dt);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref m_Velocity, m_PositionDamping);
            m_Look = Vector3.SmoothDamp(m_Look, look, ref m_LookVelocity, m_PositionDamping * 0.6f);
            Vector3 dir = m_Look - transform.position;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        private void ComputeGoal(out Vector3 position, out Vector3 look, float dt)
        {
            Vector3 pivot;
            float yaw;
            float distance;
            float height;

            if (m_Target != null)
            {
                Transform t = m_Target.transform;
                float speed = m_Target.Controller != null ? m_Target.Controller.Speed : 0f;
                bool busy = m_Target.Tool != null && m_Target.Tool.IsBusy;
                float follow = speed < -0.2f ? m_ReverseYawFollow : m_YawFollow;
                if (busy) follow = 0f; // never swing during a construction animation
                if (dt > 0f) m_Yaw = Mathf.LerpAngle(m_Yaw, t.eulerAngles.y, 1f - Mathf.Exp(-follow * dt));

                bool still = Mathf.Abs(speed) < 0.15f && !busy;
                m_StillTime = still ? m_StillTime + dt : 0f;
                float showcaseTarget = m_StillTime > m_ShowcaseDelay ? 1f : 0f;
                m_Showcase = Mathf.MoveTowards(m_Showcase, showcaseTarget, dt * (showcaseTarget > m_Showcase ? 0.25f : 1.2f));
                float s = Mathf.SmoothStep(0f, 1f, m_Showcase);

                float ahead = Mathf.Clamp(speed / 7f, -0.3f, 1f) * m_LookAhead;
                pivot = t.position + Vector3.up * m_LookHeight + Flat(t.forward) * ahead;
                yaw = m_Yaw + m_ShowcaseYaw * s;
                distance = m_Distance * (1f + m_ShowcaseZoom * s);
                height = m_Height;
            }
            else
            {
                pivot = m_Focus + Vector3.up;
                yaw = m_OverviewYaw;
                distance = m_OverviewDistance;
                height = m_OverviewHeight;
                m_Showcase = 0f;
                m_StillTime = 0f;
            }

            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 wanted = pivot - rot * Vector3.forward * distance + Vector3.up * (height - m_LookHeight);
            position = AvoidObstacles(pivot, wanted, dt);
            look = pivot;
        }

        private Vector3 AvoidObstacles(Vector3 pivot, Vector3 wanted, float dt)
        {
            Vector3 dir = wanted - pivot;
            float full = dir.magnitude;
            if (full < 0.01f) return wanted;
            dir /= full;
            float allowed = full;
            if (Physics.SphereCast(pivot, m_CollisionRadius, dir, out RaycastHit hit, full, m_Obstacles, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(m_MinDistance, hit.distance - 0.1f);
            // Pull in immediately (no clipping), ease back out slowly (no pumping).
            m_CollisionDistance = allowed < m_CollisionDistance || dt <= 0f
                ? allowed
                : Mathf.MoveTowards(m_CollisionDistance, allowed, dt * 4f);
            m_CollisionDistance = Mathf.Min(m_CollisionDistance, full);
            return pivot + dir * m_CollisionDistance;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }
    }
}
