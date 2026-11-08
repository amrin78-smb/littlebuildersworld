using LittleBuildersWorld.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// A big press-and-hold control (steer, go, back). Held while its finger stays down, even if the finger wanders
    /// a little off the button — small hands slide. Released on finger up, cancel, disable, app pause, or when the
    /// watchdog sees no pointer down at all.
    /// </summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Graphic m_Background;
        [SerializeField] private Color m_Normal = Color.white;
        [SerializeField] private Color m_Pressed = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private float m_PressedScale = 0.9f;

        private int m_PointerId = int.MinValue;
        private float m_GraceUntil;
        private Vector3 m_RestScale = Vector3.one;

        public bool IsHeld { get; private set; }

        private void Awake()
        {
            m_RestScale = transform.localScale;
            if (m_Background == null) m_Background = GetComponent<Graphic>();
            Paint();
        }

        public void OnPointerDown(PointerEventData e)
        {
            m_PointerId = e.pointerId;
            IsHeld = true;
            m_GraceUntil = Time.unscaledTime + 0.1f; // the device state can lag the UI event by a frame
            Paint();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == m_PointerId) Release();
        }

        public void Release()
        {
            IsHeld = false;
            m_PointerId = int.MinValue;
            Paint();
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

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Release();
        }

        private void Paint()
        {
            if (m_Background != null) m_Background.color = IsHeld ? m_Pressed : m_Normal;
            transform.localScale = IsHeld ? m_RestScale * m_PressedScale : m_RestScale;
        }
    }
}
