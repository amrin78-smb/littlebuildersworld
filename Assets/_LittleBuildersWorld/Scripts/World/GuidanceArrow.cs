using LittleBuildersWorld.Activities;
using LittleBuildersWorld.Vehicles;
using UnityEngine;

namespace LittleBuildersWorld.World
{
    /// <summary>
    /// A big bouncing arrow beside the driven vehicle pointing to the most useful next place for that vehicle
    /// (a project stage, a loading bay...). Hidden when close, or when there is nothing to suggest. A hint, never a rule.
    /// </summary>
    public sealed class GuidanceArrow : MonoBehaviour
    {
        [SerializeField] private VehicleInteraction m_Interaction;
        [SerializeField] private Transform m_Arrow;
        [SerializeField] private float m_Orbit = 4.2f;
        [SerializeField] private float m_Height = 3.2f;
        [SerializeField] private float m_HideWithin = 9f;
        [SerializeField] private float m_RefreshSeconds = 0.25f;

        private Vector3 m_Target;
        private bool m_HasTarget;
        private float m_NextRefresh;
        private float m_Phase;
        private float m_Visible;
        private Vector3 m_RestScale;

        private void Awake()
        {
            if (m_Arrow != null) m_RestScale = m_Arrow.localScale;
        }

        private void LateUpdate()
        {
            if (m_Arrow == null) return;
            Vehicle v = m_Interaction != null ? m_Interaction.Current : null;
            if (v != null && Time.time >= m_NextRefresh)
            {
                m_NextRefresh = Time.time + m_RefreshSeconds;
                m_HasTarget = GuidanceRegistry.TryFind(v, out m_Target);
            }
            bool show = v != null && m_HasTarget;
            Vector3 toTarget = Vector3.zero;
            if (show)
            {
                toTarget = m_Target - v.transform.position;
                toTarget.y = 0f;
                show = toTarget.magnitude > m_HideWithin && (v.Tool == null || !v.Tool.IsBusy);
            }
            m_Visible = Mathf.MoveTowards(m_Visible, show ? 1f : 0f, Time.deltaTime * 3f);
            bool active = m_Visible > 0.01f;
            if (m_Arrow.gameObject.activeSelf != active) m_Arrow.gameObject.SetActive(active);
            if (!active || v == null) return;

            if (toTarget.sqrMagnitude < 0.01f) return;
            Vector3 dir = toTarget.normalized;
            m_Phase += Time.deltaTime * 4f;
            Vector3 pos = v.transform.position + dir * m_Orbit + Vector3.up * (m_Height + Mathf.Sin(m_Phase) * 0.25f);
            m_Arrow.position = Vector3.Lerp(m_Arrow.position, pos, 1f - Mathf.Exp(-10f * Time.deltaTime));
            m_Arrow.rotation = Quaternion.Slerp(m_Arrow.rotation, Quaternion.LookRotation(dir, Vector3.up), 1f - Mathf.Exp(-8f * Time.deltaTime));
            m_Arrow.localScale = m_RestScale * Mathf.SmoothStep(0f, 1f, m_Visible);
        }
    }
}
