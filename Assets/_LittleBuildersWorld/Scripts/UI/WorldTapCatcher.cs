using UnityEngine;
using UnityEngine.EventSystems;

namespace LittleBuildersWorld.UI
{
    /// <summary>Full-screen invisible UI layer behind the HUD: a tap on empty screen (not on a button) can enter a vehicle.</summary>
    public sealed class WorldTapCatcher : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private DriveBubbles m_Bubbles;

        public void OnPointerDown(PointerEventData e)
        {
            if (m_Bubbles != null) m_Bubbles.TryTapVehicle(e.position);
        }
    }
}
