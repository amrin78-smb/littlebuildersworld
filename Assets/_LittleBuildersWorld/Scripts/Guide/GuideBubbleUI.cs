using System;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBuildersWorld.Guide
{
    /// <summary>
    /// The friendly foreman in the corner with a speech bubble: short words (for grown-ups and early readers) plus a
    /// picture (for everyone). The bubble pops up with each new line and tucks away after a few seconds; the foreman
    /// stays, and tapping him says the line again.
    /// </summary>
    public sealed class GuideBubbleUI : MonoBehaviour
    {
        [SerializeField] private TapButton m_Mascot;
        [SerializeField] private RectTransform m_MascotFace;
        [SerializeField] private RectTransform m_Bubble;
        [SerializeField] private TextMeshProUGUI m_Text;
        [SerializeField] private GameObject m_IconRoot;
        [SerializeField] private Image m_IconDisc;
        [SerializeField] private Image m_Icon;
        [SerializeField] private float m_MinShowSeconds = 6f;

        private float m_HideAt;
        private bool m_Expanded;
        private bool m_Talking;
        private float m_TalkPhase;

        public event Action MascotTapped;
        public bool IsExpanded => m_Expanded;
        public string ShownText => m_Text != null ? m_Text.text : "";

        private void Awake()
        {
            if (m_Mascot != null) m_Mascot.Pressed += OnMascot;
            if (m_Bubble != null)
            {
                m_Bubble.localScale = Vector3.zero;
                m_Bubble.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (m_Mascot != null) m_Mascot.Pressed -= OnMascot;
        }

        private void OnMascot() => MascotTapped?.Invoke();

        /// <summary>Shows a line. icon null = words only. seconds = how long the bubble stays open (at least a few).</summary>
        public void Show(string text, Sprite icon, Color iconDisc, float seconds)
        {
            if (m_Text != null) m_Text.text = text;
            bool hasIcon = icon != null;
            if (m_IconRoot != null) m_IconRoot.SetActive(hasIcon);
            if (hasIcon)
            {
                m_Icon.sprite = icon;
                m_Icon.preserveAspect = true;
                m_IconDisc.color = iconDisc;
            }
            if (m_Text != null)
            {
                // Words take the room the picture does not need.
                RectTransform rt = m_Text.rectTransform;
                rt.offsetMax = new Vector2(hasIcon ? -170f : -36f, rt.offsetMax.y);
            }
            m_HideAt = Time.unscaledTime + Mathf.Max(m_MinShowSeconds, seconds);
            if (m_Bubble == null) return;
            m_Bubble.gameObject.SetActive(true);
            Tween.Kill(m_Bubble);
            m_Bubble.localScale = Vector3.one * (m_Expanded ? 0.9f : 0.3f);
            Tween.Scale(m_Bubble, Vector3.one, 0.35f, Ease.OutBack);
            m_Expanded = true;
        }

        public void Collapse()
        {
            if (!m_Expanded || m_Bubble == null) return;
            m_Expanded = false;
            Tween.Kill(m_Bubble);
            Tween.Scale(m_Bubble, Vector3.zero, 0.25f, Ease.InBack, () => m_Bubble.gameObject.SetActive(false));
        }

        /// <summary>The foreman bobs while the voice is speaking.</summary>
        public void SetTalking(bool talking) => m_Talking = talking;

        private void Update()
        {
            if (m_Expanded && Time.unscaledTime >= m_HideAt) Collapse();
            if (m_MascotFace == null) return;
            if (m_Talking)
            {
                m_TalkPhase += Time.unscaledDeltaTime * 14f;
                float s = 1f + Mathf.Abs(Mathf.Sin(m_TalkPhase)) * 0.06f;
                m_MascotFace.localScale = new Vector3(1f / Mathf.Sqrt(s), s, 1f);
            }
            else if (m_MascotFace.localScale != Vector3.one)
            {
                m_MascotFace.localScale = Vector3.MoveTowards(m_MascotFace.localScale, Vector3.one, Time.unscaledDeltaTime * 2f);
            }
        }
    }
}
