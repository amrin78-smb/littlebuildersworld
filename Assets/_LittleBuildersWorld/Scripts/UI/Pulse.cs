using UnityEngine;

namespace LittleBuildersWorld.UI
{
    /// <summary>Gentle "press me" breathing for the action button and other invitations.</summary>
    public sealed class Pulse : MonoBehaviour
    {
        [SerializeField] private float m_Amount = 0.07f;
        [SerializeField] private float m_Speed = 4f;

        private Vector3 m_Rest = Vector3.one;
        private float m_Phase;

        public bool Active { get; set; } = true;

        private void Awake() => m_Rest = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

        private void OnEnable() => m_Phase = 0f;

        private void Update()
        {
            if (!Active) return;
            m_Phase += Time.unscaledDeltaTime * m_Speed;
            transform.localScale = m_Rest * (1f + Mathf.Sin(m_Phase) * m_Amount);
        }

        private void OnDisable() => transform.localScale = m_Rest;
    }
}
