using System;
using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// Toddler-friendly button: fires the moment a finger touches it (no need to lift cleanly on the same spot),
    /// with a squishy bounce and a soft click. A short cooldown stops accidental double-taps.
    /// </summary>
    public sealed class TapButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Graphic m_Background;
        [SerializeField] private float m_Cooldown = 0.35f;
        [SerializeField] private bool m_PlaySound = true;

        private float m_ReadyAt;
        private Vector3 m_RestScale = Vector3.one;
        private Color m_Normal;

        public event Action Pressed;
        public bool Interactable { get; set; } = true;

        private void Awake()
        {
            m_RestScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            if (m_Background == null) m_Background = GetComponent<Graphic>();
            if (m_Background != null) m_Normal = m_Background.color;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactable || Time.unscaledTime < m_ReadyAt) return;
            m_ReadyAt = Time.unscaledTime + m_Cooldown;
            if (m_Background != null) m_Background.color = m_Normal * 0.85f;
            transform.localScale = m_RestScale * 0.88f;
            if (m_PlaySound) GameServices.Instance?.Audio.Play(SfxId.ButtonTap);
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (m_Background != null) m_Background.color = m_Normal;
            if (this != null && isActiveAndEnabled) Tween.Squash(transform, m_RestScale, 0.18f, 0.3f);
            else transform.localScale = m_RestScale;
        }

        /// <summary>For keyboard/gamepad and automated tests.</summary>
        public void SimulatePress()
        {
            if (!Interactable) return;
            Pressed?.Invoke();
        }

        private void OnDisable()
        {
            transform.localScale = m_RestScale;
            if (m_Background != null) m_Background.color = m_Normal;
        }
    }
}
