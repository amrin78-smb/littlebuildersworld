using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    public interface IDriveInputSource
    {
        DriveInput ReadDrive();
    }

    /// <summary>
    /// Arcade Rigidbody controller. The <see cref="VehicleDriveModel"/> decides speed and yaw; this sets the body's
    /// velocity along the ground and rotates it about the vertical only. Pitch and roll are frozen, so a vehicle can
    /// never tip over; the visible body leans to the ground slope instead (<see cref="VehicleVisuals"/>).
    /// Colliders are frictionless, so bumping a wall slides along it gently instead of sticking or bouncing.
    ///
    /// Why Rigidbody over kinematic: we get collision resolution against the world and other vehicles for free,
    /// while the drive model keeps the feel fully deterministic and tunable.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        [SerializeField] private VehicleProfile m_Profile;
        [SerializeField] private LayerMask m_GroundMask = 1; // Default
        [SerializeField] private float m_GroundProbeHeight = 1.2f;
        [SerializeField] private float m_GroundProbeLength = 2.2f;

        private Rigidbody m_Body;
        private VehicleDriveModel m_Model;
        private IDriveInputSource m_Input;
        private IVehicleTool m_Tool;
        private bool m_Driven;
        private bool m_Grounded;
        private Vector3 m_GroundNormal = Vector3.up;
        private DriveInput m_LastInput;
        private float m_ExtraSteerScale = 1f;

        public VehicleDriveModel Model => m_Model;
        public float Speed => m_Model?.Speed ?? 0f;
        public float MeasuredSpeed { get; private set; }
        public bool IsGrounded => m_Grounded;
        public Vector3 GroundNormal => m_GroundNormal;
        public DriveInput LastInput => m_LastInput;
        public bool IsDriven => m_Driven;
        public Rigidbody Body => m_Body;

        private void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_Tool = GetComponent<IVehicleTool>();
            var tuning = m_Profile != null ? m_Profile.tuning : DriveTuning.Default;
            m_Model = new VehicleDriveModel(tuning);
            m_Body.mass = m_Profile != null ? m_Profile.mass : 2000f;
            m_Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;
            m_Body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            m_Body.linearDamping = 0f;
            m_Body.angularDamping = 5f;
            m_Body.centerOfMass = new Vector3(0f, 0.2f, 0f);
            Park();
        }

        public void SetInputSource(IDriveInputSource source) => m_Input = source;

        /// <summary>Steering sensitivity from settings (0.5..1.5).</summary>
        public void SetSteerSensitivity(float scale) => m_ExtraSteerScale = Mathf.Clamp(scale, 0.5f, 1.5f);

        public void SetDriven(bool driven)
        {
            m_Driven = driven;
            if (driven)
            {
                m_Body.isKinematic = false;
                m_Body.WakeUp();
            }
            else
            {
                Park();
            }
        }

        /// <summary>Stops dead and becomes an immovable parked object, so other vehicles cannot shove it around.</summary>
        public void Park()
        {
            m_Model?.Reset();
            m_LastInput = DriveInput.None;
            if (!m_Body.isKinematic)
            {
                m_Body.linearVelocity = Vector3.zero;
                m_Body.angularVelocity = Vector3.zero;
            }
            m_Body.isKinematic = true;
            MeasuredSpeed = 0f;
        }

        /// <summary>Moves the vehicle safely (recovery, return to yard). Keeps it upright.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            bool driven = m_Driven;
            float yaw = rotation.eulerAngles.y;
            m_Model?.Reset();
            if (!m_Body.isKinematic)
            {
                m_Body.linearVelocity = Vector3.zero;
                m_Body.angularVelocity = Vector3.zero;
            }
            m_Body.position = position;
            m_Body.rotation = Quaternion.Euler(0f, yaw, 0f);
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Physics.SyncTransforms();
            if (!driven) Park();
        }

        private void FixedUpdate()
        {
            if (m_Body.isKinematic) return;
            float dt = Time.fixedDeltaTime;

            DriveInput input = m_Driven && m_Input != null ? m_Input.ReadDrive() : DriveInput.None;
            if (m_Tool != null && m_Tool.IsBusy) input = DriveInput.None; // hold still while the machine works
            input.Steer *= m_ExtraSteerScale;
            input.Steer = Mathf.Clamp(input.Steer, -1f, 1f);
            m_LastInput = input;

            ProbeGround();

            Vector3 velocity = m_Body.linearVelocity;
            Vector3 forward = transform.forward;
            MeasuredSpeed = Vector3.Dot(new Vector3(velocity.x, 0f, velocity.z), new Vector3(forward.x, 0f, forward.z).normalized);
            m_Model.ReconcileMeasuredSpeed(MeasuredSpeed);
            m_Model.Step(input, dt);

            // Rebuilt from yaw alone every step: whatever a collision does, the body can never pitch or roll
            // (a tilted collision box could otherwise "climb" and lift the vehicle off the ground).
            float yaw = m_Body.rotation.eulerAngles.y + m_Model.YawRate * dt;
            Quaternion upright = Quaternion.Euler(0f, yaw, 0f);
            if (Quaternion.Angle(m_Body.rotation, upright) > 0.01f) m_Body.MoveRotation(upright);

            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;
            Vector3 drive;
            if (m_Grounded)
            {
                // Follow the slope, with a light press into the ground so bumps never launch the vehicle.
                drive = Vector3.ProjectOnPlane(flatForward, m_GroundNormal).normalized * m_Model.Speed;
                drive.y = Mathf.Min(drive.y, 2f) - 0.5f;
            }
            else
            {
                // Airborne (edge of a ramp or a bump): never rise, keep falling under the physics step's gravity.
                drive = flatForward * m_Model.Speed;
                drive.y = Mathf.Min(velocity.y, 0f);
            }
            m_Body.linearVelocity = drive;
            m_Body.angularVelocity = Vector3.zero;
        }

        private void ProbeGround()
        {
            Vector3 origin = m_Body.position + Vector3.up * m_GroundProbeHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, m_GroundProbeLength, m_GroundMask, QueryTriggerInteraction.Ignore))
            {
                m_Grounded = hit.distance <= m_GroundProbeHeight + 0.6f;
                m_GroundNormal = Vector3.Slerp(m_GroundNormal, hit.normal, 0.3f);
            }
            else
            {
                m_Grounded = false;
                m_GroundNormal = Vector3.Slerp(m_GroundNormal, Vector3.up, 0.1f);
            }
        }
    }
}
