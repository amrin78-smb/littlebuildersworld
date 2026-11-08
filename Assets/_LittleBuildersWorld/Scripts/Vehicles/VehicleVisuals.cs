using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>
    /// Presentation only: spins wheels and drums, scrolls track textures, leans the body to the slope and into turns,
    /// adds a gentle idle engine wobble, and turns the beacon. Reads the controller; never affects physics.
    /// </summary>
    public sealed class VehicleVisuals : MonoBehaviour
    {
        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] private VehicleController m_Controller;
        [SerializeField] private VehicleProfile m_Profile;
        [Tooltip("Visual pivot that leans; must be a child of the vehicle root.")] [SerializeField] private Transform m_Body;
        [SerializeField] private Transform[] m_Wheels = new Transform[0];
        [Tooltip("Front wheels that also turn with steering.")] [SerializeField] private Transform[] m_SteerWheels = new Transform[0];
        [SerializeField] private Renderer[] m_TrackRenderers = new Renderer[0];
        [SerializeField] private Transform m_Beacon;
        [SerializeField] private float m_MaxSteerAngle = 25f;

        private MaterialPropertyBlock m_Block;
        private float m_TrackOffset;
        private float m_WheelAngle;
        private Quaternion[] m_SteerRest;
        private float m_Idle;

        private void Awake()
        {
            m_Block = new MaterialPropertyBlock();
            m_SteerRest = new Quaternion[m_SteerWheels.Length];
            for (int i = 0; i < m_SteerWheels.Length; i++) m_SteerRest[i] = m_SteerWheels[i].localRotation;
        }

        private void LateUpdate()
        {
            if (m_Controller == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float speed = m_Controller.Speed;
            float radius = m_Profile != null ? Mathf.Max(0.1f, m_Profile.wheelRadius) : 0.5f;

            m_WheelAngle = Mathf.Repeat(m_WheelAngle + speed / radius * Mathf.Rad2Deg * dt, 360f);
            Quaternion spin = Quaternion.Euler(m_WheelAngle, 0f, 0f);
            for (int i = 0; i < m_Wheels.Length; i++)
            {
                if (m_Wheels[i] != null) m_Wheels[i].localRotation = spin;
            }
            float steer = m_Controller.Model != null ? m_Controller.Model.Steer : 0f;
            for (int i = 0; i < m_SteerWheels.Length; i++)
            {
                if (m_SteerWheels[i] != null) m_SteerWheels[i].localRotation = m_SteerRest[i] * Quaternion.Euler(0f, steer * m_MaxSteerAngle, 0f);
            }

            if (m_TrackRenderers.Length > 0)
            {
                // Tracked vehicles turning on the spot still look like their tracks are moving.
                float yawMotion = m_Controller.Model != null ? Mathf.Abs(m_Controller.Model.YawRate) * 0.02f : 0f;
                float scroll = m_Profile != null ? m_Profile.trackScroll : 0.35f;
                m_TrackOffset = Mathf.Repeat(m_TrackOffset + (speed + Mathf.Sign(speed + 0.001f) * yawMotion) * scroll * dt, 1f);
                for (int i = 0; i < m_TrackRenderers.Length; i++)
                {
                    Renderer r = m_TrackRenderers[i];
                    if (r == null) continue;
                    r.GetPropertyBlock(m_Block);
                    m_Block.SetVector(BaseMapSt, new Vector4(1f, 4f, 0f, -m_TrackOffset));
                    r.SetPropertyBlock(m_Block);
                }
            }

            if (m_Body != null)
            {
                Vector3 n = m_Controller.transform.InverseTransformDirection(m_Controller.GroundNormal);
                float maxLean = m_Profile != null ? m_Profile.maxBodyLean : 4f;
                float pitch = Mathf.Clamp(Mathf.Atan2(n.z, n.y) * Mathf.Rad2Deg, -12f, 12f);
                float roll = Mathf.Clamp(-Mathf.Atan2(n.x, n.y) * Mathf.Rad2Deg, -12f, 12f);
                float turnLean = m_Controller.Model != null ? -m_Controller.Model.YawRate / 90f * maxLean * Mathf.Clamp01(Mathf.Abs(speed) / 3f) : 0f;
                m_Idle += dt * (m_Controller.IsDriven ? 18f : 0f);
                float wobble = m_Controller.IsDriven ? Mathf.Sin(m_Idle) * 0.012f : 0f;
                Quaternion target = Quaternion.Euler(pitch, 0f, roll + turnLean);
                m_Body.localRotation = Quaternion.Slerp(m_Body.localRotation, target, 1f - Mathf.Exp(-8f * dt));
                m_Body.localPosition = new Vector3(0f, wobble, 0f);
            }

            if (m_Beacon != null && m_Controller.IsDriven) m_Beacon.Rotate(0f, 240f * dt, 0f, Space.Self);
        }
    }
}
