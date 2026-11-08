using LittleBuildersWorld.Core;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>Applies the saved paint scheme to the vehicle's painted panels using property blocks (no material copies).</summary>
    public sealed class VehiclePaint : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Vehicle m_Vehicle;
        [SerializeField] private Renderer[] m_Primary = new Renderer[0];
        [SerializeField] private Renderer[] m_Secondary = new Renderer[0];

        private MaterialPropertyBlock m_Block;

        private void Start()
        {
            m_Block = new MaterialPropertyBlock();
            Apply();
            if (GameServices.Instance != null) GameServices.Instance.Progression.PaintChanged += OnPaintChanged;
        }

        private void OnDestroy()
        {
            if (GameServices.Instance != null) GameServices.Instance.Progression.PaintChanged -= OnPaintChanged;
        }

        private void OnPaintChanged(string vehicleId)
        {
            if (m_Vehicle != null && vehicleId == m_Vehicle.Id) Apply();
        }

        public void Apply()
        {
            GameServices s = GameServices.Instance;
            if (s == null || m_Vehicle == null) return;
            int index = s.Progression.GetPaint(m_Vehicle.Id);
            PaintScheme[] paints = s.Config.paints;
            bool factory = index <= 0 || index >= paints.Length || !s.Progression.IsPaintAvailable(index);
            SetAll(m_Primary, factory ? (Color?)null : paints[index].primary);
            SetAll(m_Secondary, factory ? (Color?)null : paints[index].secondary);
        }

        private void SetAll(Renderer[] renderers, Color? colour)
        {
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                if (colour == null)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                m_Block.Clear();
                m_Block.SetColor(BaseColor, colour.Value);
                r.SetPropertyBlock(m_Block);
            }
        }
    }
}
