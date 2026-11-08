using LittleBuildersWorld.Core;
using LittleBuildersWorld.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// The row of big vehicle pictures shown when not driving. Tap one to drive it; if it is far away it is brought
    /// back to its parking bay first. Locked vehicles show a lock and how many stars they need.
    /// </summary>
    public sealed class VehicleSelectorUI : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Slot
        {
            public Vehicle vehicle;
            public TapButton button;
            public Image portrait;
            public GameObject lockBadge;
            public TextMeshProUGUI starsNeeded;
            public GameObject selectedRing;
        }

        [SerializeField] private VehicleInteraction m_Interaction;
        [SerializeField] private Slot[] m_Slots = new Slot[0];

        private void Awake()
        {
            foreach (Slot s in m_Slots)
            {
                if (s.button == null) continue;
                Slot captured = s;
                s.button.Pressed += () => OnSlot(captured);
            }
        }

        private void OnEnable()
        {
            Refresh();
            if (GameServices.Instance != null) GameServices.Instance.Stars.TotalChanged += Refresh;
        }

        private void OnDisable()
        {
            if (GameServices.Instance != null) GameServices.Instance.Stars.TotalChanged -= Refresh;
        }

        public void Refresh()
        {
            GameServices s = GameServices.Instance;
            foreach (Slot slot in m_Slots)
            {
                if (slot.vehicle == null) continue;
                VehicleProfile p = slot.vehicle.Profile;
                bool available = s == null || s.Progression.IsVehicleAvailable(p);
                if (slot.portrait != null && p != null) slot.portrait.sprite = p.portrait;
                if (slot.portrait != null) slot.portrait.color = available ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                if (slot.lockBadge != null) slot.lockBadge.SetActive(!available);
                if (slot.starsNeeded != null && p != null) slot.starsNeeded.text = p.starsRequired.ToString();
                if (slot.selectedRing != null) slot.selectedRing.SetActive(s != null && s.Progression.SelectedVehicle == slot.vehicle.Id);
            }
        }

        private void OnSlot(Slot slot)
        {
            if (slot.vehicle == null) return;
            if (!m_Interaction.Select(slot.vehicle)) Tween.Squash(slot.button.transform, Vector3.one, 0.3f, 0.4f);
            Refresh();
        }
    }
}
