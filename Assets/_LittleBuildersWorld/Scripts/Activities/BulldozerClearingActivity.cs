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
                else if ((p - FocusPosition).sqrMagnitude > m_BoundsRadius * m_BoundsRadius || p.y < -2f)
                {
                    d.ResetHome(true);
                    GameServices.Instance?.Audio.Play(SfxId.Pop);
                }
            }
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
