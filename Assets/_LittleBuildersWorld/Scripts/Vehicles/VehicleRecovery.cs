using System;
using LittleBuildersWorld.World;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>
    /// Notices when the driven vehicle is stuck (pedal held but not moving), fell out of the world or wandered past
    /// the edge, and puts it back on the nearest road. Manual reset is also available from the HUD.
    /// Vehicles cannot overturn (pitch/roll are frozen), so "upside-down" is not a state that needs handling.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public sealed class VehicleRecovery : MonoBehaviour
    {
        [SerializeField] private float m_StuckSeconds = 4f;
        [SerializeField] private float m_FallY = -4f;
        [SerializeField] private float m_WorldHalfSize = 105f;

        private VehicleController m_Controller;
        private float m_StuckTime;

        /// <summary>True when the HUD should gently suggest the reset button.</summary>
        public bool SeemsStuck => m_StuckTime >= m_StuckSeconds;

        public static event Action<Vehicle, Vector3> Recovered;

        private void Awake() => m_Controller = GetComponent<VehicleController>();

        private void FixedUpdate()
        {
            if (!m_Controller.IsDriven) { m_StuckTime = 0f; return; }
            Vector3 p = transform.position;
            if (p.y < m_FallY || Mathf.Abs(p.x) > m_WorldHalfSize || Mathf.Abs(p.z) > m_WorldHalfSize)
            {
                Recover();
                return;
            }
            DriveInput input = m_Controller.LastInput;
            bool pushing = input.Throttle > 0.1f || input.Brake > 0.1f;
            if (pushing && Mathf.Abs(m_Controller.MeasuredSpeed) < 0.25f) m_StuckTime += Time.fixedDeltaTime;
            else m_StuckTime = Mathf.Max(0f, m_StuckTime - Time.fixedDeltaTime * 2f);
        }

        public void Recover()
        {
            m_StuckTime = 0f;
            // The nearest road spot that is free: never drop a vehicle onto a parked one or into a fence.
            RecoveryPoint point = RecoveryPoint.NearestWhere(transform.position,
                p => m_Controller.IsClear(p.transform.position + Vector3.up * 0.3f, p.transform.rotation));
            var vehicle = GetComponent<Vehicle>();
            vehicle?.Tool?.CancelToRest();
            Vector3 from = transform.position;
            if (point != null) m_Controller.Teleport(point.transform.position + Vector3.up * 0.3f, point.transform.rotation);
            else if (vehicle != null) m_Controller.Teleport(vehicle.HomePosition, vehicle.HomeRotation);
            Recovered?.Invoke(vehicle, from);
        }
    }
}
