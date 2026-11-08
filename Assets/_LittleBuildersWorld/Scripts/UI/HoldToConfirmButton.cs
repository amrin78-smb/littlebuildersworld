using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// Adult gate for the pause/settings button: must be held for a moment while a ring fills. A quick toddler tap
    /// does nothing, so play is never interrupted by accident.
    /// </summary>
    public sealed class HoldToConfirmButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image m_Fill;
        [SerializeField] private float m_HoldSeconds = 1.5f;

        private bool m_Holding;
        private float m_Held;

        public event Action Confirmed;

        private void Awake() => SetFill(0f);

        public void OnPointerDown(PointerEventData e)
        {
            m_Holding = true;
            m_Held = 0f;
        }

        public void OnPointerUp(PointerEventData e) => Cancel();
        public void OnPointerExit(PointerEventData e) => Cancel();

        private void Cancel()
        {
            m_Holding = false;
            m_Held = 0f;
            SetFill(0f);
        }

        private void Update()
        {
            if (!m_Holding) return;
            m_Held += Time.unscaledDeltaTime;
            SetFill(m_Held / m_HoldSeconds);
            if (m_Held >= m_HoldSeconds)
            {
                Cancel();
                Confirmed?.Invoke();
            }
        }

        private void OnDisable() => Cancel();

        public void SimulateConfirm() => Confirmed?.Invoke();

        private void SetFill(float f)
        {
            if (m_Fill != null) m_Fill.fillAmount = Mathf.Clamp01(f);
        }
    }
}
