using LittleBuildersWorld.Vehicles;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>Dump truck load-and-deliver: tip N loads at the delivery zone. Each delivery leaves a visible heap.</summary>
    public sealed class MaterialDeliveryActivity : ActivityBase
    {
        [Tooltip("One heap per delivery, shown as loads arrive (optional).")] [SerializeField] private GameObject[] m_DeliveredHeaps = new GameObject[0];
        [SerializeField] private GameObject m_OpenVisual;

        internal void RegisterDelivery(LoadKind kind, Vector3 at)
        {
            if (IsOpen) AddProgress(1, at);
        }

        protected override void ApplyState(bool instant)
        {
            int shown = Progress.IsComplete ? 0 : Progress.Progress; // heaps are used up once the stage is built
            for (int i = 0; i < m_DeliveredHeaps.Length; i++)
            {
                if (m_DeliveredHeaps[i] != null) m_DeliveredHeaps[i].SetActive(i < shown);
            }
        }

        protected override void OnProgressChanged(int finishedStage) => ApplyState(false);

        protected override void OnOpenChanged(bool open)
        {
            if (m_OpenVisual != null) m_OpenVisual.SetActive(open);
        }

        public override bool TryGetGuidance(Vehicle vehicle, out Vector3 position, out int priority)
        {
            bool ok = base.TryGetGuidance(vehicle, out position, out priority);
            var bed = vehicle.GetTool<DumpTruckBed>();
            priority = 10;
            return ok && bed != null && bed.IsLoaded; // empty trucks are guided to a loading bay instead
        }
    }
}
