using LittleBuildersWorld.Core;
using LittleBuildersWorld.Vehicles;
using LittleBuildersWorld.World;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>
    /// A soil pile the excavator can scoop from when parked anywhere inside the (generous) zone. The pile shrinks a
    /// little per scoop and the stockpile beside it grows when soil is dumped there.
    /// </summary>
    public sealed class DigZone : MonoBehaviour, IActionProvider, ExcavatorArm.ISoilSource
    {
        [SerializeField] private ActivityZone m_Zone;
        [SerializeField] private ExcavationActivity m_Activity;
        [SerializeField] private Transform m_Pile;
        [SerializeField] private Transform m_Stockpile;
        [SerializeField] private float m_MinPileScale = 0.45f;

        private Vector3 m_PileRest;
        private Vector3 m_StockRest;

        public Vector3 DigPoint => m_Pile != null ? m_Pile.position : transform.position;
        public Vector3 StockpilePoint => m_Stockpile != null ? m_Stockpile.position : transform.position;
        public bool HasStockpile => m_Stockpile != null;
        public ActivityZone Zone => m_Zone;

        private void Awake()
        {
            if (m_Pile != null) m_PileRest = m_Pile.localScale;
            if (m_Stockpile != null) m_StockRest = m_Stockpile.localScale;
        }

        private void OnEnable() => ActionProviderRegistry.Register(this);
        private void OnDisable() => ActionProviderRegistry.Unregister(this);

        public bool TryGetOffer(Vehicle vehicle, out ActionOffer offer)
        {
            offer = default;
            if (vehicle.Type != VehicleType.Excavator || m_Activity == null || !m_Activity.IsOpen) return false;
            var arm = vehicle.GetTool<ExcavatorArm>();
            if (arm == null || !arm.CanScoop || m_Zone == null || !m_Zone.Contains(vehicle.transform.position)) return false;
            offer = new ActionOffer { Icon = ActionIcon.Scoop, Priority = 10, Provider = this };
            return true;
        }

        public void Execute(Vehicle vehicle, int code) => vehicle.GetTool<ExcavatorArm>()?.Scoop(this);

        public void OnScooped()
        {
            if (m_Pile != null) Tween.Squash(m_Pile, m_Pile.localScale, 0.15f, 0.3f);
        }

        public void OnSoilMoved(bool intoTruck)
        {
            if (m_Activity != null) m_Activity.RegisterSoilMoved(intoTruck ? DigPoint : StockpilePoint);
        }

        /// <summary>Shows progress: the pile shrinks and the stockpile grows with each soil move (fraction 0..1).</summary>
        public void ShowProgress(float fraction, bool instant)
        {
            fraction = Mathf.Clamp01(fraction);
            if (m_Pile != null)
            {
                Vector3 s = m_PileRest * Mathf.Lerp(1f, m_MinPileScale, fraction);
                if (instant) m_Pile.localScale = s;
                else Tween.Scale(m_Pile, s, 0.4f, Ease.OutBack);
            }
            if (m_Stockpile != null)
            {
                Vector3 s = m_StockRest * Mathf.Lerp(0.35f, 1f, fraction);
                if (instant) m_Stockpile.localScale = s;
                else Tween.Scale(m_Stockpile, s, 0.4f, Ease.OutBack);
            }
        }
    }
}
