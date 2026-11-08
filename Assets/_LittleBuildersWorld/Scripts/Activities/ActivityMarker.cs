using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>
    /// Big floating picture of the vehicle an activity needs, bobbing above it, plus a pulsing ring on the ground.
    /// Faces the camera. Shape and motion carry the meaning, not colour alone.
    /// </summary>
    public sealed class ActivityMarker : MonoBehaviour
    {
        [SerializeField] private Transform m_Icon;
        [SerializeField] private Transform m_Ring;
        [SerializeField] private float m_BobHeight = 0.5f;
        [SerializeField] private float m_BobSpeed = 2.2f;

        private Vector3 m_IconRest;
        private Vector3 m_RingScale;
        private float m_Phase;
        private Camera m_Camera;

        private void Awake()
        {
            if (m_Icon != null) m_IconRest = m_Icon.localPosition;
            if (m_Ring != null) m_RingScale = m_Ring.localScale;
            m_Phase = Random.value * 6f;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            m_Phase += Time.deltaTime * m_BobSpeed;
            if (m_Icon != null)
            {
                m_Icon.localPosition = m_IconRest + Vector3.up * (Mathf.Sin(m_Phase) * m_BobHeight);
                if (m_Camera == null) m_Camera = Camera.main;
                if (m_Camera != null)
                {
                    Vector3 toCam = m_Camera.transform.position - m_Icon.position;
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude > 0.01f) m_Icon.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                }
            }
            if (m_Ring != null)
            {
                float pulse = 1f + Mathf.Sin(m_Phase * 1.5f) * 0.08f;
                m_Ring.localScale = m_RingScale * pulse;
            }
        }
    }
}
