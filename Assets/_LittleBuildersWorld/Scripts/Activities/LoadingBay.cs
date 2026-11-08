using System.Collections;
using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Vehicles;
using LittleBuildersWorld.World;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>A hopper/pallet stop where a dump truck is filled with one tap of Load. Always available, never runs out.</summary>
    public sealed class LoadingBay : MonoBehaviour, IActionProvider, IGuidanceTarget
    {
        [SerializeField] private ActivityZone m_Zone;
        [SerializeField] private LoadKind m_Kind = LoadKind.Bricks;
        [Tooltip("Visual that drops into the bed (a pallet or chute flap).")] [SerializeField] private Transform m_DropVisual;

        private bool m_Busy;
        private Vector3 m_DropRest;

        private void Awake()
        {
            if (m_DropVisual != null) m_DropRest = m_DropVisual.position;
        }

        private void OnEnable()
        {
            ActionProviderRegistry.Register(this);
            GuidanceRegistry.Register(this);
        }

        private void OnDisable()
        {
            ActionProviderRegistry.Unregister(this);
            GuidanceRegistry.Unregister(this);
        }

        public bool TryGetOffer(Vehicle vehicle, out ActionOffer offer)
        {
            offer = default;
            if (m_Busy || vehicle.Type != VehicleType.DumpTruck || m_Zone == null || !m_Zone.Contains(vehicle.transform.position)) return false;
            var bed = vehicle.GetTool<DumpTruckBed>();
            if (bed == null || !bed.CanReceive) return false;
            offer = new ActionOffer { Icon = ActionIcon.Load, Priority = 6, Provider = this };
            return true;
        }

        public void Execute(Vehicle vehicle, int code)
        {
            var bed = vehicle.GetTool<DumpTruckBed>();
            if (bed != null && !m_Busy) StartCoroutine(LoadRoutine(bed));
        }

        private IEnumerator LoadRoutine(DumpTruckBed bed)
        {
            m_Busy = true;
            GameServices.Instance?.Audio.Play(SfxId.Whoosh);
            if (m_DropVisual != null)
            {
                Vector3 target = bed.BedCentre + Vector3.up * 0.3f;
                Tween.Jump(m_DropVisual, target, 2f, 0.55f);
                yield return new WaitForSeconds(0.55f);
            }
            bed.AddLoad(m_Kind, DumpTruckBed.Capacity);
            if (m_DropVisual != null)
            {
                m_DropVisual.position = m_DropRest;
                m_DropVisual.localScale = Vector3.zero;
                Tween.Scale(m_DropVisual, Vector3.one, 0.4f, Ease.OutBack, null, 0.6f);
            }
            yield return new WaitForSeconds(0.3f);
            m_Busy = false;
        }

        /// <summary>Points an empty truck here when some delivery is waiting.</summary>
        public bool TryGetGuidance(Vehicle vehicle, out Vector3 position, out int priority)
        {
            position = transform.position;
            priority = 5;
            if (vehicle.Type != VehicleType.DumpTruck) return false;
            var bed = vehicle.GetTool<DumpTruckBed>();
            if (bed == null || bed.IsLoaded) return false;
            foreach (ActivityBase a in ActivityBase.All)
            {
                if (a is MaterialDeliveryActivity && a.IsOpen) return true;
            }
            return false;
        }
    }
}
