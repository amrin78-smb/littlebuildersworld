using System.Collections.Generic;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// While not driving, a big round "Drive" bubble with the vehicle's picture floats over every nearby vehicle.
    /// Tap it (or the vehicle itself) to climb in. Bubbles are pooled screen-space buttons.
    /// </summary>
    public sealed class DriveBubbles : MonoBehaviour
    {
        [SerializeField] private VehicleInteraction m_Interaction;
        [SerializeField] private Camera m_Camera;
        [SerializeField] private RectTransform m_Container;
        [SerializeField] private TapButton m_BubblePrefab;
        [SerializeField] private float m_TapVehicleRadius = 140f;

        private readonly List<TapButton> m_Bubbles = new List<TapButton>();
        private readonly List<Vehicle> m_Bound = new List<Vehicle>();
        private readonly List<Image> m_Portraits = new List<Image>();

        private void LateUpdate()
        {
            bool show = !m_Interaction.IsDriving && !(GameServices.Instance != null && GameServices.Instance.State.IsPaused);
            int used = 0;
            if (show && m_Camera != null)
            {
                foreach (Vehicle v in Vehicle.All)
                {
                    if (!m_Interaction.IsNearFocus(v) || !m_Interaction.CanEnter(v)) continue;
                    Vector3 sp = m_Camera.WorldToScreenPoint(v.BubblePosition);
                    if (sp.z <= 0.5f) continue;
                    TapButton b = Get(used);
                    m_Bound[used] = v;
                    if (m_Portraits[used] != null && v.Profile != null) m_Portraits[used].sprite = v.Profile.portrait;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Container, sp, null, out Vector2 local);
                    var rt = (RectTransform)b.transform.parent;
                    rt.anchoredPosition = local + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 2.5f + used) * 10f);
                    if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);
                    used++;
                }
            }
            for (int i = used; i < m_Bubbles.Count; i++)
            {
                GameObject root = m_Bubbles[i].transform.parent.gameObject;
                if (root.activeSelf) root.SetActive(false);
                m_Bound[i] = null;
            }
        }

        private TapButton Get(int index)
        {
            while (m_Bubbles.Count <= index)
            {
                TapButton b = Instantiate(m_BubblePrefab.transform.parent.gameObject, m_Container).GetComponentInChildren<TapButton>(true);
                int captured = m_Bubbles.Count;
                b.Pressed += () => OnBubble(captured);
                m_Bubbles.Add(b);
                m_Bound.Add(null);
                Transform portrait = b.transform.Find("Portrait");
                m_Portraits.Add(portrait != null ? portrait.GetComponent<Image>() : null);
            }
            return m_Bubbles[index];
        }

        private void OnBubble(int index)
        {
            if (index < m_Bound.Count && m_Bound[index] != null) m_Interaction.Enter(m_Bound[index]);
        }

        /// <summary>Tapping a vehicle in the world (not a bubble) also enters it. Called by the HUD's world-tap catcher.</summary>
        public bool TryTapVehicle(Vector2 screen)
        {
            if (m_Interaction.IsDriving || m_Camera == null) return false;
            Vehicle best = null;
            float bestSq = m_TapVehicleRadius * m_TapVehicleRadius;
            foreach (Vehicle v in Vehicle.All)
            {
                if (!m_Interaction.IsNearFocus(v) || !m_Interaction.CanEnter(v)) continue;
                Vector3 sp = m_Camera.WorldToScreenPoint(v.transform.position + Vector3.up * 1.5f);
                if (sp.z <= 0f) continue;
                float sq = ((Vector2)sp - screen).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = v; }
            }
            return best != null && m_Interaction.Enter(best);
        }
    }
}
