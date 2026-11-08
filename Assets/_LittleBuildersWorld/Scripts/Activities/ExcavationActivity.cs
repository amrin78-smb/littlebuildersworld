using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>Excavator dig-and-scoop: move N scoops of soil from the pile (into a truck or onto the stockpile).</summary>
    public sealed class ExcavationActivity : ActivityBase
    {
        [SerializeField] private DigZone m_DigZone;
        [Tooltip("Shown while this stage is the open one (e.g. the marked foundation outline).")] [SerializeField] private GameObject m_OpenVisual;

        internal void RegisterSoilMoved(Vector3 at)
        {
            if (IsOpen) AddProgress(1, at);
        }

        protected override void ApplyState(bool instant)
        {
            if (m_DigZone != null) m_DigZone.ShowProgress(Progress.IsComplete ? 1f : Progress.StageFraction, instant);
        }

        protected override void OnProgressChanged(int finishedStage) => ApplyState(false);

        protected override void OnOpenChanged(bool open)
        {
            if (m_OpenVisual != null) m_OpenVisual.SetActive(open);
        }

        public override bool TryGetGuidance(Vehicles.Vehicle vehicle, out Vector3 position, out int priority)
        {
            bool ok = base.TryGetGuidance(vehicle, out position, out priority);
            var arm = vehicle.GetTool<Vehicles.ExcavatorArm>();
            if (ok && arm != null && arm.HasSoil) return false; // full bucket: dump anywhere, no arrow needed
            priority = Parent != null ? 10 : 5; // projects first, the yard pit is always there for free play
            return ok;
        }
    }
}
