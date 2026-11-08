using LittleBuildersWorld.Vehicles;
using LittleBuildersWorld.World;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>Where a loaded truck tips its load for a project. Accepts any kind of load.</summary>
    public sealed class DeliveryZone : MonoBehaviour, IActionProvider
    {
        [SerializeField] private ActivityZone m_Zone;
        [SerializeField] private MaterialDeliveryActivity m_Activity;
        [SerializeField] private Transform m_DropPoint;

        public Vector3 DropPoint => m_DropPoint != null ? m_DropPoint.position : transform.position;

        private void OnEnable() => ActionProviderRegistry.Register(this);
        private void OnDisable() => ActionProviderRegistry.Unregister(this);

        public bool TryGetOffer(Vehicle vehicle, out ActionOffer offer)
        {
            offer = default;
            if (vehicle.Type != VehicleType.DumpTruck || m_Activity == null || !m_Activity.IsOpen) return false;
            var bed = vehicle.GetTool<DumpTruckBed>();
            if (bed == null || bed.IsBusy || !bed.IsLoaded || m_Zone == null || !m_Zone.Contains(vehicle.transform.position)) return false;
            offer = new ActionOffer { Icon = ActionIcon.Unload, Priority = 10, Provider = this };
            return true;
        }

        public void Execute(Vehicle vehicle, int code)
        {
            var bed = vehicle.GetTool<DumpTruckBed>();
            if (bed == null) return;
            MaterialDeliveryActivity activity = m_Activity;
            Vector3 at = DropPoint;
            bed.TipInto(at, (kind, amount) => activity.RegisterDelivery(kind, at));
        }
    }
}
