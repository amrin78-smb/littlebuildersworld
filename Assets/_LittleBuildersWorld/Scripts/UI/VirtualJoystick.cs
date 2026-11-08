using LittleBuildersWorld.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// Optional steering joystick (Settings ▸ Controls). Forgiving: the knob follows the first touch anywhere in its
    /// big area, has a generous dead zone, and snaps home when released.
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform m_Base;
        [SerializeField] private RectTransform m_Knob;
        [SerializeField] private float m_Radius = 150f;
        [SerializeField] [Range(0f, 0.5f)] private float m_DeadZone = 0.18f;

        private int m_PointerId = int.MinValue;
        private Vector2 m_Origin;
        private float m_GraceUntil;

        public Vector2 Value { get; private set; }
        public bool IsHeld => m_PointerId != int.MinValue;

        public void OnPointerDown(PointerEventData e)
        {
            if (IsHeld) return;
            m_PointerId = e.pointerId;
            m_GraceUntil = Time.unscaledTime + 0.1f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out m_Origin);
            if (m_Base != null) m_Base.anchoredPosition = m_Origin;
            UpdateValue(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == m_PointerId) UpdateValue(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == m_PointerId) Release();
        }

        private void UpdateValue(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out Vector2 local);
            Vector2 delta = Vector2.ClampMagnitude(local - m_Origin, m_Radius);
            if (m_Knob != null) m_Knob.anchoredPosition = m_Origin + delta;
            Vector2 v = delta / m_Radius;
            float mag = v.magnitude;
            Value = mag < m_DeadZone ? Vector2.zero : v.normalized * Mathf.InverseLerp(m_DeadZone, 1f, mag);
        }

        public void Release()
        {
            m_PointerId = int.MinValue;
            Value = Vector2.zero;
            if (m_Knob != null && m_Base != null) m_Knob.anchoredPosition = m_Base.anchoredPosition;
        }

        private void Update()
        {
            if (IsHeld && Time.unscaledTime > m_GraceUntil && !PointerWatchdog.AnyPointerDown()) Release();
        }

        private void OnDisable() => Release();

        private void OnApplicationPause(bool paused)
        {
            if (paused) Release();
        }
    }
}
