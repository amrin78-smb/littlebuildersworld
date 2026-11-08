using UnityEngine;

namespace LittleBuildersWorld.UI
{
    /// <summary>Keeps this RectTransform inside the screen's safe area (notches, rounded corners, gesture bars).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect m_Last;
        private Vector2Int m_Screen;

        private void Awake() => Apply();

        private void Update()
        {
            if (Screen.safeArea != m_Last || m_Screen.x != Screen.width || m_Screen.y != Screen.height) Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            m_Last = safe;
            m_Screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
