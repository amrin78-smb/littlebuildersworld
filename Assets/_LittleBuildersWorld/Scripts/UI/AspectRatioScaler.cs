using UnityEngine;
using UnityEngine.UI;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// The UI is laid out at 1920×1080. On screens squarer than 16:9 (16:10 and 4:3 tablets) scaling by height would
    /// cut the sides off, so match width there; on 16:9 and wider phones match height.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class AspectRatioScaler : MonoBehaviour
    {
        private const float Reference = 16f / 9f;

        private CanvasScaler m_Scaler;
        private int m_W, m_H;

        private void Awake()
        {
            m_Scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            if (Screen.width != m_W || Screen.height != m_H) Apply();
        }

        private void Apply()
        {
            m_W = Screen.width;
            m_H = Screen.height;
            float aspect = m_H > 0 ? (float)m_W / m_H : Reference;
            m_Scaler.matchWidthOrHeight = aspect < Reference - 0.01f ? 0f : 1f;
        }
    }
}
