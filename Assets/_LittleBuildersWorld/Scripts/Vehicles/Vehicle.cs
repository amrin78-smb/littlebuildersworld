using System.Collections.Generic;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>Root of a drivable machine. Links the profile, controller and tool, and keeps a registry of all vehicles.</summary>
    [RequireComponent(typeof(VehicleController))]
    public sealed class Vehicle : MonoBehaviour
    {
        private static readonly List<Vehicle> s_All = new List<Vehicle>();
        public static IReadOnlyList<Vehicle> All => s_All;

        [SerializeField] private VehicleProfile m_Profile;
        [SerializeField] private VehicleController m_Controller;
        [Tooltip("Where Drive bubbles and markers float.")] [SerializeField] private Transform m_BubbleAnchor;

        private IVehicleTool m_Tool;
        private Vector3 m_HomePosition;
        private Quaternion m_HomeRotation;

        public VehicleProfile Profile => m_Profile;
        public VehicleController Controller => m_Controller;
        public IVehicleTool Tool => m_Tool;
        public string Id => m_Profile != null ? m_Profile.id : name;
        public VehicleType Type => m_Profile != null ? m_Profile.type : VehicleType.DumpTruck;
        public bool IsOccupied { get; private set; }
        public Vector3 BubblePosition => m_BubbleAnchor != null ? m_BubbleAnchor.position : transform.position + Vector3.up * 4f;
        public Vector3 HomePosition => m_HomePosition;
        public Quaternion HomeRotation => m_HomeRotation;

        private void Awake()
        {
            if (m_Controller == null) m_Controller = GetComponent<VehicleController>();
            m_Tool = GetComponent<IVehicleTool>();
            if (m_Profile == null) Debug.LogError($"[LBW] Vehicle '{name}' has no VehicleProfile assigned.", this);
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;
        }

        private void OnEnable()
        {
            if (!s_All.Contains(this)) s_All.Add(this);
        }

        private void OnDisable() => s_All.Remove(this);

        public T GetTool<T>() where T : class, IVehicleTool => m_Tool as T;

        public void SetHome(Vector3 position, Quaternion rotation)
        {
            m_HomePosition = position;
            m_HomeRotation = rotation;
        }

        public void SetOccupied(bool occupied)
        {
            IsOccupied = occupied;
            m_Controller.SetDriven(occupied);
            if (!occupied) m_Tool?.CancelToRest();
        }

        /// <summary>Puts the vehicle back in its yard bay (used by the vehicle selector).</summary>
        public void ReturnHome()
        {
            m_Tool?.CancelToRest();
            if (m_Controller.IsClear(m_HomePosition, m_HomeRotation))
            {
                m_Controller.Teleport(m_HomePosition, m_HomeRotation);
                return;
            }
            // Something is parked in the bay: use the nearest free road spot instead of landing on top of it.
            World.RecoveryPoint p = World.RecoveryPoint.NearestWhere(m_HomePosition,
                rp => m_Controller.IsClear(rp.transform.position + Vector3.up * 0.3f, rp.transform.rotation));
            if (p != null) m_Controller.Teleport(p.transform.position + Vector3.up * 0.3f, p.transform.rotation);
            else m_Controller.Teleport(m_HomePosition, m_HomeRotation);
        }

        public static Vehicle FindNearest(Vector3 position, VehicleType type, float maxDistance, Vehicle exclude = null)
        {
            Vehicle best = null;
            float bestSq = maxDistance * maxDistance;
            foreach (Vehicle v in s_All)
            {
                if (v == exclude || v.Type != type) continue;
                float sq = (v.transform.position - position).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = v;
                }
            }
            return best;
        }
    }
}
