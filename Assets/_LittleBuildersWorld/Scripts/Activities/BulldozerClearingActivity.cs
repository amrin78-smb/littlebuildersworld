using LittleBuildersWorld.Core;
using LittleBuildersWorld.Audio;
using LittleBuildersWorld.World;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>
    /// Bulldozer push-and-clear: push each rock/log into the marked area. Debris that leaves the activity area is
    /// gently returned to its start so it can never be lost or stuck. Progress = debris cleared.
    /// </summary>
    public sealed class BulldozerClearingActivity : ActivityBase
    {
        [SerializeField] private PushableDebris[] m_Debris = new PushableDebris[0];
        [SerializeField] private ActivityZone m_Target;
        [Tooltip("Debris further than this from the activity centre goes home.")] [SerializeField] private float m_BoundsRadius = 30f;
        [SerializeField] private Transform[] m_ClearSlots = new Transform[0];
        [SerializeField] private GameObject m_OpenVisual;
        [Tooltip("Inside of the fence (XZ box). Debris outside it pops home. Zero size = use the bounds radius.")] [SerializeField] private Vector3 m_AreaCentre;
        [SerializeField] private Vector2 m_AreaHalfSize;

        private int m_Cleared;

        protected override void Awake()
        {
            m_StageGoals = new[] { Mathf.Max(1, m_Debris.Length) };
            base.Awake();
        }

        private void FixedUpdate()
        {
            if (Progress == null || m_Target == null) return;
            bool open = IsOpen;
            for (int i = 0; i < m_Debris.Length; i++)
            {
                PushableDebris d = m_Debris[i];
                if (d == null || d.IsCleared) continue;
                Vector3 p = d.transform.position;
                if (open && m_Target.Contains(p))
                {
                    d.Clear(Slot(m_Cleared), false);
                    m_Cleared++;
                    GameServices.Instance?.Audio.Play(SfxId.Thud);
                    AddProgress(1, p);
                }
                else if (OutOfArea(p) || Wedged(d, p))
                {
                    d.ResetHome(true);
                    GameServices.Instance?.Audio.Play(SfxId.Pop);
                }
            }
        }

        /// <summary>Outside the fenced play area (or fallen out of the world): unreachable, so it goes home.</summary>
        public bool OutOfArea(Vector3 p)
        {
            if (p.y < -2f) return true;
            if (m_AreaHalfSize.x > 0f && m_AreaHalfSize.y > 0f)
                return Mathf.Abs(p.x - m_AreaCentre.x) > m_AreaHalfSize.x || Mathf.Abs(p.z - m_AreaCentre.z) > m_AreaHalfSize.y;
            return (p - FocusPosition).sqrMagnitude > m_BoundsRadius * m_BoundsRadius;
        }

        /// <summary>
        /// Resting against the fence on the far side from the stripes: there is no room for the bulldozer to get behind
        /// it, so it could never be pushed in. After a short rest it goes home (where it can be pushed) instead.
        /// </summary>
        public bool Wedged(PushableDebris d, Vector3 p)
        {
            if (m_AreaHalfSize.x <= 0f || m_AreaHalfSize.y <= 0f || Time.time - d.LastPushedTime < WedgeRestSeconds) return false;
            Vector3 toTarget = m_Target.transform.position - p;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return false;
            Vector3 behind = p - toTarget.normalized * WedgeRoom;
            return OutOfArea(behind) && !OutOfArea(d.HomePosition - toTarget.normalized * WedgeRoom);
        }

        /// <summary>Room the bulldozer needs behind a piece to push it (blade to tail is about 4 m; 3.5 m to line up).</summary>
        public const float WedgeRoom = 3.5f;
        public const float WedgeRestSeconds = 4f;

        /// <summary>The arrow leads to the nearest piece still to push (to the stripes while one is being pushed).</summary>
        public override bool TryGetGuidance(Vehicles.Vehicle vehicle, out Vector3 position, out int priority)
        {
            if (!base.TryGetGuidance(vehicle, out position, out priority)) return false;
            Vector3 from = vehicle.transform.position;
            float best = float.MaxValue;
            for (int i = 0; i < m_Debris.Length; i++)
            {
                PushableDebris d = m_Debris[i];
                if (d == null || d.IsCleared) continue;
                Vector3 p = d.transform.position;
                float sq = (p.x - from.x) * (p.x - from.x) + (p.z - from.z) * (p.z - from.z);
                if (Time.time - d.LastPushedTime < 0.5f && sq < 36f && m_Target != null)
                {
                    position = m_Target.transform.position;
                    return true;
                }
                if (sq < best) { best = sq; position = p; }
            }
            return true;
        }

        private Vector3 Slot(int index)
        {
            if (m_ClearSlots.Length > 0) return m_ClearSlots[index % m_ClearSlots.Length].position;
            return m_Target.transform.position + new Vector3((index % 3 - 1) * 1.2f, 0.3f, (index / 3) * 1.2f);
        }

        protected override void ApplyState(bool instant)
        {
            int done = Progress.IsComplete ? m_Debris.Length : Progress.Progress;
            m_Cleared = 0;
            for (int i = 0; i < m_Debris.Length; i++)
            {
                if (m_Debris[i] == null) continue;
                if (i < done)
                {
                    m_Debris[i].Clear(Slot(m_Cleared), true);
                    m_Cleared++;
                }
                else m_Debris[i].ResetHome(false);
            }
        }

        protected override void OnOpenChanged(bool open)
        {
            if (m_OpenVisual != null) m_OpenVisual.SetActive(open);
        }
    }
}
